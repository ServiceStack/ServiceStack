using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    public class MarkdownTable
    {
        public bool IncludeHeaders { get; set; } = true;
        public bool IncludeRowNumbers { get; set; } = true;
        public string Caption { get; set; }
        public List<string> Headers { get; } = new List<string>();
        public List<List<string>> Rows { get; } = new List<List<string>>();
        
        public string Render()
        {
            var sb = StringBuilderCache.Allocate();

            var offset = IncludeRowNumbers ? -1 : 0;
            var columnCount = Headers.Count - offset;
            var rowLengths = new int[columnCount];
            var i=0;
            if (IncludeRowNumbers)
                rowLengths[i++] = Rows.Count.ToString().Length;

            for (; i < columnCount; i++)
            {
                rowLengths[i] = Headers[i + offset].Length;

                foreach (var row in Rows)
                {
                    var rowLen = row[i + offset]?.Length ?? 0;
                    if (rowLen > rowLengths[i])
                        rowLengths[i] = rowLen;
                }
            }

            if (!string.IsNullOrEmpty(Caption))
                sb.AppendLine(" " + Caption);

            if (IncludeHeaders)
            {
                for (i = 0; i < columnCount; i++)
                {
                    if (IncludeRowNumbers)
                    {
                        sb.Append("| ")
                            .Append("#".PadRight(rowLengths[i], ' '))
                            .Append(" |");
                    }
                    
                    var header = Headers[i + offset];
                    sb.Append("| ")
                        .Append(header.PadRight(rowLengths[i], ' '))
                        .Append(" |");
                }
                sb.AppendLine();

                for (i = 0; i < columnCount; i++)
                {
                    sb.Append("|-")
                        .Append("".PadRight(rowLengths[i], '-'))
                        .Append("-|");
                }
                sb.AppendLine();
            }

            for (var rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
            {
                var row = Rows[rowIndex];
                for (i = 0; i < columnCount; i++)
                {
                    if (IncludeRowNumbers)
                    {
                        sb.Append("| ")
                            .Append($"{rowIndex}".PadRight(rowLengths[i], ' '))
                            .Append(" |");
                    }
                    
                    var field = row[i + offset];
                    sb.Append("| ")
                        .Append(field.PadRight(rowLengths[i], ' '))
                        .Append(" |");                    
                }
                sb.AppendLine();
            }
            sb.AppendLine();

            return StringBuilderCache.ReturnAndFree(sb);
        }
    }

    public partial class TemplateDefaultFilters
    {
        public string textList(TemplateScopeContext scope, object target) => textList(scope, target, null);
        public string textList(TemplateScopeContext scope, object target, object scopeOptions)
        {
            if (target is IDictionary<string, object> single)
                target = new[] { single };
            
            var items = target.AssertEnumerable(nameof(textList));
            var scopedParams = scope.AssertOptions(nameof(textList), scopeOptions);
            var depth = scopedParams.TryGetValue("depth", out object oDepth) ? (int)oDepth : 0;
            scopedParams["depth"] = depth + 1;

            try
            {
                scopedParams.TryGetValue("headerStyle", out object oHeaderStyle);
                scopedParams.TryGetValue("captionIfEmpty", out object captionIfEmpty);
                var headerStyle = oHeaderStyle as string ?? "splitCase";

                List<string> keys = null;
                
                var table = new MarkdownTable();

                foreach (var item in items)
                {
                    if (item is IDictionary<string, object> d)
                    {
                        if (keys == null)
                        {
                            keys = d.Keys.ToList();
                            foreach (var key in keys)
                            {
                                table.Headers.Add(Context.DefaultFilters?.textStyle(key, headerStyle));
                            }
                        }

                        var row = new List<string>();
                        
                        foreach (var key in keys)
                        {
                            var value = d[key];
                            if (value == target) break; // Prevent cyclical deps like 'it' binding
                            
                            if (!isComplexType(value))
                            {
                                row.Add(GetScalarText(value));
                            }
                            else
                            {
                                var cellValue = textDump(scope, value, scopeOptions);
                                row.Add(cellValue);
                            }
                        }
                        table.Rows.Add(row);
                    }
                }

                var isEmpty = table.Rows.Count == 0;
                if (isEmpty && captionIfEmpty == null)
                    return string.Empty;

                scopedParams.TryGetValue("caption", out object caption);
                if (isEmpty)
                    caption = captionIfEmpty;

                if (caption != null && !scopedParams.TryGetValue("hasCaption", out _))
                {
                    table.Caption = caption.ToString();
                    scopedParams["hasCaption"] = true;
                }

                var txt = table.Render();
                return txt;
            }
            finally
            {
                scopedParams["depth"] = depth;
            }
        }

        public string textDump(TemplateScopeContext scope, object target) => textDump(scope, target, null);
        public string textDump(TemplateScopeContext scope, object target, object scopeOptions)
        {
            var scopedParams = scope.AssertOptions(nameof(textDump), scopeOptions);
            var depth = scopedParams.TryGetValue("depth", out object oDepth) ? (int)oDepth : 0;
            scopedParams["depth"] = depth + 1;

            try
            {
                if (!isComplexType(target))
                    return GetScalarText(target);

                scopedParams.TryGetValue("captionIfEmpty", out object captionIfEmpty);
                scopedParams.TryGetValue("headerStyle", out object oHeaderStyle);
                var headerStyle = oHeaderStyle as string ?? "splitCase";

                if (target is IEnumerable e)
                {
                    var objs = e.Map(x => x);

                    var isEmpty = objs.Count == 0;
                    if (isEmpty && captionIfEmpty == null)
                        return string.Empty;

                    var first = !isEmpty ? objs[0] : null;
                    if (first is IDictionary)
                        return textList(scope, target, scopeOptions);

                    scopedParams.TryGetValue("caption", out object caption);
                    if (isEmpty)
                        caption = captionIfEmpty;
                    
                    var sb = StringBuilderCacheAlt.Allocate();

                    if (caption != null && !scopedParams.TryGetValue("hasCaption", out _))
                    {
                        sb.AppendLine(caption.ToString());
                        scopedParams["hasCaption"] = true;
                    }

                    if (!isEmpty)
                    {
                        if (first is KeyValuePair<string, object>)
                        {
                            foreach (var o in objs)
                            {
                                if (o is KeyValuePair<string, object> kvp)
                                {
                                    if (kvp.Value == target) break; // Prevent cyclical deps like 'it' binding

                                    var header = Context.DefaultFilters?.textStyle(kvp.Key, headerStyle) ?? "";
                                    var field = !isComplexType(kvp.Value)
                                        ? GetScalarText(kvp.Value)
                                        : textDump(scope, kvp.Value, scopeOptions);

                                    sb.AppendLine(header);
                                    sb.AppendLine(field);
                                }
                            }
                        }
                        else if (!isComplexType(first))
                        {
                            foreach (var o in objs)
                            {
                                sb.AppendLine(GetScalarText(o));
                            }
                        }
                        else
                        {
                            if (objs.Count > 1)
                            {
                                var rows = objs.Map(x => x.ToObjectDictionary());
                                var list = textList(scope, rows, scopeOptions);
                                sb.AppendLine(list);
                            }
                            else
                            {
                                foreach (var o in objs)
                                {
                                    if (!isComplexType(o))
                                    {
                                        sb.AppendLine(GetScalarText(o));
                                    }
                                    else
                                    {
                                        var body = textDump(scope, o, scopeOptions);
                                        sb.AppendLine(body);
                                    }
                                }
                            }
                        }
                    }

                    return StringBuilderCache.ReturnAndFree(sb);
                }

                return textDump(scope, target.ToObjectDictionary(), scopeOptions);
            }
            finally 
            {
                scopedParams["depth"] = depth;
            }
        }

        private string GetScalarText(object target)
        {
            if (target == null || target.ToString() == string.Empty)
                return string.Empty;

            if (target is string s)
                return s.HtmlEncode();

            if (target is decimal dec)
            {
                var isMoney = dec == Math.Floor(dec * 100);
                if (isMoney)
                    return Context.DefaultFilters?.currency(dec) ?? dec.ToString();
            }

            if (target.GetType().IsNumericType() || target is bool)
                return target.ToString();

            if (target is DateTime d)
                return Context.DefaultFilters?.dateFormat(d) ?? d.ToString();

            if (target is TimeSpan t)
                return Context.DefaultFilters?.timeFormat(t) ?? t.ToString();

            return target.ToString() ?? "";
        }

        private static bool isComplexType(object first)
        {
            return !(first == null || first is string || first.GetType().IsValueType);
        }
        
    }
}