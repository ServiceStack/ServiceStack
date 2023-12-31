using System.Collections;
using System.Globalization;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using ServiceStack.DataAnnotations;
using ServiceStack.Html;
using ServiceStack.Text;

namespace ServiceStack.Blazor;

public enum TextStyle
{
    None,
    SplitCase,
    Humanize,
    TitleCase,
    PascalCase,
    CamelCase,
}

public class TextDumpOptions
{
    public TextStyle HeaderStyle { get; set; }
    public string? Caption { get; set; }
    public string? CaptionIfEmpty { get; set; }
    public string[]? Headers { get; set; }
    public bool IncludeRowNumbers { get; set; } = true;
    
    internal int Depth { get; set; }
    internal bool HasCaption { get; set; }
}

public class MarkdownTable
{
    public bool IncludeHeaders { get; set; } = true;
    public bool IncludeRowNumbers { get; set; } = true;
    public string? Caption { get; set; }
    public List<string> Headers { get; } = new();
    public List<Type>? HeaderTypes { get; set; }
    public List<List<string>> Rows { get; } = new();

    public string Render()
    {
        if (Rows.Count == 0)
            return "";

        var sb = StringBuilderCache.Allocate();

        var headersCount = Headers.Count;
        var colSize = new int[headersCount];
        var i = 0;

        var rowNumLength = IncludeRowNumbers ? (Rows.Count + 1).ToString().Length : 0;

        var noOfCols = IncludeHeaders && headersCount > 0
            ? headersCount
            : Rows[0].Count;

        for (; i < noOfCols; i++)
        {
            colSize[i] = IncludeHeaders && i < headersCount
                ? Headers[i].Length
                : 0;

            foreach (var row in Rows)
            {
                var rowLen = i < row.Count ? row[i]?.Length ?? 0 : 0;
                if (rowLen > colSize[i])
                    colSize[i] = rowLen;
            }
        }

        if (!string.IsNullOrEmpty(Caption))
        {
            sb.AppendLine(Caption)
                .AppendLine();
        }

        if (IncludeHeaders && headersCount > 0)
        {
            sb.Append("| ");
            if (IncludeRowNumbers)
            {
                sb.Append("#".PadRight(rowNumLength, ' '))
                    .Append(" | ");
            }

            for (i = 0; i < headersCount; i++)
            {
                var header = Headers[i];
                sb.Append(header.PadRight(colSize[i], ' '))
                    .Append(i + 1 < headersCount ? " | " : " |");
            }
            sb.AppendLine();

            sb.Append("|-");
            if (IncludeRowNumbers)
            {
                sb.Append("".PadRight(rowNumLength, '-'))
                    .Append("-|-");
            }

            for (i = 0; i < headersCount; i++)
            {
                sb.Append("".PadRight(colSize[i], '-'))
                    .Append(i + 1 < headersCount ? "-|-" : "-|");
            }
            sb.AppendLine();
        }

        for (var rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
        {
            var row = Rows[rowIndex];
            sb.Append("| ");

            if (IncludeRowNumbers)
            {
                sb.Append($"{rowIndex + 1}".PadRight(rowNumLength, ' '))
                    .Append(" | ");
            }

            for (i = 0; i < headersCount; i++)
            {
                var field = i < row.Count ? row[i] : null;
                var headerType = HeaderTypes?.Count > i ? HeaderTypes[i] : typeof(string);
                var cellValue = headerType.IsNumericType() || headerType == typeof(DateTime) || headerType == typeof(TimeSpan)
                    ? (field ?? "").PadLeft(colSize[i], ' ')
                    : (field ?? "").PadRight(colSize[i], ' ');
                sb.Append(cellValue)
                    .Append(i + 1 < headersCount ? " | " : " |");
            }
            sb.AppendLine();
        }
        sb.AppendLine();

        return StringBuilderCache.ReturnAndFree(sb);
    }
}

public static class TextUtils
{
    public static CultureInfo UseCulture { get; set; } = CultureInfo.InvariantCulture;

    public static Func<string, string> FormatString { get; set; } = DefaultFormatString;
    public static string DefaultFormatString(string value) => value.StartsWith("/Date(")
        ? FormatDate(ServiceStack.Text.Common.DateTimeSerializer.ParseShortestXsdDateTime(value))
        : value;
    public static Func<decimal, string> FormatCurrency { get; set; } = DefaultFormatCurrency;
    public static string DefaultFormatCurrency(decimal value) => string.Format(UseCulture, "{0:C}", value);

    public static Func<DateTime, string> FormatDate { get; set; } = DefaultFormatDate;
    public static string DefaultFormatDate(DateTime value) => value.ToString("yyyy/MM/dd", UseCulture);
    public static string FormatIso8601Date(DateTime value) => value.ToString("yyyy-MM-dd", UseCulture);
    public static Func<TimeSpan, string> FormatTime { get; set; } = DefaultFormatTime;
    public static string DefaultFormatTime(TimeSpan value) => value.ToString(@"h\:mm\:ss");

    public static T ConvertTo<T>(object from) => from.ConvertTo<T>();
    public static object ConvertTo(object from, Type toType) => from.ConvertTo(toType);

    public static string FormatDateObject(object? o) => o switch {
        DateTime dt => FormatDate(dt),
        null => "",
        _ => FormatDate(o.ConvertTo<DateTime>())
    };

    public static string SplitCase(string text) => text.SplitCamelCase().Replace('_', ' ').Replace("  ", " ");
    public static string Humanize(string text) => SplitCase(text).ToTitleCase();
    public static string TitleCase(string text) => text.ToTitleCase();
    public static string PascalCase(string text) => text.ToPascalCase();
    public static string CamelCase(string text) => text.ToCamelCase();
    public static string SnakeCase(string text) => text.ToLowercaseUnderscore();
    public static string KebabCase(string text) => text.ToLowercaseUnderscore().Replace("_", "-");

    public static string StyleText(string text, TextStyle textStyle)
    {
        if (text == null) return "";
        return textStyle switch
        {
            TextStyle.SplitCase => SplitCase(text),
            TextStyle.Humanize => Humanize(text),
            TextStyle.TitleCase => TitleCase(text),
            TextStyle.PascalCase => PascalCase(text),
            TextStyle.CamelCase => CamelCase(text),
            _ => text,
        };
    }

    public static List<KeyValuePair<string, string>> ToKeyValuePairs(IEnumerable? values)
    {
        if (values is null)
            return new();

        var to = new List<KeyValuePair<string, string>>();
        foreach (var item in values)
        {
            if (item is string s)
            {
                to.Add(KeyValuePair.Create(s, s));
            }
            else if (item.GetType().IsValueType)
            {
                var v = ToModelString(item);
                to.Add(KeyValuePair.Create(v, v));
            }
            else
                throw new NotSupportedException($"Cannot convert '{item.GetType().Name}' to KeyValuePairs");
        }
        return to;
    }

    public static string TextList(IEnumerable items, TextDumpOptions? options)
    {
        if (options == null)
            options = new TextDumpOptions();

        if (items is IDictionary<string, object> single)
            items = new[] { single };

        var depth = options.Depth;
        options.Depth += 1;

        try
        {
            var headerStyle = options.HeaderStyle;

            List<string>? keys = null;

            var table = new MarkdownTable
            {
                IncludeRowNumbers = options.IncludeRowNumbers
            };

            var list = items.Cast<object>().ToList();
            foreach (var item in list)
            {
                if (item is IDictionary<string, object> d)
                {
                    if (keys == null)
                    {
                        keys = options.Headers?.ToList() ?? AllKeysWithDefaultValues(list);
                        table.HeaderTypes ??= new List<Type>();
                        foreach (var key in keys)
                        {
                            table.Headers.Add(StyleText(key, headerStyle));
                            table.HeaderTypes.Add(list.FirstElementType(key));
                        }
                    }

                    var row = new List<string>();

                    foreach (var key in keys)
                    {
                        var value = d.ContainsKey(key) ? d[key] : null;
                        if (ReferenceEquals(value, list)) break; // Prevent cyclical deps like 'it' binding

                        if (value == null)
                        {
                            row.Add(string.Empty);
                        }
                        else if (!IsComplexType(value?.GetType()))
                        {
                            row.Add(GetScalarText(value));
                        }
                        else
                        {
                            var cellValue = TextDump(value, options);
                            row.Add(cellValue);
                        }
                    }
                    table.Rows.Add(row);
                }
            }

            var isEmpty = table.Rows.Count == 0;
            if (isEmpty)
                return options.CaptionIfEmpty ?? string.Empty;

            var caption = options.Caption;
            if (caption != null && !options.HasCaption)
            {
                table.Caption = caption;
                options.HasCaption = true;
            }

            var txt = table.Render();
            return txt;
        }
        finally
        {
            options.Depth = depth;
        }
    }

    public static string TextDump(object? target, TextDumpOptions options)
    {
        if (target == null) return string.Empty;
        if (options == null)
            options = new TextDumpOptions();

        var depth = options.Depth;
        options.Depth += 1;

        try
        {
            target = ConvertDumpType(target);

            if (!IsComplexType(target?.GetType()))
                return GetScalarText(target);

            var headerStyle = options.HeaderStyle;

            if (target is IEnumerable e)
            {
                var objs = e.Cast<object>().Select(x => x).ToList();

                var isEmpty = objs.Count == 0;
                if (isEmpty)
                    return options.CaptionIfEmpty ?? string.Empty;

                var first = objs[0];
                if (first is IDictionary && objs.Count > 1)
                    return TextList(objs, options);

                var sb = StringBuilderCacheAlt.Allocate();

                string? writeCaption = null;
                var caption = options.Caption;
                if (caption != null && !options.HasCaption)
                {
                    writeCaption = caption;
                    options.HasCaption = true;
                }

                var keys = new List<string>();
                var values = new List<string>();

                string TextKvps(StringBuilder s, IEnumerable<KeyValuePair<string, object>> kvps)
                {
                    foreach (var kvp in kvps)
                    {
                        if (kvp.Value == target)
                            break; // Prevent cyclical deps like 'it' binding

                        keys.Add(StyleText(kvp.Key, headerStyle) ?? "");

                        var field = !IsComplexType(kvp.Value?.GetType())
                            ? GetScalarText(kvp.Value)
                            : TextDump(kvp.Value, options);

                        values.Add(field);
                    }

                    var keySize = keys.Max(x => x.Length);
                    var valuesSize = values.Max(x => x.Length);

                    s.AppendLine(writeCaption != null
                        ? $"| {writeCaption.PadRight(keySize + valuesSize + 2, ' ')} ||"
                        : $"|||");
                    s.AppendLine(writeCaption != null
                        ? $"|-{"".PadRight(keySize, '-')}-|-{"".PadRight(valuesSize, '-')}-|"
                        : "|-|-|");

                    for (var i = 0; i < keys.Count; i++)
                    {
                        s.Append("| ")
                            .Append(keys[i].PadRight(keySize, ' '))
                            .Append(" | ")
                            .Append(values[i].PadRight(valuesSize, ' '))
                            .Append(" |")
                            .AppendLine();
                    }

                    return StringBuilderCache.ReturnAndFree(s);
                }

                if (first is KeyValuePair<string, object>)
                {
                    return TextKvps(sb, objs.Cast<KeyValuePair<string, object>>());
                }
                else
                {
                    if (!IsComplexType(first?.GetType()))
                    {
                        foreach (var o in objs)
                        {
                            values.Add(GetScalarText(o));
                        }

                        var valuesSize = values.Max(MaxLineLength);
                        if (writeCaption?.Length > valuesSize)
                            valuesSize = writeCaption.Length;

                        sb.AppendLine(writeCaption != null
                            ? $"| {writeCaption.PadRight(valuesSize)} |"
                            : $"||");
                        sb.AppendLine(writeCaption != null
                            ? $"|-{"".PadRight(valuesSize, '-')}-|"
                            : "|-|");

                        foreach (var value in values)
                        {
                            sb.Append("| ")
                                .Append(value.PadRight(valuesSize, ' '))
                                .Append(" |")
                                .AppendLine();
                        }
                    }
                    else
                    {
                        if (objs.Count > 1)
                        {
                            if (writeCaption != null)
                                sb.AppendLine(writeCaption)
                                    .AppendLine();

                            var rows = objs.Select(x => x.ToObjectDictionary()).ToList();
                            var list = TextList(rows, options);
                            sb.AppendLine(list);
                            return StringBuilderCache.ReturnAndFree(sb);
                        }
                        else
                        {
                            foreach (var o in objs)
                            {
                                if (!IsComplexType(o?.GetType()))
                                {
                                    values.Add(GetScalarText(o));
                                }
                                else
                                {
                                    var body = TextDump(o, options);
                                    values.Add(body);
                                }
                            }

                            var valuesSize = values.Max(MaxLineLength);
                            if (writeCaption?.Length > valuesSize)
                                valuesSize = writeCaption.Length;

                            sb.AppendLine(writeCaption != null
                                ? $"| {writeCaption.PadRight(valuesSize, ' ')} |"
                                : $"||");
                            sb.AppendLine(writeCaption != null ? $"|-{"".PadRight(valuesSize, '-')}-|" : "|-|");

                            foreach (var value in values)
                            {
                                sb.Append("| ")
                                    .Append(value.PadRight(valuesSize, ' '))
                                    .Append(" |")
                                    .AppendLine();
                            }
                        }
                    }
                }
                return StringBuilderCache.ReturnAndFree(sb);
            }

            return TextDump(target.ToObjectDictionary(), options);
        }
        finally
        {
            options.Depth = depth;
        }
    }

    private static int MaxLineLength(string s)
    {
        if (string.IsNullOrEmpty(s))
            return 0;

        var len = 0;
        foreach (var line in s.ReadLines())
        {
            if (line.Length > len)
                len = line.Length;
        }
        return len;
    }
        
    public static string GetScalarText(object? value)
    {
        if (value == null || value.ToString() == string.Empty)
            return string.Empty;

        if (value is string s)
            return FormatString(s);

        if (value is decimal dec)
        {
            var isMoney = dec == Math.Floor(dec * 100);
            if (isMoney)
                return FormatCurrency(dec);
        }

        if (value.GetType().IsNumericType() || value is bool)
            return value.ToString() ?? "";

        if (value is DateTime d)
            return FormatDate(d);

        if (value is TimeSpan t)
            return FormatTime(t);

        return value.ToString() ?? "";
    }

    public static bool IsComplexType(this Type? type)
    {
        return type != null && !type.IsValueType && type != typeof(string) && type != typeof(Uri);
    }

    public static bool IsComputed(this System.Reflection.PropertyInfo? prop)
    {
        return prop != null && prop.AllAttributes().Any(x => x.GetType() == typeof(ComputedAttribute) || x.GetType() == typeof(CustomSelectAttribute));
    }

    internal static object ConvertDumpType(object target)
    {
        var targetType = target.GetType();
        var genericKvps = targetType.GetTypeWithGenericTypeDefinitionOf(typeof(KeyValuePair<,>));
        if (genericKvps != null)
        {
            var keyGetter = TypeProperties.Get(targetType).GetPublicGetter("Key");
            var valueGetter = TypeProperties.Get(targetType).GetPublicGetter("Value");
            return new Dictionary<string, object> {
                { keyGetter(target).ConvertTo<string>(), valueGetter(target) },
            };
        }

        if (target is IEnumerable e)
        {
            //Convert IEnumerable<object> to concrete generic collection so generic args can be inferred
            if (e is IEnumerable<object> enumObjs)
            {
                Type? elType = null;
                foreach (var item in enumObjs)
                {
                    elType = item.GetType();
                    break;
                }
                if (elType != null)
                {
                    targetType = typeof(List<>).MakeGenericType(elType);
                    var genericList = (IList)targetType.CreateInstance();
                    foreach (var item in e)
                    {
                        genericList.Add(item.ConvertTo(elType));
                    }
                    target = genericList;
                }
            }

            if (targetType.GetKeyValuePairsTypes(out var keyType, out var valueType, out var kvpType))
            {
                var keyGetter = TypeProperties.Get(kvpType).GetPublicGetter("Key");
                var valueGetter = TypeProperties.Get(kvpType).GetPublicGetter("Value");

                string? key1 = null, key2 = null;
                foreach (var kvp in e)
                {
                    if (key1 == null)
                    {
                        key1 = keyGetter(kvp).ConvertTo<string>();
                        continue;
                    }
                    key2 = keyGetter(kvp).ConvertTo<string>();
                    break;
                }

                var isColumn = key1 == key2;
                if (isColumn)
                {
                    var to = new List<Dictionary<string, object>>();
                    foreach (var kvp in e)
                    {
                        to.Add(new Dictionary<string, object> { { keyGetter(kvp).ConvertTo<string>(), valueGetter(kvp) } });
                    }
                    return to;
                }

                return target.ToObjectDictionary();
            }
        }

        return target;
    }

    public static List<string> AllKeysWithDefaultValues(IEnumerable collection)
    {
        List<string> allKeys = new();
        HashSet<string> keysWithValues = new();

        foreach (var o in collection)
        {
            if (o is IEnumerable<KeyValuePair<string, object>> d)
            {
                foreach (var entry in d)
                {
                    if (!allKeys.Contains(entry.Key))
                        allKeys.Add(entry.Key);
                    if (entry.Value == null)
                        continue;
                    var valueType = entry.Value.GetType();
                    if (valueType.IsValueType && entry.Value.Equals(valueType.GetDefaultValue()))
                        continue;
                    keysWithValues.Add(entry.Key);
                }
            }
        }
        allKeys.RemoveAll(x => !keysWithValues.Contains(x));
        return allKeys;
    }

    public static T? IIF<T>(bool test, T ifTrue) => test ? ifTrue : default;
    public static T? IIF<T>(bool test, T ifTrue, T ifFalse) => test ? ifTrue : ifFalse;

    public static object? Get(this Dictionary<string, object>? o, string name)
    {
        return o?.TryGetValue(name, out var value) == true
            ? value
            : null;
    }

    public static bool IsNullOrWhiteSpace(this Dictionary<string, object>? o, string name)
    {
        var value = Get(o, name);
        return value == null || string.IsNullOrWhiteSpace(value.ToString());
    }

    public static List<T> Prepend<T>(this List<T> list, T item)
    {
        list.Insert(0, item);
        return list;
    }

    public static List<T> Append<T>(this List<T> list, T item)
    {
        list.Add(item);
        return list;
    }

    public static string ToModelString(this object? from)
    {
        if (from == null)
            return "";

        if (from is DateTime dt)
            return dt.ToString("yyyy-MM-dd");
        if (from is DateTimeOffset dto)
            return dto.ToString("yyyy-MM-dd");
        if (from is DateOnly dtonly)
            return dtonly.ToString("yyyy-MM-dd");

        return from.ConvertTo<string>();
    }

    public static List<string>? ToModelStrings(this object? from)
    {
        if (from == null)
            return null;
        if (from is List<string> list)
            return list;
        if (from is string s)
            return s.FromJsv<List<string>>();
        return from.ConvertTo<List<string>>();
    }

    /// <summary>
    /// Used to convert Typed model into an object dictionary for usage in DynamicInput
    /// </summary>
    public static Dictionary<string, object> ToModelDictionary<T>(this T? from)
    {
        var isNew = from == null;
        if (isNew)
            return new Dictionary<string, object>();

        var obj = from.ToObjectDictionary();

        foreach (var entry in obj)
        {
            if (entry.Value is IEnumerable xs)
            {
                obj[entry.Key] = xs.ShallowClone();
            }
        }

        return obj;
    }

    /// <summary>
    /// Used to convert DynamicInput dictionary to a Typed model
    /// </summary>
    public static T FromModelDictionary<T>(this Dictionary<string, object> from) => (T)FromModelDictionary(from, typeof(T));

    /// <summary>
    /// Used to convert DynamicInput dictionary to a Typed model
    /// </summary>
    public static object FromModelDictionary(this Dictionary<string, object> from, Type type)
    {
        var to = from.FromObjectDictionary(type);
        return to;
    }

    /// <summary>
    /// Create a Form Layout from a declarative annotated DTO definition
    /// </summary>
    public static List<InputInfo> CreateFormLayout<T>(this MetadataType metadataType) => CreateFormLayout(metadataType, typeof(T));
    public static List<InputInfo> CreateFormLayout(this MetadataType metadataType, Type type) => CreateFormLayout(metadataType, type, null);
    public static List<InputInfo> CreateFormLayout(this MetadataType metadataType, Type type, AppMetadata? appMetadata)
    {
        var typeProps = TypeProperties.Get(type).PropertyMap;
        metadataType.Type ??= type;

        MetadataType? dataModel = null;
        if (appMetadata != null)
        {
            var op = appMetadata.Api.Operations.FirstOrDefault(x => x.Request.Name == metadataType.Name);
            if (op != null)
            {
                dataModel = appMetadata.GetType(op.DataModel);
            }
        }

        var formLayout = new List<InputInfo>();
        foreach (var prop in metadataType.Properties)
        {
            prop.PropertyInfo ??= typeProps.TryGetValue(prop.Name, out var pi) ? pi.PropertyInfo : null;
            if (prop.PropertyInfo == null)
                continue;

            if (prop.IsPrimaryKey == true)
                continue;

            if (prop.Input == null)
                prop.PopulateInput(Input.Create(prop.PropertyInfo));

            var input = prop.Input!;
            if (appMetadata != null)
            {
                if (input.Type == Input.Types.File && prop.UploadTo != null)
                {
                    var uploadLocation = appMetadata.Plugins.FilesUpload?.Locations.FirstOrDefault(x => x.Name == prop.UploadTo);
                    if (uploadLocation?.AllowExtensions != null)
                    {
                        input.Accept ??= string.Join(',', uploadLocation.AllowExtensions.Map(x => x.StartsWith('.') ? x : $".{x}"));
                    }
                }
                if (dataModel != null)
                {
                    var dataModelProp = dataModel.Property(prop.Name);
                    prop.Ref ??= dataModelProp?.Ref;
                }
            }

            formLayout.Add(input);
        }
        return formLayout;
    }

    public static string Truncate(string str, int maxLength)
    {
        return str.Length > maxLength
            ? str.SafeSubstring(0, maxLength) + "..."
            : str;
    }

    public static string TruncateJson(string json, int maxLength)
    {
        var s = Truncate(json, maxLength);
        return s.EndsWith("...")
            ? s + (s.StartsWith('{') ? " }" : s.StartsWith('[') ? " ]" : "")
            : s;
    }

    public static object? FirstOrDefault(IEnumerable items)
    {
        if (items == null)
            return null;
        foreach (var item in items)
        {
            return item;
        }
        return null;
    }

    public static string FormatJson(object? o)
    {
        return o == null ? "" : System.Text.Json.JsonSerializer.Serialize(o, BlazorConfig.Instance.FormatJsonOptions);
    }

    public static string Dump(object? o) => TypeSerializer.Dump(o);

    public static string Pluralize<T>(string word, ICollection<T>? collection) => Pluralize(word, collection?.Count ?? 0);
    public static string Pluralize(string word, int count)
    {
        return count + " " + (count == 1
            ? word
            : Words.Pluralize(word));
    }

    const int k = 1024;
    public static string[] ByteSizes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };

    public static string FormatBytes(long bytes, int decimals = 2)
    {
        if (bytes == 0)
            return "0 bytes";
        
        var dm = decimals < 0 ? 0 : decimals;
        var i = (int) Math.Floor(Math.Log(bytes) / Math.Log(k));
        return (bytes / Math.Pow(k, i)).ToString("N" + dm) + ' ' + ByteSizes[i % ByteSizes.Length];
    }
}
