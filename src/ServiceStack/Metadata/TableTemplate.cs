using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Metadata
{
    internal class TableTemplate
    {
        public string Title { get; set; }
        public IList<string> Items { get; set; }
        public IDictionary<string, string> ItemsMap { get; set; }
        public IDictionary<int, string> ItemsIntMap { get; set; }
        public Func<string, string> ForEachItem { get; set; }
        public string ItemTemplate { get; set; }

        public override string ToString()
        {
            var sb = StringBuilderCache.Allocate();
            if (!string.IsNullOrEmpty(Title))
            {
                sb.Append($"<h3>{Title}</h3>");
            }

            sb.Append("<table>");
            sb.Append("<tbody>");
            if (ItemTemplate != null)
            {
                if (Items != null)
                {
                    foreach (var item in Items)
                    {
                        sb.AppendFormat(ItemTemplate, item, item);
                    }
                }
                if (ItemsMap != null)
                {
                    foreach (var listItem in ItemsMap)
                    {
                        sb.AppendFormat(ItemTemplate, listItem.Key, listItem.Value);
                    }
                }
                if (ItemsIntMap != null)
                {
                    foreach (var listItem in ItemsIntMap)
                    {
                        sb.AppendFormat(ItemTemplate, listItem.Key, listItem.Value);
                    }
                }
            }
            if (this.ForEachItem != null)
            {
                if (Items != null)
                {
                    foreach (var item in Items)
                    {
                        sb.Append(ForEachItem(item));
                    }
                }
            }
            sb.Append("</tbody>");
            sb.Append("</table>");

            return StringBuilderCache.ReturnAndFree(sb);
        }
    }
}