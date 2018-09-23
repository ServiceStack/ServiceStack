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
                            if (value == target) break; // Prevent cyclical deps like 'it' binding
                            
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
                                    if (kvp.Value == target) break; // Prevent cyclical deps like 'it' binding

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
            return !(first == null || first is string || first.GetType().IsValueType);
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

        public string htmlAttrsList(Dictionary<string, object> attrs)
        {
            if (attrs == null || attrs.Count == 0)
                return string.Empty;
            
            var sb = StringBuilderCache.Allocate();

            var keys = attrs.Keys.OrderBy(x => x);
            foreach (var key in keys)
            {
                if (key == "text" || key == "html") 
                    continue;

                var value = attrs[key];                
                var useKey = key == "className"
                    ? "class"
                    : key == "htmlFor"
                        ? "for"
                        : key;

                if (value is bool boolAttr)
                {
                    if (boolAttr) // only emit attr name if value == true
                    {
                        sb.Append(' ').Append(useKey);
                    }
                }
                else
                {
                    sb.Append(' ').Append(useKey).Append('=').Append('"').Append(value?.ToString().HtmlEncode()).Append('"');
                }
            }
            
            return sb.ToString();
        }

        public IRawString htmlAttrs(object target)
        {
            var attrs = htmlAttrsList(target as Dictionary<string, object>);
            return (attrs.Length > 0 ? attrs : "").ToRawString();
        }

        public string htmlClassList(object target)
        {
            if (target == null)
                return null;
            
            if (target is string clsName)
                return clsName;

            var sb = StringBuilderCache.Allocate();
            if (target is Dictionary<string, object> flags)
            {
                foreach (var entry in flags)
                {
                    if (entry.Value is bool b && b)
                    {
                        if (sb.Length > 0)
                            sb.Append(" ");
                        sb.Append(entry.Key);
                    }
                }
            }
            else if (target is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is string str && str.Length > 0)
                    {
                        if (sb.Length > 0)
                            sb.Append(" ");
                        sb.Append(str);
                    }
                }
            }
            else if (target != null)
            {
                throw new NotSupportedException($"{nameof(htmlClass)} expects a Dictionary, List or String argument but was '{target.GetType().Name}'");
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }

        public IRawString htmlClass(object target)
        {
            var cls = htmlClassList(target);
            return (cls.Length > 0 ? $" class=\"{cls}\"" : "").ToRawString();
        }

        [HandleUnknownValue] public IRawString htmlLink(string href) => htmlLink(href, new Dictionary<string, object> { ["text"] = href });
        [HandleUnknownValue] public IRawString htmlLink(string href, Dictionary<string, object> attrs)
        {
            if (string.IsNullOrEmpty(href))
                return RawString.Empty;

            return htmlA(new Dictionary<string, object>(attrs ?? TypeConstants.EmptyObjectDictionary) { ["href"] = href });
        }

        [HandleUnknownValue] public IRawString htmlImage(string src) => htmlImage(src, null);
        [HandleUnknownValue] public IRawString htmlImage(string src, Dictionary<string, object> attrs)
        {
            if (string.IsNullOrEmpty(src))
                return RawString.Empty;

            return htmlImg(new Dictionary<string, object>(attrs ?? TypeConstants.EmptyObjectDictionary) { ["src"] = src });
        }
       
        public static HashSet<string> VoidElements { get; } = new HashSet<string>
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr"
        };

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
            
            var attrString = htmlAttrsList(attrs);            
            return VoidElements.Contains(tag)
                ? $"<{tag}{attrString}>".ToRawString()
                : $"<{tag}{attrString}>{innerHtml}</{tag}>".ToRawString();
        }

        public IRawString htmlTag(string innerHtml, Dictionary<string, object> attrs, string tag)
        {
            return htmlTag(new Dictionary<string, object>(attrs ?? TypeConstants.EmptyObjectDictionary) { ["html"] = innerHtml }, tag);
        }
 
        public IRawString htmlDiv(Dictionary<string, object> attrs) => htmlTag(attrs, "div");
        public IRawString htmlDiv(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "div");
        public IRawString htmlSpan(Dictionary<string, object> attrs) => htmlTag(attrs, "span");
        public IRawString htmlSpan(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "span");
        
        public IRawString htmlA(Dictionary<string, object> attrs) => htmlTag(attrs, "a");
        public IRawString htmlA(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "a");
        public IRawString htmlImg(Dictionary<string, object> attrs) => htmlTag(attrs, "img");
        public IRawString htmlImg(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "img");
        
        public IRawString htmlH1(Dictionary<string, object> attrs) => htmlTag(attrs, "h1");
        public IRawString htmlH1(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "h1");
        public IRawString htmlH2(Dictionary<string, object> attrs) => htmlTag(attrs, "h2");
        public IRawString htmlH2(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "h2");
        public IRawString htmlH3(Dictionary<string, object> attrs) => htmlTag(attrs, "h3");
        public IRawString htmlH3(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "h3");
        public IRawString htmlH4(Dictionary<string, object> attrs) => htmlTag(attrs, "h4");
        public IRawString htmlH4(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "h4");
        public IRawString htmlH5(Dictionary<string, object> attrs) => htmlTag(attrs, "h5");
        public IRawString htmlH5(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "h5");
        public IRawString htmlH6(Dictionary<string, object> attrs) => htmlTag(attrs, "h6");
        public IRawString htmlH6(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "h6");
        
        public IRawString htmlEm(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "em");
        public IRawString htmlEm(string text) => htmlTag(new Dictionary<string, object>{ ["text"] = text }, "em");
        public IRawString htmlB(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "b");
        public IRawString htmlB(string text) => htmlTag(new Dictionary<string, object>{ ["text"] = text }, "b");
        
        public IRawString htmlUl(Dictionary<string, object> attrs) => htmlTag(attrs, "ul");
        public IRawString htmlUl(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "ul");
        public IRawString htmlOl(Dictionary<string, object> attrs) => htmlTag(attrs, "ol");
        public IRawString htmlOl(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "ol");
        public IRawString htmlLi(Dictionary<string, object> attrs) => htmlTag(attrs, "li");
        public IRawString htmlLi(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "li");
 
        public IRawString htmlTable(Dictionary<string, object> attrs) => htmlTag(attrs, "table");
        public IRawString htmlTable(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "table");
        public IRawString htmlTr(Dictionary<string, object> attrs) => htmlTag(attrs, "tr");
        public IRawString htmlTr(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "tr");
        public IRawString htmlTh(Dictionary<string, object> attrs) => htmlTag(attrs, "th");
        public IRawString htmlTh(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "th");
        public IRawString htmlTd(Dictionary<string, object> attrs) => htmlTag(attrs, "td");
        public IRawString htmlTd(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "td");
        
        public IRawString htmlForm(Dictionary<string, object> attrs) => htmlTag(attrs, "form");
        public IRawString htmlForm(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "form");
        public IRawString htmlLabel(Dictionary<string, object> attrs) => htmlTag(attrs, "label");
        public IRawString htmlLabel(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "label");
        public IRawString htmlInput(Dictionary<string, object> attrs) => htmlTag(attrs, "input");
        public IRawString htmlInput(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "input");
        public IRawString htmlTextArea(Dictionary<string, object> attrs) => htmlTag(attrs, "textarea");
        public IRawString htmlTextArea(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "textarea");
        public IRawString htmlButton(Dictionary<string, object> attrs) => htmlTag(attrs, "button");
        public IRawString htmlButton(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "button");
        public IRawString htmlSelect(Dictionary<string, object> attrs) => htmlTag(attrs, "select");
        public IRawString htmlSelect(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "select");
        public IRawString htmlOption(string innerHtml, Dictionary<string, object> attrs) => htmlTag(innerHtml, attrs, "option");
        public IRawString htmlOption(string text) => htmlTag(new Dictionary<string, object>{ ["text"] = text }, "option");
    }
}