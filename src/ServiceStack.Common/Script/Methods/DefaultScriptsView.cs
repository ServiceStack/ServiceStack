using System.Collections.Generic;

namespace ServiceStack.Script
{
    public partial class DefaultScripts
    {
        public List<NavItem> navItems() => ViewUtils.NavItems;

        public List<NavItem> navItems(string key) => ViewUtils.NavItemsMap.TryGetValue(key, out var items)
            ? items
            : null;
    }
}