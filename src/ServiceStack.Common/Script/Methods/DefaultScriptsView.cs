using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.Script
{
    public partial class DefaultScripts
    {
        public List<NavItem> navItems() => ViewUtils.NavItems;
        public List<NavItem> navItems(string key) => ViewUtils.GetNavItems(key);

        public IRawString cssIncludes(IEnumerable cssFiles) =>
            ViewUtils.CssIncludes(Context.VirtualFiles, ViewUtils.SplitStringList(cssFiles)).ToRawString();

        public IRawString jsIncludes(IEnumerable jsFiles) =>
            ViewUtils.JsIncludes(Context.VirtualFiles, ViewUtils.SplitStringList(jsFiles)).ToRawString();
    }
}