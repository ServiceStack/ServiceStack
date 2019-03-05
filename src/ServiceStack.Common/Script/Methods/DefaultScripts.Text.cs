using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    // ReSharper disable InconsistentNaming

    public class MarkdownTable
    {
        public bool IncludeHeaders { get; set; } = true;
        public bool IncludeRowNumbers { get; set; } = true;
        public string Caption { get; set; }
        public List<string> Headers { get; } = new List<string>();
        public List<List<string>> Rows { get; } = new List<List<string>>();
        
        public string Render()
        {
            if (Rows.Count == 0)
                return null;
            
            var sb = StringBuilderCache.Allocate();

            var headersCount = Headers.Count;
            var colSize = new int[headersCount];
            var i=0;

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
                        .Append( i + 1 < headersCount ? " | " : " |");
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
                        .Append( i + 1 < headersCount ? "-|-" : "-|");
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
                    sb.Append((field ?? "").PadRight(colSize[i], ' '))
                        .Append( i + 1 < headersCount ? " | " : " |");
                }
                sb.AppendLine();
            }
            sb.AppendLine();

            return StringBuilderCache.ReturnAndFree(sb);
        }
    }

    public partial class DefaultScripts
    {
        public IRawString textList(IEnumerable target) => TextList(target, new TextDumpOptions { Defaults = Context.DefaultMethods }).ToRawString();
        public IRawString textList(IEnumerable target, Dictionary<string, object> options) => 
            TextList(target, TextDumpOptions.Parse(options, Context.DefaultMethods)).ToRawString();
        
        public IRawString textDump(object target) => TextDump(target, new TextDumpOptions { Defaults = Context.DefaultMethods }).ToRawString();
        public IRawString textDump(object target, Dictionary<string, object> options) => 
            TextDump(target, TextDumpOptions.Parse(options, Context.DefaultMethods)).ToRawString();
        
        public static string TextList(IEnumerable items, TextDumpOptions options)
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

                List<string> keys = null;

                var table = new MarkdownTable {
                    IncludeRowNumbers = options.IncludeRowNumbers
                };

                foreach (var item in items)
                {
                    if (item is IDictionary<string, object> d)
                    {
                        if (keys == null)
                        {
                            keys = d.Keys.ToList();
                            foreach (var key in keys)
                            {
                                table.Headers.Add(ViewUtils.StyleText(key, headerStyle));
                            }
                        }

                        var row = new List<string>();
                        
                        foreach (var key in keys)
                        {
                            var value = d[key];
                            if (ReferenceEquals(value, items)) break; // Prevent cyclical deps like 'it' binding
                            
                            if (!isComplexType(value))
                            {
                                row.Add(GetScalarText(value, options.Defaults));
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

        public static string TextDump(object target, TextDumpOptions options)
        {
            if (options == null)
                options = new TextDumpOptions();
            
            var depth = options.Depth;
            options.Depth += 1;

            try
            {
                target = ConvertDumpType(target);
                
                if (!isComplexType(target))
                    return GetScalarText(target, options.Defaults);

                var headerStyle = options.HeaderStyle;

                if (target is IEnumerable e)
                {
                    var objs = e.Map(x => x);

                    var isEmpty = objs.Count == 0;
                    if (isEmpty)
                        return options.CaptionIfEmpty ?? string.Empty;

                    var first = objs[0];
                    if (first is IDictionary && objs.Count > 1)
                        return TextList(objs, options);

                    var sb = StringBuilderCacheAlt.Allocate();

                    string writeCaption = null; 
                    var caption = options.Caption;
                    if (caption != null && !options.HasCaption)
                    {
                        writeCaption = caption;
                        options.HasCaption = true;
                    }

                    if (!isEmpty)
                    {
                        var keys = new List<string>();
                        var values = new List<string>();

                        string TextKvps(StringBuilder s, IEnumerable<KeyValuePair<string, object>> kvps)
                        {
                            foreach (var kvp in kvps)
                            {
                                if (kvp.Value == target) 
                                    break; // Prevent cyclical deps like 'it' binding

                                keys.Add(ViewUtils.StyleText(kvp.Key, headerStyle) ?? "");

                                var field = !isComplexType(kvp.Value)
                                    ? GetScalarText(kvp.Value, options.Defaults)
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
                            if (!isComplexType(first))
                            {
                                foreach (var o in objs)
                                {
                                    values.Add(GetScalarText(o, options.Defaults));
                                }
                                
                                var valuesSize = values.Max(MaxLineLength);
                                if (writeCaption?.Length > valuesSize)
                                    valuesSize = writeCaption.Length;

                                sb.AppendLine(writeCaption != null 
                                    ? $"| {writeCaption.PadRight(valuesSize)} |" 
                                    : $"||");
                                sb.AppendLine(writeCaption != null 
                                    ? $"|-{"".PadRight(valuesSize,'-')}-|" 
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
                            
                                    var rows = objs.Map(x => x.ToObjectDictionary());
                                    var list = TextList(rows, options);
                                    sb.AppendLine(list);
                                    return StringBuilderCache.ReturnAndFree(sb);
                                }
                                else
                                {
                                    foreach (var o in objs)
                                    {
                                        if (!isComplexType(o))
                                        {
                                            values.Add(GetScalarText(o, options.Defaults));
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
                                    sb.AppendLine(writeCaption != null ? $"|-{"".PadRight(valuesSize,'-')}-|" : "|-|");

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
                    Type elType = null;
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

                    string key1 = null, key2 = null; 
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
                            to.Add(new Dictionary<string, object> { {keyGetter(kvp).ConvertTo<string>(), valueGetter(kvp) } });
                        }
                        return to;
                    }
                    
                    return target.ToObjectDictionary();
                }
            }

            return target;
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

        private static string GetScalarText(object target, DefaultScripts defaults)
        {
            if (target == null || target.ToString() == string.Empty)
                return string.Empty;

            if (target is string s)
                return s;

            if (target is decimal dec)
            {
                var isMoney = dec == Math.Floor(dec * 100);
                if (isMoney)
                    return defaults?.currency(dec) ?? dec.ToString(defaults.GetDefaultCulture());
            }

            if (target.GetType().IsNumericType() || target is bool)
                return target.ToString();

            if (target is DateTime d)
                return defaults?.dateFormat(d) ?? d.ToString(defaults.GetDefaultCulture());

            if (target is TimeSpan t)
                return defaults?.timeFormat(t) ?? t.ToString();

            return target.ToString() ?? "";
        }

        private static bool isComplexType(object first)
        {
            return !(first == null || first is string || first.GetType().IsValueType);
        }
    }
}