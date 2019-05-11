using System.Collections.Generic;
using System.Linq;
using ServiceStack.Script;

namespace ServiceStack
{
    // ReSharper disable InconsistentNaming

    public class BootstrapScripts : ScriptMethods
    {
        public IRawString validationSummary(ScriptScopeContext scope) =>
            validationSummary(scope, null, null);

        public IRawString validationSummary(ScriptScopeContext scope, object exceptFields) =>
            validationSummary(scope, exceptFields, null);
        
        
        public IRawString validationSummary(ScriptScopeContext scope, object exceptFields, object htmlAttrs)
        {
            var ssFilters = Context.GetServiceStackFilters();
            if (ssFilters == null)
                return null;

            var errorSummaryMsg = exceptFields != null
                ? ssFilters.errorResponseExcept(scope, exceptFields) // string | string[]
                : ssFilters.errorResponseSummary(scope);

            if (string.IsNullOrEmpty(errorSummaryMsg))
                return null;

            var divAttrs = htmlAttrs.AssertOptions(nameof(validationSummary));
            if (!divAttrs.ContainsKey("class") && !divAttrs.ContainsKey("className"))
                divAttrs["class"] = "alert alert-danger";
            
            return Context.HtmlMethods.htmlDiv(errorSummaryMsg, divAttrs);
        }

        public IRawString formTextarea(ScriptScopeContext scope, object args) => formTextarea(scope, args, null);
        public IRawString formTextarea(ScriptScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "textarea", inputOptions);
        
        public IRawString formSelect(ScriptScopeContext scope, object args) => formSelect(scope, args, null);
        public IRawString formSelect(ScriptScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "select", inputOptions);
        
        public IRawString formInput(ScriptScopeContext scope, object args) => formInput(scope, args, null);

        public IRawString formInput(ScriptScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "input", inputOptions);

        public IRawString formControl(ScriptScopeContext scope, object inputAttrs, string tagName, object inputOptions) => 
            ViewUtils.FormControl(Context.GetServiceStackFilters().req(scope), inputAttrs.AssertOptions(nameof(formControl)), tagName, 
                (inputOptions as IEnumerable<KeyValuePair<string, object>>).FromObjectDictionary<InputOptions>()).ToRawString();

        NavOptions ToNavOptions(ScriptScopeContext scope, Dictionary<string, object> options)
        {
            var navOptions = new NavOptions();            
            if (options != null)
            {
                if (options.TryGetValue("attributes", out var oAttributes))
                    navOptions.Attributes = ViewUtils.ToStrings(nameof(ToNavOptions), oAttributes).ToHashSet();
                if (options.TryGetValue("active", out var oActive))
                    navOptions.ActivePath = (string)oActive;
                if (options.TryGetValue("prefix", out var oPrefix))
                    navOptions.HrefPrefix = (string)oPrefix;
                if (options.TryGetValue("navClass", out var oNavClass))
                    navOptions.NavClass = (string) oNavClass;
                if (options.TryGetValue("navLinkClass", out var oNavLinkClass))
                    navOptions.NavLinkClass = (string) oNavLinkClass;
                if (options.TryGetValue("childNavItemClass", out var oChildNavItemClass))
                    navOptions.ChildNavItemClass = (string) oChildNavItemClass;
                if (options.TryGetValue("childNavLinkClass", out var oChildNavLinkClass))
                    navOptions.ChildNavLinkClass = (string) oChildNavLinkClass;
                if (options.TryGetValue("childNavMenuClass", out var oChildNavMenuClass))
                    navOptions.ChildNavMenuClass = (string) oChildNavMenuClass;
                if (options.TryGetValue("childNavMenuItemClass", out var oChildNavMenuItemClass))
                    navOptions.ChildNavMenuItemClass = (string) oChildNavMenuItemClass;
            }

            if (navOptions.ActivePath == null)
                navOptions.ActivePath = scope.GetValue("PathInfo")?.ToString();
            if (navOptions.Attributes == null)
                navOptions.Attributes = Context.GetServiceStackFilters().req(scope).GetUserAttributes();

            return navOptions;
        }

        public IRawString nav(ScriptScopeContext scope) => nav(scope, ViewUtils.NavItems);
        public IRawString nav(ScriptScopeContext scope, List<NavItem> navItems) => nav(scope, navItems, null);
        public IRawString nav(ScriptScopeContext scope, List<NavItem> navItems, Dictionary<string, object> options) => 
            ViewUtils.Nav(navItems, ToNavOptions(scope, options)).ToRawString();

        public IRawString navbar(ScriptScopeContext scope) => navbar(scope, ViewUtils.NavItems);
        public IRawString navbar(ScriptScopeContext scope, List<NavItem> navItems) => navbar(scope, navItems, null);
        public IRawString navbar(ScriptScopeContext scope, List<NavItem> navItems, Dictionary<string, object> options) => 
            ViewUtils.Nav(navItems, ToNavOptions(scope, options).NavBar()).ToRawString();

        public IRawString navLink(ScriptScopeContext scope, NavItem navItem) => navLink(scope, navItem, null);
        public IRawString navLink(ScriptScopeContext scope, NavItem navItem, Dictionary<string, object> options) =>
            ViewUtils.NavLink(navItem, ToNavOptions(scope, options)).ToRawString();

        public IRawString navLinkButtons(ScriptScopeContext scope) => navLinkButtons(scope, ViewUtils.NavItems);
        public IRawString navLinkButtons(ScriptScopeContext scope, List<NavItem> navItems) => navLinkButtons(scope, navItems, null);
        public IRawString navLinkButtons(ScriptScopeContext scope, List<NavItem> navItems, Dictionary<string, object> options) => 
            ViewUtils.NavLinkButtons(navItems, ToNavOptions(scope, options).NavLinkButtons()).ToRawString();
    }
}