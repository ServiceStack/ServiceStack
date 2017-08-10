using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    public class TemplateHtmlFilters : TemplateFilter
    {
        public IRawString htmltable(TemplateScopeContext scope, object target) => htmltable(scope, target, null);
        public IRawString htmltable(TemplateScopeContext scope, object target, object scopeOptions)
        {
            if (target is IDictionary<string, object> single)
                target = new[] { single };
            
            var items = target.AssertEnumerable(nameof(htmltable));
            var scopedParams = scope.AssertOptions(nameof(htmltable), scopeOptions);

            scopedParams.TryGetValue("headerStyle", out object oHeaderStyle);
            scopedParams.TryGetValue("headerTag", out object oHeaderTag);
            scopedParams.TryGetValue("emptyCaption", out object emptyCaption);
            var headerTag = oHeaderTag as string ?? "th";
            var headerStyle = oHeaderStyle as string ?? "splitCase";

            var sbHeader = StringBuilderCache.Allocate();
            var sbRows = StringBuilderCacheAlt.Allocate();
            List<string> keys = null;

            foreach (var item in items)
            {
                if (item is IDictionary<string,object> d)
                {
                    if (keys == null)
                    {
                        keys = d.Keys.ToList();
                        sbHeader.Append("<tr>");
                        foreach (var key in keys)
                        {
                            sbHeader.Append('<').Append(headerTag).Append('>');
                            sbHeader.Append(TemplateDefaultFilters.Instance.textStyle(key, headerStyle)?.HtmlEncode());
                            sbHeader.Append("</").Append(headerTag).Append('>');
                        }
                        sbHeader.Append("</tr>");
                    }

                    sbRows.Append("<tr>");
                    foreach (var key in keys)
                    {
                        var value = d[key];
                        var encodedValue = value?.ToString()?.HtmlEncode();
                        sbRows.Append("<td>").Append(encodedValue).Append("</td>");
                    }
                    sbRows.Append("</tr>");
                }
            }

            var isEmpty = sbRows.Length == 0;
            if (isEmpty && emptyCaption == null)
                return RawString.Empty;

            var htmlHeaders = StringBuilderCache.ReturnAndFree(sbHeader);
            var htmlRows = StringBuilderCacheAlt.ReturnAndFree(sbRows);

            var sb = StringBuilderCache.Allocate();
            sb.Append("<table");

            if (scopedParams.TryGetValue("id", out object id))
                sb.Append(" id=\"").Append(id).Append("\"");
            if (scopedParams.TryGetValue("className", out object className))
                sb.Append(" class=\"").Append(className).Append("\"");

            sb.Append(">");

            scopedParams.TryGetValue("caption", out object caption);
            if (isEmpty)
                caption = emptyCaption;

            if (caption != null)
                sb.Append("<caption>").Append(caption.ToString().HtmlEncode()).Append("</caption>");
                
            if (htmlHeaders.Length > 0)
                sb.Append("<thead>").Append(htmlHeaders).Append("</thead>");
            if (htmlRows.Length > 0)
                sb.Append("<tbody>").Append(htmlRows).Append("</tbody>");

            sb.Append("</table>");

            var html = StringBuilderCache.ReturnAndFree(sb);
            return html.ToRawString();
        }
    }
}