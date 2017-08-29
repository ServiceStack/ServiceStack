using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public class TemplateHtmlFilters : TemplateFilter
    {
        public IRawString htmlList(TemplateScopeContext scope, object target) => htmlList(scope, target, null);
        public IRawString htmlList(TemplateScopeContext scope, object target, object scopeOptions)
        {
            if (target is IDictionary<string, object> single)
                target = new[] { single };
            
            var items = target.AssertEnumerable(nameof(htmlList));
            var scopedParams = scope.AssertOptions(nameof(htmlList), scopeOptions);
            var depth = scopedParams.TryGetValue("depth", out object oDepth) ? (int)oDepth : 0;
            var childDepth = scopedParams.TryGetValue("childDepth", out object oChildDepth) ? oChildDepth.ConvertTo<int>() : 1;
            scopedParams["depth"] = depth + 1;

            try
            {
                scopedParams.TryGetValue("className", out object parentClass);
                scopedParams.TryGetValue("childClass", out object childClass);
                var className = ((depth < childDepth ? parentClass : childClass ?? parentClass) 
                                 ?? Context.Args[TemplateConstants.DefaultTableClassName]).ToString();

                scopedParams.TryGetValue("headerStyle", out object oHeaderStyle);
                scopedParams.TryGetValue("headerTag", out object oHeaderTag);
                scopedParams.TryGetValue("captionIfEmpty", out object captionIfEmpty);
                var headerTag = oHeaderTag as string ?? "th";
                var headerStyle = oHeaderStyle as string ?? "splitCase";

                var sbHeader = StringBuilderCache.Allocate();
                var sbRows = StringBuilderCacheAlt.Allocate();
                List<string> keys = null;

                foreach (var item in items)
                {
                    if (item is IDictionary<string, object> d)
                    {
                        if (keys == null)
                        {
                            keys = d.Keys.ToList();
                            sbHeader.Append("<tr>");
                            foreach (var key in keys)
                            {
                                sbHeader.Append('<').Append(headerTag).Append('>');
                                sbHeader.Append(Context.DefaultFilters?.textStyle(key, headerStyle)?.HtmlEncode());
                                sbHeader.Append("</").Append(headerTag).Append('>');
                            }
                            sbHeader.Append("</tr>");
                        }

                        sbRows.Append("<tr>");
                        foreach (var key in keys)
                        {
                            var value = d[key];
                            sbRows.Append("<td>");

                            if (!isComplexType(value))
                            {
                                sbRows.Append(GetScalarHtml(value));
                            }
                            else
                            {
                                var htmlValue = htmlDump(scope, value, scopeOptions);
                                sbRows.Append(htmlValue.ToRawString());
                            }

                            sbRows.Append("</td>");
                        }
                        sbRows.Append("</tr>");
                    }
                }

                var isEmpty = sbRows.Length == 0;
                if (isEmpty && captionIfEmpty == null)
                    return RawString.Empty;

                var htmlHeaders = StringBuilderCache.ReturnAndFree(sbHeader);
                var htmlRows = StringBuilderCacheAlt.ReturnAndFree(sbRows);

                var sb = StringBuilderCache.Allocate();
                sb.Append("<table");

                if (scopedParams.TryGetValue("id", out object id))
                    sb.Append(" id=\"").Append(id).Append("\"");
                if (!string.IsNullOrEmpty(className))
                    sb.Append(" class=\"").Append(className).Append("\"");

                sb.Append(">");

                scopedParams.TryGetValue("caption", out object caption);
                if (isEmpty)
                    caption = captionIfEmpty;

                if (caption != null && !scopedParams.TryGetValue("hasCaption", out _))
                {
                    sb.Append("<caption>").Append(caption.ToString().HtmlEncode()).Append("</caption>");
                    scopedParams["hasCaption"] = true;
                }

                if (htmlHeaders.Length > 0)
                    sb.Append("<thead>").Append(htmlHeaders).Append("</thead>");
                if (htmlRows.Length > 0)
                    sb.Append("<tbody>").Append(htmlRows).Append("</tbody>");

                sb.Append("</table>");

                var html = StringBuilderCache.ReturnAndFree(sb);
                return html.ToRawString();
            }
            finally
            {
                scopedParams["depth"] = depth;
            }
        }

        public IRawString htmlDump(TemplateScopeContext scope, object target) => htmlDump(scope, target, null);
        public IRawString htmlDump(TemplateScopeContext scope, object target, object scopeOptions)
        {
            var scopedParams = scope.AssertOptions(nameof(htmlDump), scopeOptions);
            var depth = scopedParams.TryGetValue("depth", out object oDepth) ? (int)oDepth : 0;
            var childDepth = scopedParams.TryGetValue("childDepth", out object oChildDepth) ? oChildDepth.ConvertTo<int>() : 1;
            scopedParams["depth"] = depth + 1;

            try
            {
                if (!isComplexType(target))
                    return GetScalarHtml(target).ToRawString();

                scopedParams.TryGetValue("captionIfEmpty", out object captionIfEmpty);
                scopedParams.TryGetValue("headerStyle", out object oHeaderStyle);
                scopedParams.TryGetValue("className", out object parentClass);
                scopedParams.TryGetValue("childClass", out object childClass);
                var headerStyle = oHeaderStyle as string ?? "splitCase";
                var className = ((depth < childDepth ? parentClass : childClass ?? parentClass) 
                                 ?? Context.Args[TemplateConstants.DefaultTableClassName]).ToString();

                if (target is IEnumerable e)
                {
                    var objs = e.Map(x => x);

                    var isEmpty = objs.Count == 0;
                    if (isEmpty && captionIfEmpty == null)
                        return RawString.Empty;

                    var first = !isEmpty ? objs[0] : null;
                    if (first is IDictionary)
                        return htmlList(scope, target, scopeOptions);

                    var sb = StringBuilderCacheAlt.Allocate();

                    sb.Append("<table");

                    if (scopedParams.TryGetValue("id", out object id))
                        sb.Append(" id=\"").Append(id).Append("\"");
                    
                    sb.Append(" class=\"").Append(className).Append("\"");

                    sb.Append(">");

                    scopedParams.TryGetValue("caption", out object caption);
                    if (isEmpty)
                        caption = captionIfEmpty;

                    if (caption != null && !scopedParams.TryGetValue("hasCaption", out _))
                    {
                        sb.Append("<caption>").Append(caption.ToString().HtmlEncode()).Append("</caption>");
                        scopedParams["hasCaption"] = true;
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
                                    sb.Append("<tr>");
                                    sb.Append("<th>");
                                    sb.Append(Context.DefaultFilters?.textStyle(kvp.Key, headerStyle)?.HtmlEncode());
                                    sb.Append("</th>");
                                    sb.Append("<td>");
                                    if (!isComplexType(kvp.Value))
                                    {
                                        sb.Append(GetScalarHtml(kvp.Value));
                                    }
                                    else
                                    {
                                        var body = htmlDump(scope, kvp.Value, scopeOptions);
                                        sb.Append(body.ToRawString());
                                    }
                                    sb.Append("</td>");
                                    sb.Append("</tr>");
                                }
                            }
                        }
                        else if (!isComplexType(first))
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
                            if (objs.Count > 1)
                            {
                                var rows = objs.Map(x => x.ToObjectDictionary());
                                sb.Append("<tr>");
                                sb.Append("<td>");
                                var list = htmlList(scope, rows, scopeOptions);
                                sb.Append(list.ToRawString());
                                sb.Append("</td>");
                                sb.Append("</tr>");
                            }
                            else
                            {
                                foreach (var o in objs)
                                {
                                    sb.Append("<tr>");

                                    if (!isComplexType(o))
                                    {
                                        sb.Append("<td>");
                                        sb.Append(GetScalarHtml(o));
                                        sb.Append("</td>");
                                    }
                                    else
                                    {
                                        sb.Append("<td>");
                                        var body = htmlDump(scope, o, scopeOptions);
                                        sb.Append(body.ToRawString());
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
                    return html.ToRawString();
                }

                return htmlDump(scope, target.ToObjectDictionary(), scopeOptions);
            }
            finally 
            {
                scopedParams["depth"] = depth;
            }
        }

        private string GetScalarHtml(object target)
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

            return (target.ToString() ?? "").HtmlEncode();
        }

        private static bool isComplexType(object first)
        {
            return !(first == null || first is string || first.GetType().IsValueType());
        }

        public IRawString htmlError(TemplateScopeContext scope) => htmlError(scope, scope.PageResult.LastFilterError);
        [HandleUnknownValue] public IRawString htmlError(TemplateScopeContext scope, Exception ex) => htmlError(scope, ex, null);
        [HandleUnknownValue] public IRawString htmlError(TemplateScopeContext scope, Exception ex, object options) => 
            Context.DebugMode ? htmlErrorDebug(scope, ex, options) : htmlErrorMessage(ex, options);

        public IRawString htmlErrorMessage(TemplateScopeContext scope) => htmlErrorMessage(scope.PageResult.LastFilterError);
        [HandleUnknownValue] public IRawString htmlErrorMessage(Exception ex) => htmlErrorMessage(ex, null);
        [HandleUnknownValue] public IRawString htmlErrorMessage(Exception ex, object options)
        {
            if (ex == null)
                return RawString.Empty;

            var scopedParams = options as Dictionary<string, object> ?? TypeConstants.EmptyObjectDictionary;
            var className = (scopedParams.TryGetValue("className", out object oClassName) ? oClassName : null) 
                            ?? Context.Args[TemplateConstants.DefaultErrorClassName];
           
            return $"<div class=\"{className}\">{ex.Message}</div>".ToRawString();
        }

        public IRawString htmlErrorDebug(TemplateScopeContext scope) => htmlErrorDebug(scope, scope.PageResult.LastFilterError);
        [HandleUnknownValue] public IRawString htmlErrorDebug(TemplateScopeContext scope, object ex) => 
            htmlErrorDebug(scope, ex as Exception ?? scope.PageResult.LastFilterError, ex as Dictionary<string, object>);
        
        [HandleUnknownValue] 
        public IRawString htmlErrorDebug(TemplateScopeContext scope, Exception ex, object options)
        {
            if (ex == null)
                return RawString.Empty;

            var scopedParams = options as Dictionary<string, object> ?? TypeConstants.EmptyObjectDictionary;
            var className = (scopedParams.TryGetValue("className", out object oClassName) ? oClassName : null) 
                            ?? Context.Args[TemplateConstants.DefaultErrorClassName];
            
            var sb = StringBuilderCache.Allocate();
            sb.Append($"<pre class=\"{className}\">");
            sb.AppendLine($"{ex.GetType().Name}: {ex.Message}");

            var stackTrace = scope.Context.DefaultFilters.lastErrorStackTrace(scope);
            if (!string.IsNullOrEmpty(stackTrace))
            {
                sb.AppendLine();
                sb.AppendLine("StackTrace:");
                sb.AppendLine(stackTrace);
            }
            else if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine();
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace);
            }

            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine("Inner Exceptions:");
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    sb.AppendLine($"{innerEx.GetType().Name}: {innerEx.Message}");
                    if (!string.IsNullOrEmpty(innerEx.StackTrace))
                        sb.AppendLine(innerEx.StackTrace);
                    innerEx = innerEx.InnerException;
                }
            }
            sb.AppendLine("</pre>");
            var html = StringBuilderCache.ReturnAndFree(sb);
            return html.ToRawString();
        }

        public string htmlAttrs(Dictionary<string, object> attrs)
        {
            if (attrs == null || attrs.Count == 0)
                return string.Empty;
            
            var sb = StringBuilderCache.Allocate();

            foreach (var entry in attrs)
            {
                if (entry.Key == "text" || entry.Key == "html") 
                    continue;

                var key = entry.Key == "className"
                    ? "class"
                    : entry.Key == "htmlFor"
                        ? "for"
                        : entry.Key;
                
                sb.Append(' ').Append(key).Append('=').Append('"').Append(entry.Value?.ToString().HtmlEncode()).Append('"');
            }
            
            return sb.ToString();
        }

        [HandleUnknownValue] public IRawString htmlLink(string href) => htmlLink(href, new Dictionary<string, object>{ ["text"] = href });
        [HandleUnknownValue] public IRawString htmlLink(string href, Dictionary<string, object> attrs)
        {
            if (string.IsNullOrEmpty(href))
                return RawString.Empty;

            return htmlA(new Dictionary<string, object>(attrs) { ["href"] = href });
        }

        public IRawString htmlTag(Dictionary<string, object> attrs, string tag)
        {
            var scopedParams = attrs ?? TypeConstants.EmptyObjectDictionary;
            
            var innerHtml = scopedParams.TryGetValue("html", out object oInnerHtml)
                ? oInnerHtml?.ToString()
                : null;

            if (innerHtml == null)
            {
                innerHtml = scopedParams.TryGetValue("text", out object text)
                    ? text?.ToString().HtmlEncode()
                    : null;
            }
            
            var attrString = htmlAttrs(attrs);
            return $"<{tag}{attrString}>{innerHtml}</{tag}>".ToRawString();
        }

        public IRawString htmlDiv(Dictionary<string, object> attrs) => htmlTag(attrs, "div");
        public IRawString htmlSpan(Dictionary<string, object> attrs) => htmlTag(attrs, "span");
        
        public IRawString htmlA(Dictionary<string, object> attrs) => htmlTag(attrs, "a");
        public IRawString htmlImg(Dictionary<string, object> attrs) => htmlTag(attrs, "img");

        public IRawString htmlH1(Dictionary<string, object> attrs) => htmlTag(attrs, "h1");
        public IRawString htmlH2(Dictionary<string, object> attrs) => htmlTag(attrs, "h2");
        public IRawString htmlH3(Dictionary<string, object> attrs) => htmlTag(attrs, "h3");
        public IRawString htmlH4(Dictionary<string, object> attrs) => htmlTag(attrs, "h4");
        public IRawString htmlH5(Dictionary<string, object> attrs) => htmlTag(attrs, "h5");
        public IRawString htmlH6(Dictionary<string, object> attrs) => htmlTag(attrs, "h6");
        
        public IRawString htmlEm(Dictionary<string, object> attrs) => htmlTag(attrs, "em");
        public IRawString htmlb(Dictionary<string, object> attrs) => htmlTag(attrs, "b");
        
        public IRawString htmlTable(Dictionary<string, object> attrs) => htmlTag(attrs, "table");
        public IRawString htmlTr(Dictionary<string, object> attrs) => htmlTag(attrs, "tr");
        public IRawString htmlTh(Dictionary<string, object> attrs) => htmlTag(attrs, "th");
        public IRawString htmlTd(Dictionary<string, object> attrs) => htmlTag(attrs, "td");
        
        public IRawString htmlUl(Dictionary<string, object> attrs) => htmlTag(attrs, "ul");
        public IRawString htmlOl(Dictionary<string, object> attrs) => htmlTag(attrs, "ol");
        public IRawString htmlLi(Dictionary<string, object> attrs) => htmlTag(attrs, "li");
    }
}