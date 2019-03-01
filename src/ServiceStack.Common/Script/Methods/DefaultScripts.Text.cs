using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.Script
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
                sb.AppendLine(" " + Caption)
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
        public IRawString textList(ScriptScopeContext scope, object target) => textList(scope, target, null);
        public IRawString textList(ScriptScopeContext scope, object target, object scopeOptions)
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

                if (scopedParams.TryGetValue("rowNumbers", out object rowNumbers))
                    table.IncludeRowNumbers = !(rowNumbers is bool b) || b;

                foreach (var item in items)
                {
                    if (item is IDictionary<string, object> d)
                    {
                        if (keys == null)
                        {
                            keys = d.Keys.ToList();
                            foreach (var key in keys)
                            {
                                table.Headers.Add(Context.DefaultMethods?.textStyle(key, headerStyle));
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
                                row.Add(cellValue.ToRawString());
                            }
                        }
                        table.Rows.Add(row);
                    }
                }

                var isEmpty = table.Rows.Count == 0;
                if (isEmpty && captionIfEmpty == null)
                    return RawString.Empty;

                scopedParams.TryGetValue("caption", out object caption);
                if (isEmpty)
                    caption = captionIfEmpty;

                if (caption != null && !scopedParams.TryGetValue("hasCaption", out _))
                {
                    table.Caption = caption.ToString();
                    scopedParams["hasCaption"] = true;
                }

                var txt = table.Render();
                return txt.ToRawString();
            }
            finally
            {
                scopedParams["depth"] = depth;
            }
        }

        public IRawString textDump(ScriptScopeContext scope, object target) => textDump(scope, target, null);
        public IRawString textDump(ScriptScopeContext scope, object target, object scopeOptions)
        {
            var scopedParams = scope.AssertOptions(nameof(textDump), scopeOptions);
            var depth = scopedParams.TryGetValue("depth", out object oDepth) ? (int)oDepth : 0;
            scopedParams["depth"] = depth + 1;

            try
            {
                if (!isComplexType(target))
                    return GetScalarText(target).ToRawString();

                scopedParams.TryGetValue("captionIfEmpty", out object captionIfEmpty);
                scopedParams.TryGetValue("headerStyle", out object oHeaderStyle);
                var headerStyle = oHeaderStyle as string ?? "splitCase";

                if (target is IEnumerable e)
                {
                    var objs = e.Map(x => x);

                    var isEmpty = objs.Count == 0;
                    if (isEmpty && captionIfEmpty == null)
                        return RawString.Empty;

                    var first = !isEmpty ? objs[0] : null;
                    if (first is IDictionary)
                        return textList(scope, target, scopeOptions);

                    scopedParams.TryGetValue("caption", out object caption);
                    if (isEmpty)
                        caption = captionIfEmpty;
                    
                    var sb = StringBuilderCacheAlt.Allocate();

                    string writeCaption = null; 
                    if (caption != null && !scopedParams.TryGetValue("hasCaption", out _))
                    {
                        writeCaption = caption.ToString();
                        scopedParams["hasCaption"] = true;
                    }

                    if (!isEmpty)
                    {
                        var keys = new List<string>();
                        var values = new List<string>();

                        if (first is KeyValuePair<string, object>)
                        {
                            foreach (var o in objs)
                            {
                                if (o is KeyValuePair<string, object> kvp)
                                {
                                    if (kvp.Value == target) break; // Prevent cyclical deps like 'it' binding

                                    keys.Add(Context.DefaultMethods?.textStyle(kvp.Key, headerStyle) ?? "");
                                    
                                    var field = !isComplexType(kvp.Value)
                                        ? GetScalarText(kvp.Value)
                                        : textDump(scope, kvp.Value, scopeOptions).ToRawString();
                                    
                                    values.Add(field);
                                }
                            }

                            var keySize = keys.Max(x => x.Length);
                            var valuesSize = values.Max(x => x.Length);

                            sb.AppendLine(writeCaption != null 
                                ? $"| {writeCaption.PadRight(keySize + valuesSize + 2, ' ')} ||" 
                                : $"|||");
                            sb.AppendLine(writeCaption != null 
                                ? $"|-{"".PadRight(keySize,'-')}-|-{"".PadRight(valuesSize,'-')}-|" 
                                : "|-|-|");
                            
                            for (var i = 0; i < keys.Count; i++)
                            {
                                sb.Append("| ")
                                  .Append(keys[i].PadRight(keySize, ' '))
                                  .Append(" | ")
                                  .Append(values[i].PadRight(valuesSize, ' '))
                                  .Append(" |")
                                  .AppendLine();
                            }
                        }
                        else
                        {
                            if (!isComplexType(first))
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
                                        sb.AppendLine(writeCaption);
                            
                                    var rows = objs.Map(x => x.ToObjectDictionary());
                                    var list = textList(scope, rows, scopeOptions).ToRawString();
                                    sb.AppendLine(list);
                                }
                                else
                                {
                                    foreach (var o in objs)
                                    {
                                        if (!isComplexType(o))
                                        {
                                            values.Add(GetScalarText(o));
                                        }
                                        else
                                        {
                                            var body = textDump(scope, o, scopeOptions).ToRawString();
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

                    return StringBuilderCache.ReturnAndFree(sb).ToRawString();
                }

                return textDump(scope, target.ToObjectDictionary(), scopeOptions);
            }
            finally 
            {
                scopedParams["depth"] = depth;
            }
        }

        private int MaxLineLength(string s)
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

        private string GetScalarText(object target)
        {
            if (target == null || target.ToString() == string.Empty)
                return string.Empty;

            if (target is string s)
                return s;

            if (target is decimal dec)
            {
                var isMoney = dec == Math.Floor(dec * 100);
                if (isMoney)
                    return Context.DefaultMethods?.currency(dec) ?? dec.ToString();
            }

            if (target.GetType().IsNumericType() || target is bool)
                return target.ToString();

            if (target is DateTime d)
                return Context.DefaultMethods?.dateFormat(d) ?? d.ToString();

            if (target is TimeSpan t)
                return Context.DefaultMethods?.timeFormat(t) ?? t.ToString();

            return target.ToString() ?? "";
        }

        private static bool isComplexType(object first)
        {
            return !(first == null || first is string || first.GetType().IsValueType);
        }
    }
}