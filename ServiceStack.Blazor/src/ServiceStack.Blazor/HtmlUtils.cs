using System.Collections;
using ServiceStack.Text;

namespace ServiceStack.Blazor;

public class HtmlDumpOptions
{
    public static HtmlDumpOptions Default { get; set; } = new();

    public string? Id { get; set; }
    public string? ClassName { get; set; }
    public string? ChildClass { get; set; }

    public TextStyle HeaderStyle { get; set; }
    public string? HeaderTag { get; set; }

    public string? Caption { get; set; }
    public string? CaptionIfEmpty { get; set; }

    public string[]? Headers { get; set; }

    public string? Display { get; set; }
    internal int Depth { get; set; }
    internal int ChildDepth { get; set; } = 1;
    internal bool HasCaption { get; set; }
}

public static class HtmlUtils
{
    public static string TableClass { get; set; } = "table table-striped";

    public static string HtmlEncode(this string s) => System.Net.WebUtility.HtmlEncode(s);
    public static string AsString(this object str) => str is IRawString r ? r.ToRawString() : str?.ToString() ?? "";

    public static string GetScalarHtml(object? target)
    {
        if (target == null || target.ToString() == string.Empty)
            return string.Empty;

        if (target is string s)
            return TextUtils.FormatString(s).HtmlEncode();

        if (target is decimal dec)
        {
            var isMoney = dec == Math.Floor(dec * 100);
            if (isMoney)
                return TextUtils.FormatCurrency(dec);
        }

        if (target.GetType().IsNumericType() || target is bool)
            return target.ToString() ?? "";

        if (target is DateTime d)
            return TextUtils.FormatDate(d);

        if (target is TimeSpan t)
            return TextUtils.FormatTime(t);

        return (target.ToString() ?? "").HtmlEncode();
    }

    public static string HtmlDump(object target) => HtmlDump(target, HtmlDumpOptions.Default);
    public static string HtmlDump(object target, HtmlDumpOptions options)
    {
        var depth = options.Depth;
        var childDepth = options.ChildDepth;
        options.Depth += 1;

        try
        {
            target = TextUtils.ConvertDumpType(target);

            if (!TextUtils.IsComplexType(target?.GetType()))
                return GetScalarHtml(target);

            var parentClass = options.ClassName;
            var childClass = options.ChildClass;
            var className = (depth < childDepth ? parentClass : childClass ?? parentClass) ?? TableClass;

            var headerStyle = options.HeaderStyle;
            var headerTag = options.HeaderTag ?? "th";

            if (target is IEnumerable e)
            {
                var objs = e.Cast<object>().Select(x => x).ToList();

                var isEmpty = objs.Count == 0;
                if (isEmpty && options.CaptionIfEmpty == null)
                    return string.Empty;

                var first = !isEmpty ? objs[0] : null;
                if (first is IDictionary && objs.Count > 1 && options.Display != "table")
                    return HtmlList(objs, options);

                var sb = StringBuilderCacheAlt.Allocate();

                sb.Append("<table");

                if (options.Id != null)
                    sb.Append(" id=\"").Append(options.Id).Append('"');
                if (!string.IsNullOrEmpty(className))
                    sb.Append(" class=\"").Append(className).Append('"');

                sb.Append('>');

                var caption = options.Caption;
                if (isEmpty)
                    caption = options.CaptionIfEmpty;

                var holdCaption = options.HasCaption;
                if (caption != null && !options.HasCaption)
                {
                    sb.Append("<caption>").Append(caption.HtmlEncode()).Append("</caption>");
                    options.HasCaption = true;
                }

                if (!isEmpty)
                {
                    sb.Append("<tbody>");

                    if (first is KeyValuePair<string, object>)
                    {
                        foreach (var o in objs)
                        {
                            if (o is KeyValuePair<string, object> kvp)
                            {
                                if (kvp.Value == target) break; // Prevent cyclical deps like 'it' binding

                                sb.Append("<tr>");
                                sb.Append('<').Append(headerTag).Append('>');
                                sb.Append(TextUtils.StyleText(kvp.Key, headerStyle)?.HtmlEncode());
                                sb.Append("</").Append(headerTag).Append('>');
                                sb.Append("<td>");
                                if (!TextUtils.IsComplexType(kvp.Value?.GetType()))
                                {
                                    sb.Append(GetScalarHtml(kvp.Value));
                                }
                                else
                                {
                                    var body = HtmlDump(kvp.Value, options);
                                    sb.Append(body.AsString());
                                }
                                sb.Append("</td>");
                                sb.Append("</tr>");
                            }
                        }
                    }
                    else if (!TextUtils.IsComplexType(first?.GetType()))
                    {
                        foreach (var o in objs)
                        {
                            sb.Append("<tr>");
                            sb.Append("<td>");
                            sb.Append(GetScalarHtml(o));
                            sb.Append("</td>");
                            sb.Append("</tr>");
                        }
                    }
                    else
                    {
                        if (objs.Count > 1 || options.Display == "table")
                        {
                            var rows = objs.Select(x => x.ToObjectDictionary()).ToList();
                            StringBuilderCache.Free(sb);
                            options.HasCaption = holdCaption;
                            return HtmlList(rows, options);
                        }
                        else
                        {
                            foreach (var o in objs)
                            {
                                sb.Append("<tr>");

                                if (!TextUtils.IsComplexType(o?.GetType()))
                                {
                                    sb.Append("<td>");
                                    sb.Append(GetScalarHtml(o));
                                    sb.Append("</td>");
                                }
                                else
                                {
                                    sb.Append("<td>");
                                    var body = HtmlDump(o, options);
                                    sb.Append(body.AsString());
                                    sb.Append("</td>");
                                }

                                sb.Append("</tr>");
                            }
                        }
                    }

                    sb.Append("</tbody>");
                }

                sb.Append("</table>");

                var html = StringBuilderCacheAlt.ReturnAndFree(sb);
                return html;
            }

            return HtmlDump(target.ToObjectDictionary(), options);
        }
        finally
        {
            options.Depth = depth;
        }
    }

    public static string HtmlList(IEnumerable items) => HtmlDump(items, HtmlDumpOptions.Default);
    public static string HtmlList(IEnumerable items, HtmlDumpOptions options)
    {
        if (items is IDictionary<string, object> single)
            items = new[] { single };

        var depth = options.Depth;
        var childDepth = options.ChildDepth;
        options.Depth += 1;

        try
        {
            var parentClass = options.ClassName;
            var childClass = options.ChildClass;
            var className = (depth < childDepth ? parentClass : childClass ?? parentClass) ?? TableClass;

            var headerStyle = options.HeaderStyle;
            var headerTag = options.HeaderTag ?? "th";

            var sbHeader = StringBuilderCache.Allocate();
            var sbRows = StringBuilderCacheAlt.Allocate();
            List<string>? keys = null;

            foreach (var item in items)
            {
                if (item is IDictionary<string, object> d)
                {
                    if (keys == null)
                    {
                        keys = options.Headers?.ToList() ?? TextUtils.AllKeysWithDefaultValues(items);
                        sbHeader.Append("<tr>");
                        foreach (var key in keys)
                        {
                            sbHeader.Append('<').Append(headerTag).Append('>');
                            sbHeader.Append(TextUtils.StyleText(key, headerStyle)?.HtmlEncode());
                            sbHeader.Append("</").Append(headerTag).Append('>');
                        }
                        sbHeader.Append("</tr>");
                    }

                    sbRows.Append("<tr>");
                    foreach (var key in keys)
                    {
                        var value = d[key];
                        if (ReferenceEquals(value, items))
                            break; // Prevent cyclical deps like 'it' binding

                        sbRows.Append("<td>");

                        if (!TextUtils.IsComplexType(value?.GetType()))
                        {
                            sbRows.Append(GetScalarHtml(value));
                        }
                        else
                        {
                            var htmlValue = HtmlDump(value!, options);
                            sbRows.Append(htmlValue.AsString());
                        }

                        sbRows.Append("</td>");
                    }
                    sbRows.Append("</tr>");
                }
            }

            var isEmpty = sbRows.Length == 0;
            if (isEmpty && options.CaptionIfEmpty == null)
                return string.Empty;

            var htmlHeaders = StringBuilderCache.ReturnAndFree(sbHeader);
            var htmlRows = StringBuilderCacheAlt.ReturnAndFree(sbRows);

            var sb = StringBuilderCache.Allocate();
            sb.Append("<table");

            if (options.Id != null)
                sb.Append(" id=\"").Append(options.Id).Append('"');
            if (!string.IsNullOrEmpty(className))
                sb.Append(" class=\"").Append(className).Append('"');

            sb.Append('>');

            var caption = options.Caption;
            if (isEmpty)
                caption = options.CaptionIfEmpty;

            if (caption != null && !options.HasCaption)
            {
                sb.Append("<caption>").Append(caption.HtmlEncode()).Append("</caption>");
                options.HasCaption = true;
            }

            if (htmlHeaders.Length > 0)
                sb.Append("<thead>").Append(htmlHeaders).Append("</thead>");
            if (htmlRows.Length > 0)
                sb.Append("<tbody>").Append(htmlRows).Append("</tbody>");

            sb.Append("</table>");

            var html = StringBuilderCache.ReturnAndFree(sb);
            return html;
        }
        finally
        {
            options.Depth = depth;
        }
    }

    public static IReadOnlyDictionary<string, object>? SanitizeAttributes(this IReadOnlyDictionary<string, object>? attrs)
    {
        if (attrs == null) return null;
        var safeAttrs = new Dictionary<string, object>();
        foreach (var attr in attrs)
        {
            if (attr.Key == "@bind" || attr.Key.StartsWith("@bind:"))
                continue;
            safeAttrs[attr.Key] = attr.Value;
        }
        return safeAttrs;
    }
}
