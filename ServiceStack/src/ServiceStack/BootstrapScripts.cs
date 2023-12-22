using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Script;

namespace ServiceStack;

// ReSharper disable InconsistentNaming
public class BootstrapScripts : ScriptMethods
{
    public IRawString validationSummary(ScriptScopeContext scope) =>
        validationSummary(scope, null, null);

    public IRawString validationSummary(ScriptScopeContext scope, IEnumerable exceptFields) =>
        validationSummary(scope, exceptFields, null);
        
        
    public IRawString validationSummary(ScriptScopeContext scope, IEnumerable exceptFields, object htmlAttrs)
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

    public IRawString ValidationSuccess(ScriptScopeContext scope, string message) => ValidationSuccess(scope, message, null);
    public IRawString ValidationSuccess(ScriptScopeContext scope, string message, Dictionary<string,object> divAttrs)
    {
        var errorStatus = scope.GetErrorStatus();
        if (message == null 
            || errorStatus != null
            || scope.GetRequest().Verb == HttpMethods.Get)
            return null; 

        return ViewUtils.ValidationSuccess(message, divAttrs).ToRawString();
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
        ViewUtils.FormControl(scope.GetRequest(), inputAttrs.AssertOptions(nameof(formControl)), tagName, 
            (inputOptions as IEnumerable<KeyValuePair<string, object>>).FromObjectDictionary<InputOptions>()).ToRawString();

    NavOptions ToNavOptions(ScriptScopeContext scope, Dictionary<string, object> options)
    {
        var navOptions = new NavOptions();
        if (options != null)
        {
            if (options.TryGetValue("attributes", out var oAttributes))
                navOptions.Attributes = ViewUtils.ToStrings(nameof(ToNavOptions), oAttributes).ToSet();
            if (options.TryGetValue("activePath", out var oActive))
                navOptions.ActivePath = (string)oActive;
            if (options.TryGetValue("baseHref", out var oBaseHref))
                navOptions.BaseHref = (string)oBaseHref;
            if (options.TryGetValue("navClass", out var oNavClass))
                navOptions.NavClass = (string) oNavClass;
            if (options.TryGetValue("navItemClass", out var oNavItemClass))
                navOptions.NavItemClass = (string) oNavItemClass;
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
            navOptions.Attributes = scope.GetRequest().GetUserAttributes();
        if (navOptions.BaseHref == null)
        {
            var pathBase = HostContext.Config.PathBase;
            if (!string.IsNullOrEmpty(pathBase))
                navOptions.BaseHref = pathBase;
        }

        return navOptions;
    }

    public IRawString nav(ScriptScopeContext scope) => nav(scope, ViewUtils.NavItems);
    public IRawString nav(ScriptScopeContext scope, List<NavItem> navItems) => nav(scope, navItems, null);
    public IRawString nav(ScriptScopeContext scope, List<NavItem> navItems, Dictionary<string, object> options) => 
        ViewUtils.Nav(navItems, ToNavOptions(scope, options).ForNav()).ToRawString();

    public IRawString navbar(ScriptScopeContext scope) => navbar(scope, ViewUtils.NavItems);
    public IRawString navbar(ScriptScopeContext scope, List<NavItem> navItems) => navbar(scope, navItems, null);
    public IRawString navbar(ScriptScopeContext scope, List<NavItem> navItems, Dictionary<string, object> options) => 
        ViewUtils.Nav(navItems, ToNavOptions(scope, options).ForNavbar()).ToRawString();

    public IRawString navLink(ScriptScopeContext scope, NavItem navItem) => navLink(scope, navItem, null);
    public IRawString navLink(ScriptScopeContext scope, NavItem navItem, Dictionary<string, object> options) =>
        ViewUtils.NavLink(navItem, ToNavOptions(scope, options).ForNavLink()).ToRawString();

    public IRawString navButtonGroup(ScriptScopeContext scope) => navButtonGroup(scope, ViewUtils.NavItems);
    public IRawString navButtonGroup(ScriptScopeContext scope, List<NavItem> navItems) => navButtonGroup(scope, navItems, null);
    public IRawString navButtonGroup(ScriptScopeContext scope, List<NavItem> navItems, Dictionary<string, object> options) => 
        ViewUtils.NavButtonGroup(navItems, ToNavOptions(scope, options).ForNavButtonGroup()).ToRawString();
}