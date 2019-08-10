using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Html
{
    public static class HtmlExtensions
    {
        public static MvcHtmlString AsRawJson<T>(this T model)
        {
            var json = !Equals(model, default(T)) ? model.ToJson() : "null";
            return MvcHtmlString.Create(json);
        }

        public static MvcHtmlString AsRaw<T>(this T model)
        {
            return MvcHtmlString.Create(
                (model != null ? model : default(T))?.ToString());
        }

        public static IRequest GetRequest(this HtmlHelper html) => html.HttpRequest; 
        
        public static HtmlString ToHtmlString(this string str) => str == null ? MvcHtmlString.Empty : new HtmlString(str);

        public static object GetItem(this HtmlHelper html, string key) =>
            html.GetRequest().GetItem(key);

        public static ResponseStatus GetErrorStatus(this HtmlHelper html) =>
            ViewUtils.GetErrorStatus(html.GetRequest());

        public static bool HasErrorStatus(this HtmlHelper html) =>
            ViewUtils.HasErrorStatus(html.GetRequest());
        
        public static string Form(this HtmlHelper html, string name) => html.GetRequest().FormData[name];
        public static string Query(this HtmlHelper html, string name) => html.GetRequest().QueryString[name];

        public static string FormQuery(this HtmlHelper html, string name) => 
            html.HttpRequest.FormData[name] ?? html.HttpRequest.QueryString[name];

        public static string[] FormQueryValues(this HtmlHelper html, string name) =>
            ViewUtils.FormQueryValues(html.GetRequest(), name);

        public static string FormValue(this HtmlHelper html, string name) => 
            ViewUtils.FormValue(html.GetRequest(), name, null);

        public static string FormValue(this HtmlHelper html, string name, string defaultValue) =>
            ViewUtils.FormValue(html.GetRequest(), name, defaultValue);

        public static string[] FormValues(this HtmlHelper html, string name) =>
            ViewUtils.FormValues(html.GetRequest(), name);

        public static bool FormCheckValue(this HtmlHelper html, string name) =>
            ViewUtils.FormCheckValue(html.GetRequest(), name);

        public static string GetParam(this HtmlHelper html, string name) =>
            ViewUtils.GetParam(html.GetRequest(), name);

        public static string ErrorResponseExcept(this HtmlHelper html, string fieldNames) =>
            ViewUtils.ErrorResponseExcept(html.GetErrorStatus(), fieldNames);

        public static string ErrorResponseExcept(this HtmlHelper html, ICollection<string> fieldNames) =>
            ViewUtils.ErrorResponseExcept(html.GetErrorStatus(), fieldNames);

        public static string ErrorResponseSummary(this HtmlHelper html) =>
            ViewUtils.ErrorResponseSummary(html.GetErrorStatus());

        public static string ErrorResponse(this HtmlHelper html, string fieldName) =>
            ViewUtils.ErrorResponse(html.GetErrorStatus(), fieldName);
        
        public static string UserProfileUrl(this HtmlHelper html) =>
            html.GetRequest().GetSession().GetProfileUrl();


        /// <summary>
        /// Alias for ServiceStack Html.ValidationSummary() with comma-delimited field names 
        /// </summary>
        public static HtmlString ErrorSummary(this HtmlHelper html, string exceptFor) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFor).ToHtmlString();
        public static HtmlString ErrorSummary(this HtmlHelper html) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), null).ToHtmlString();

        public static HtmlString ValidationSummary(this HtmlHelper html, ICollection<string> exceptFields) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, null).ToHtmlString();

        public static HtmlString ValidationSummary(this HtmlHelper html, ICollection<string> exceptFields, Dictionary<string, object> divAttrs) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, divAttrs).ToHtmlString();
        public static HtmlString ValidationSummary(this HtmlHelper html, ICollection<string> exceptFields, object divAttrs) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, divAttrs.ToObjectDictionary()).ToHtmlString();

        public static HtmlString HiddenInputs(this HtmlHelper html, IEnumerable<KeyValuePair<string, string>> kvps) =>
            ViewUtils.HtmlHiddenInputs(kvps.ToObjectDictionary()).ToHtmlString();
        public static HtmlString HiddenInputs(this HtmlHelper html, IEnumerable<KeyValuePair<string, object>> kvps) =>
            ViewUtils.HtmlHiddenInputs(kvps).ToHtmlString();
        public static HtmlString HiddenInputs(this HtmlHelper html, object kvps) =>
            ViewUtils.HtmlHiddenInputs(kvps.ToObjectDictionary()).ToHtmlString();

        public static HtmlString FormTextarea(this HtmlHelper html, object inputAttrs) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "textarea", null);
        public static HtmlString FormTextarea(this HtmlHelper html, Dictionary<string, object> inputAttrs) =>
            FormControl(html, inputAttrs, "textarea", null);
        public static HtmlString FormTextarea(this HtmlHelper html, object inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "textarea", inputOptions);
        public static HtmlString FormTextarea(this HtmlHelper html, Dictionary<string, object> inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs, "textarea", inputOptions);

        public static HtmlString FormSelect(this HtmlHelper html, object inputAttrs) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "select", null);
        public static HtmlString FormSelect(this HtmlHelper html, Dictionary<string, object> inputAttrs) =>
            FormControl(html, inputAttrs, "select", null);
        public static HtmlString FormSelect(this HtmlHelper html, object inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "select", inputOptions);
        public static HtmlString FormSelect(this HtmlHelper html, Dictionary<string, object> inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs, "select", inputOptions);

        public static HtmlString FormInput(this HtmlHelper html, object inputAttrs) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "input", null);
        public static HtmlString FormInput(this HtmlHelper html, Dictionary<string, object> inputAttrs) =>
            FormControl(html, inputAttrs, "input", null);
        public static HtmlString FormInput(this HtmlHelper html, object inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "input", inputOptions);
        public static HtmlString FormInput(this HtmlHelper html, Dictionary<string, object> inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs, "input", inputOptions);

        public static HtmlString FormControl(this HtmlHelper html, object inputAttrs, string tagName, InputOptions inputOptions) =>
            ViewUtils.FormControl(html.GetRequest(), inputAttrs.ToObjectDictionary(), tagName, inputOptions).ToHtmlString();
        public static HtmlString FormControl(this HtmlHelper html, Dictionary<string, object> inputAttrs, string tagName, InputOptions inputOptions) =>
            ViewUtils.FormControl(html.GetRequest(), inputAttrs, tagName, inputOptions).ToHtmlString();

        public static HtmlString BundleJs(this HtmlHelper html, BundleOptions options) => ViewUtils.BundleJs(
            nameof(BundleJs), HostContext.VirtualFileSources, HostContext.VirtualFiles, Minifiers.JavaScript, options).ToHtmlString();

        public static HtmlString BundleCss(this HtmlHelper html, BundleOptions options) => ViewUtils.BundleCss(
            nameof(BundleCss), HostContext.VirtualFileSources, HostContext.VirtualFiles, Minifiers.Css, options).ToHtmlString();

        public static HtmlString BundleHtml(this HtmlHelper html, BundleOptions options) => ViewUtils.BundleHtml(
            nameof(BundleHtml), HostContext.VirtualFileSources, HostContext.VirtualFiles, Minifiers.Html, options).ToHtmlString();

        public static string TextDump(this HtmlHelper  html, object target) => target.TextDump();
        public static string TextDump(this HtmlHelper  html, object target, TextDumpOptions options) => target.TextDump(options);

        public static HtmlString HtmlDump(this HtmlHelper  html, object target) => ViewUtils.HtmlDump(target).ToHtmlString();
        public static HtmlString HtmlDump(this HtmlHelper  html, object target, HtmlDumpOptions options) => 
            ViewUtils.HtmlDump(target,options).ToHtmlString();
        
        public static List<NavItem> GetNavItems(this HtmlHelper html) => ViewUtils.NavItems;
        public static List<NavItem> GetNavItems(this HtmlHelper html, string key) => ViewUtils.GetNavItems(key);

        public static HtmlString Nav(this HtmlHelper html) => html.Nav(ViewUtils.NavItems, null);
        public static HtmlString Nav(this HtmlHelper html, NavOptions options) => html.Nav(ViewUtils.NavItems, options);
        public static HtmlString Nav(this HtmlHelper html, List<NavItem> navItems) => html.Nav(navItems, null);
        public static HtmlString Nav(this HtmlHelper html, List<NavItem> navItems, NavOptions options) =>
            ViewUtils.Nav(navItems, options.ForNav().WithDefaults(html.GetRequest())).ToHtmlString();

        public static HtmlString Navbar(this HtmlHelper html) => html.Navbar(ViewUtils.NavItems, null);
        public static HtmlString Navbar(this HtmlHelper html, NavOptions options) => html.Navbar(ViewUtils.NavItems, options);
        public static HtmlString Navbar(this HtmlHelper html, List<NavItem> navItems) => html.Navbar(navItems, null);
        public static HtmlString Navbar(this HtmlHelper html, List<NavItem> navItems, NavOptions options) =>
            ViewUtils.Nav(navItems, options.ForNavbar().WithDefaults(html.GetRequest())).ToHtmlString();

        public static HtmlString NavLink(this HtmlHelper html, NavItem navItem) => html.NavLink(navItem, null);
        public static HtmlString NavLink(this HtmlHelper html, NavItem navItem, NavOptions options) =>
            ViewUtils.NavLink(navItem, options.ForNavLink().WithDefaults(html.GetRequest())).ToHtmlString();

        public static HtmlString NavButtonGroup(this HtmlHelper html) => html.NavButtonGroup(ViewUtils.NavItems, null);
        public static HtmlString NavButtonGroup(this HtmlHelper html, NavOptions options) => html.NavButtonGroup(ViewUtils.NavItems, options);
        public static HtmlString NavButtonGroup(this HtmlHelper html, List<NavItem> navItems) => html.NavButtonGroup(navItems, null);
        public static HtmlString NavButtonGroup(this HtmlHelper html, List<NavItem> navItems, NavOptions options) =>
            ViewUtils.NavButtonGroup(navItems, options.ForNavButtonGroup().WithDefaults(html.GetRequest())).ToHtmlString();

        public static HtmlString CssIncludes(this HtmlHelper html, params string[] cssFiles) =>
            ViewUtils.CssIncludes(HostContext.VirtualFileSources, cssFiles.ToList()).ToHtmlString();
        public static HtmlString JsIncludes(this HtmlHelper html, params string[] jsFiles) =>
            ViewUtils.CssIncludes(HostContext.VirtualFileSources, jsFiles.ToList()).ToHtmlString();

        public static HtmlString SvgImage(this HtmlHelper html, string name) => Svg.GetImage(name).ToHtmlString();
        public static HtmlString SvgImage(this HtmlHelper html, string name, string fillColor) => Svg.GetImage(name, fillColor).ToHtmlString();

        public static HtmlString SvgDataUri(this HtmlHelper html, string name) => Svg.GetDataUri(name).ToHtmlString();
        public static HtmlString SvgDataUri(this HtmlHelper html, string name, string fillColor) => Svg.GetDataUri(name, fillColor).ToHtmlString();

        public static HtmlString SvgBackgroundImageCss(this HtmlHelper html, string name) => Svg.GetBackgroundImageCss(name).ToHtmlString();
        public static HtmlString SvgBackgroundImageCss(this HtmlHelper html, string name, string fillColor) => Svg.GetBackgroundImageCss(name, fillColor).ToHtmlString();
        public static HtmlString SvgInBackgroundImageCss(this HtmlHelper html, string svg) => Svg.InBackgroundImageCss(svg).ToHtmlString();

        public static HtmlString SvgFill(this HtmlHelper html, string svg, string color) => Svg.Fill(svg, color).ToHtmlString();
        public static string SvgBaseUrl(this HtmlHelper html) => html.GetRequest().ResolveAbsoluteUrl(HostContext.AssertPlugin<SvgFeature>().RoutePath);
    }
}