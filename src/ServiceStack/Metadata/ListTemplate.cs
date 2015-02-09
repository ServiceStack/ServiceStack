using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Metadata
{
    internal class ListTemplate
    {
        public string Title { get; set; }
        public IList<string> ListItems { get; set; }
        public IDictionary<string, string> ListItemsMap { get; set; }
        public IDictionary<int, string> ListItemsIntMap { get; set; }
        public Func<string, string> ForEachListItem { get; set; }
        public string ListItemTemplate { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Title))
            {
                sb.AppendFormat("<h3>{0}</h3>", Title);
            }
            sb.Append("<ul>");
            if (ListItemTemplate != null)
            {
                if (ListItems != null)
                {
                    foreach (var item in ListItems)
                    {
                        sb.AppendFormat(ListItemTemplate, item, item);
                    }
                }
                if (ListItemsMap != null)
                {
                    foreach (var listItem in ListItemsMap)
                    {
                        sb.AppendFormat(ListItemTemplate, listItem.Key, listItem.Value);
                    }
                }
                if (ListItemsIntMap != null)
                {
                    foreach (var listItem in ListItemsIntMap)
                    {
                        sb.AppendFormat(ListItemTemplate, listItem.Key, listItem.Value);
                    }
                }
            }
            if (this.ForEachListItem != null)
            {
                if (ListItems != null)
                {
                    foreach (var item in ListItems)
                    {
                        sb.Append(ForEachListItem(item));
                    }
                }
            }
            sb.Append("</ul>");
            return sb.ToString();
        }
    }
}