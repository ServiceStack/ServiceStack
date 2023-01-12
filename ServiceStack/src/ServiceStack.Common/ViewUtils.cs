
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// High-level Input options for rendering HTML Input controls
    /// </summary>
    public class InputOptions
    {
        /// <summary>
        /// Display the Control inline 
        /// </summary>
        public bool Inline { get; set; }
        
        /// <summary>
        /// Label for the control
        /// </summary>
        public string Label { get; set; }
        
        /// <summary>
        /// Class for Label
        /// </summary>
        public string LabelClass { get; set; }
        
        /// <summary>
        /// Override the class on the error message (default: invalid-feedback)
        /// </summary>
        public string ErrorClass { get; set; }

        /// <summary>
        /// Small Help Text displayed with the control
        /// </summary>
        public string Help { get; set; }
        
        /// <summary>
        /// Bootstrap Size of the Control: sm, lg
        /// </summary>
        public string Size { get; set; }
        
        /// <summary>
        /// Multiple Value Data Source for Checkboxes, Radio boxes and Select Controls 
        /// </summary>
        public object Values { get; set; }

        /// <summary>
        /// Typed setter of Multi Input Values
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> InputValues
        {
            set => Values = value;
        }

        /// <summary>
        /// Whether to preserve value state after post back
        /// </summary>
        public bool PreserveValue { get; set; } = true;

        /// <summary>
        /// Whether to show Error Message associated with this control
        /// </summary>
        public bool ShowErrors { get; set; } = true;
    }

    /// <summary>
    /// Customize JS/CSS/HTML bundles
    /// </summary>
    public class BundleOptions
    {
        /// <summary>
        /// List of file and directory sources to include in this bundle, directory sources must end in `/`.
        /// Sources can include prefixes to specify which Virtual File System Source to use, options:
        /// 'content:' (ContentRoot HostContext.VirtualFiles), 'filesystem:' (WebRoot FileSystem), 'memory:' (WebRoot Memory)
        /// </summary>
        public List<string> Sources { get; set; } = new List<string>();
        
        /// <summary>
        /// Write bundled file to this Virtual Path
        /// </summary>
        public string OutputTo { get; set; }
        /// <summary>
        /// If needed, use alternative OutputTo Virtual Path in html tag
        /// </summary>
        public string OutputWebPath { get; set; }
        /// <summary>
        /// If needed, include PathBase prefix in output tag
        /// </summary>
        public string PathBase { get; set; }
        /// <summary>
        /// Whether to minify sources in bundle (default true)
        /// </summary>
        public bool Minify { get; set; } = true;
        /// <summary>
        /// Whether to save to disk or Memory File System (default Memory)
        /// </summary>
        public bool SaveToDisk { get; set; }
        /// <summary>
        /// Whether to return cached bundle if exists (default true)
        /// </summary>
        public bool Cache { get; set; } = true;
        /// <summary>
        /// Whether to bundle and emit single or not bundle and emit multiple html tags
        /// </summary>
        public bool Bundle { get; set; } = true;
        /// <summary>
        /// Whether to call AMD define for CommonJS modules
        /// </summary>
        public bool RegisterModuleInAmd { get; set; }
        /// <summary>
        /// Whether to wrap JS scripts in an Immediately-Invoked Function Expression
        /// </summary>
        public bool IIFE { get; set; }
    }

    public class TextDumpOptions
    {
        public TextStyle HeaderStyle { get; set; }
        public string Caption { get; set; }
        public string CaptionIfEmpty { get; set; }
        
        public string[] Headers { get; set; }
        public bool IncludeRowNumbers { get; set; } = true;

        public DefaultScripts Defaults { get; set; } = ViewUtils.DefaultScripts;
        
        internal int Depth { get; set; }
        internal bool HasCaption { get; set; }

        public static TextDumpOptions Parse(Dictionary<string, object> options, DefaultScripts defaults=null)
        {
            return new() {
                HeaderStyle = options.TryGetValue("headerStyle", out var oHeaderStyle)
                    ? oHeaderStyle.ConvertTo<TextStyle>()
                    : TextStyle.SplitCase,
                Caption = options.TryGetValue("caption", out var caption)
                    ? caption?.ToString()
                    : null,
                CaptionIfEmpty = options.TryGetValue("captionIfEmpty", out var captionIfEmpty)
                    ? captionIfEmpty?.ToString()
                    : null,
                IncludeRowNumbers = !options.TryGetValue("rowNumbers", out var rowNumbers) 
                    || (rowNumbers is not bool b || b),
                Defaults = defaults ?? ViewUtils.DefaultScripts,
            };
        }
    }

    public class HtmlDumpOptions
    {
        public string Id { get; set; }
        public string ClassName { get; set; }
        public string ChildClass { get; set; }

        public TextStyle HeaderStyle { get; set; }
        public string HeaderTag { get; set; }
        
        public string Caption { get; set; }
        public string CaptionIfEmpty { get; set; }
        
        public string[] Headers { get; set; }

        public DefaultScripts Defaults { get; set; } = ViewUtils.DefaultScripts;
        
        public string Display { get; set; }
        internal int Depth { get; set; }
        internal int ChildDepth { get; set; } = 1;
        internal bool HasCaption { get; set; }
        
        public static HtmlDumpOptions Parse(Dictionary<string, object> options, DefaultScripts defaults=null)
        {
            return new HtmlDumpOptions 
            {
                Id = options.TryGetValue("id", out var oId)
                    ? (string)oId
                    : null,
                ClassName = options.TryGetValue("className", out var oClassName)
                    ? (string)oClassName
                    : null,
                ChildClass = options.TryGetValue("childClass", out var oChildClass)
                    ? (string)oChildClass
                    : null,

                HeaderStyle = options.TryGetValue("headerStyle", out var oHeaderStyle)
                    ? oHeaderStyle.ConvertTo<TextStyle>()
                    : TextStyle.SplitCase,
                HeaderTag = options.TryGetValue("headerTag", out var oHeaderTag)
                    ? (string)oHeaderTag
                    : null,
                
                Caption = options.TryGetValue("caption", out var caption)
                    ? caption?.ToString()
                    : null,
                CaptionIfEmpty = options.TryGetValue("captionIfEmpty", out var captionIfEmpty)
                    ? captionIfEmpty?.ToString()
                    : null,
                Display = options.TryGetValue("display", out var display)
                    ? display?.ToString()
                    : null,
                Defaults = defaults ?? ViewUtils.DefaultScripts,
            };
        }
    }

    public enum TextStyle
    {
        None,
        SplitCase,
        Humanize,
        TitleCase,
        PascalCase,
        CamelCase,
    }
    
    /// <summary>
    /// Generic collection of Nav Links
    /// </summary>
    public static class NavDefaults
    {
        public static string NavClass { get; set; } = "nav";
        public static string NavItemClass { get; set; } = "nav-item";
        public static string NavLinkClass { get; set; } = "nav-link";
        
        public static string ChildNavItemClass { get; set; } = "nav-item dropdown";
        public static string ChildNavLinkClass { get; set; } = "nav-link dropdown-toggle";
        public static string ChildNavMenuClass { get; set; } = "dropdown-menu";
        public static string ChildNavMenuItemClass { get; set; } = "dropdown-item";
        
        public static NavOptions Create() => new NavOptions {
            NavClass = NavClass,
            NavItemClass = NavItemClass,
            NavLinkClass = NavLinkClass,
            ChildNavItemClass = ChildNavItemClass,
            ChildNavLinkClass = ChildNavLinkClass,
            ChildNavMenuClass = ChildNavMenuClass,
            ChildNavMenuItemClass = ChildNavMenuItemClass,
        };
        public static NavOptions ForNav(this NavOptions options) => options; //Already uses NavDefaults
        public static NavOptions OverrideDefaults(NavOptions targets, NavOptions source)
        {
            if (targets == null)
                return source;
            if (targets.NavClass == NavClass && source.NavClass != null)
                targets.NavClass = source.NavClass;
            if (targets.NavItemClass == NavItemClass && source.NavItemClass != null)
                targets.NavItemClass = source.NavItemClass;
            if (targets.NavLinkClass == NavLinkClass && source.NavLinkClass != null)
                targets.NavLinkClass = source.NavLinkClass;
            if (targets.ChildNavItemClass == ChildNavItemClass && source.ChildNavItemClass != null)
                targets.ChildNavItemClass = source.ChildNavItemClass;
            if (targets.ChildNavLinkClass == ChildNavLinkClass && source.ChildNavLinkClass != null)
                targets.ChildNavLinkClass = source.ChildNavLinkClass;
            if (targets.ChildNavMenuClass == ChildNavMenuClass && source.ChildNavMenuClass != null)
                targets.ChildNavMenuClass = source.ChildNavMenuClass;
            if (targets.ChildNavMenuItemClass == ChildNavMenuItemClass && source.ChildNavMenuItemClass != null)
                targets.ChildNavMenuItemClass = source.ChildNavMenuItemClass;
            return targets;
        }
    }
    /// <summary>
    /// Single NavLink List Item
    /// </summary>
    public static class NavLinkDefaults
    {
        public static NavOptions ForNavLink(this NavOptions options) => options; //Already uses NavDefaults
    }
    /// <summary>
    /// Navigation Bar Menu Items
    /// </summary>
    public static class NavbarDefaults
    {
        public static string NavClass { get; set; } = "navbar-nav";
        public static NavOptions Create() => new NavOptions { NavClass = NavClass };
        public static NavOptions ForNavbar(this NavOptions options) => NavDefaults.OverrideDefaults(options, Create());
    }
    /// <summary>
    /// Collection of Link Buttons (e.g. used to render /auth buttons)
    /// </summary>
    public static class NavButtonGroupDefaults
    {
        public static string NavClass { get; set; } = "btn-group";
        public static string NavItemClass { get; set; } = "btn btn-primary";
        public static NavOptions Create() => new NavOptions { NavClass = NavClass, NavItemClass = NavItemClass };
        public static NavOptions ForNavButtonGroup(this NavOptions options) => NavDefaults.OverrideDefaults(options, Create());
    }
    
    public class NavOptions
    {
        /// <summary>
        /// User Attributes for conditional rendering, e.g:
        ///  - auth - User is Authenticated
        ///  - role:name - User Role
        ///  - perm:name - User Permission 
        /// </summary>
        public HashSet<string> Attributes { get; set; }
        
        /// <summary>
        /// Path Info that should set as active 
        /// </summary>
        public string ActivePath { get; set; }
        
        
        /// <summary>
        /// Prefix to include before NavItem.Path (if any)
        /// </summary>
        public string BaseHref { get; set; }

        /// <summary>
        /// Custom classes applied to different navigation elements (defaults to Bootstrap classes)
        /// </summary>
        public string NavClass { get; set; } = NavDefaults.NavClass;
        public string NavItemClass { get; set; } = NavDefaults.NavItemClass;
        public string NavLinkClass { get; set; } = NavDefaults.NavLinkClass;
        
        public string ChildNavItemClass { get; set; } = NavDefaults.ChildNavItemClass;
        public string ChildNavLinkClass { get; set; } = NavDefaults.ChildNavLinkClass;
        public string ChildNavMenuClass { get; set; } = NavDefaults.ChildNavMenuClass;
        public string ChildNavMenuItemClass { get; set; } = NavDefaults.ChildNavMenuItemClass;
    }

    /// <summary>
    /// Public API for ViewUtils
    /// </summary>
    public static class View
    {
        public static List<NavItem> NavItems => ViewUtils.NavItems;
        public static Dictionary<string, List<NavItem>> NavItemsMap => ViewUtils.NavItemsMap;
        public static void Load(IAppSettings settings) => ViewUtils.Load(settings);

        public static List<NavItem> GetNavItems(string key) => ViewUtils.GetNavItems(key);
    }

    /// <summary>
    /// Shared Utils shared between different Template Filters and Razor Views/Helpers
    /// </summary>
    public static class ViewUtils
    {
        internal static readonly DefaultScripts DefaultScripts = new() { Context = new ScriptContext() };
        private static readonly HtmlScripts HtmlScripts = new() { Context = new ScriptContext() };

        public static string NavItemsKey { get; set; } = "NavItems";
        public static string NavItemsMapKey { get; set; } = "NavItemsMap";
        
        public static void Load(IAppSettings settings)
        {
            var navItems = settings?.Get<List<NavItem>>(NavItemsKey);
            if (navItems != null)
            {
                NavItems.AddRange(navItems);
            }

            var navItemsMap = settings?.Get<Dictionary<string, List<NavItem>>>(NavItemsMapKey);
            if (navItemsMap != null)
            {
                foreach (var entry in navItemsMap)
                {
                    NavItemsMap[entry.Key] = entry.Value;
                }
            }
        }

        public static bool ShowNav(this NavItem navItem, HashSet<string> attributes)
        {
            if (attributes.IsEmpty())
                return navItem.Show == null;
            if (navItem.Show != null && !attributes.Contains(navItem.Show))
                return false;
            if (navItem.Hide != null && attributes.Contains(navItem.Hide))
                return false;
            return true;
        }

        public static List<NavItem> NavItems { get; } = new();
        public static Dictionary<string, List<NavItem>> NavItemsMap { get; } = new();

        public static List<NavItem> GetNavItems(string key) => NavItemsMap.TryGetValue(key, out var navItems)
            ? navItems
            : TypeConstants<NavItem>.EmptyList;

        public static string CssIncludes(IVirtualPathProvider vfs, List<string> cssFiles)
        {
            if (vfs == null || cssFiles == null || cssFiles.Count == 0)
                return null;
            
            
            var sb = StringBuilderCache.Allocate();
            sb.AppendLine("<style>");

            foreach (var cssFile in cssFiles)
            {
                var virtualPath = !cssFile.StartsWith("/")
                    ? "/css/" + cssFile + ".css"
                    : cssFile;
                
                var file = vfs.GetFile(virtualPath.TrimStart('/'));
                if (file == null)
                    continue;

                string line;
                using var reader = file.OpenText();
                while ((line = reader.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
            }
            
            sb.AppendLine("</style>");
            return StringBuilderCache.ReturnAndFree(sb);
        }
        
        public static string JsIncludes(IVirtualPathProvider vfs, List<string> jsFiles)
        {
            if (vfs == null || jsFiles == null || jsFiles.Count == 0)
                return null;
            
            
            var sb = StringBuilderCache.Allocate();
            sb.AppendLine("<script>");

            foreach (var jsFile in jsFiles)
            {
                var virtualPath = !jsFile.StartsWith("/")
                    ? "/js/" + jsFile + ".js"
                    : jsFile;
                
                var file = vfs.GetFile(virtualPath.TrimStart('/'));
                if (file == null)
                    continue;

                string line;
                using var reader = file.OpenText();
                while ((line = reader.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
            }
            
            sb.AppendLine("</script>");
            return StringBuilderCache.ReturnAndFree(sb);
        }
        
        /// <summary>
        ///  Display a list of NavItem's
        /// </summary>
        public static string Nav(List<NavItem> navItems, NavOptions options)
        {
            if (navItems.IsEmpty())
                return string.Empty;
            
            var sb = StringBuilderCache.Allocate();
            sb.Append("<div class=\"")
                .Append(options.NavClass)
                .AppendLine("\">");

            foreach (var navItem in navItems)
            {
                NavLink(sb, navItem, options);
            }

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        /// <summary>
        /// Display a `nav-link` nav-item
        /// </summary>
        public static string NavLink(NavItem navItem, NavOptions options)
        {
            var sb = StringBuilderCache.Allocate();
            NavLink(sb, navItem, options);
            return StringBuilderCache.ReturnAndFree(sb);
        }

        static string ActiveClass(NavItem navItem, string activePath) => 
            navItem.Href != null && (navItem.Exact == true || activePath.Length <= 1 
                ? activePath?.TrimEnd('/').EqualsIgnoreCase(navItem.Href?.TrimEnd('/')) == true
                : activePath.TrimEnd('/').StartsWithIgnoreCase(navItem.Href?.TrimEnd('/')))
                    ? " active"
                    : "";
        
        /// <summary>
        /// Display a `nav-link` nav-item
        /// </summary>
        public static void NavLink(StringBuilder sb, NavItem navItem, NavOptions options)
        {
            if (!navItem.ShowNav(options.Attributes))
                return;
            
            var hasChildren = navItem.Children?.Count > 0;
            var navItemCls = hasChildren
                ? options.ChildNavItemClass
                : options.NavItemClass;
            var navLinkCls = hasChildren
                ? options.ChildNavLinkClass
                : options.NavLinkClass;
            var id = navItem.Id;
            if (hasChildren && id == null)
                id = navItem.Label.SafeVarName() + "MenuLink";

            sb.Append("<li class=\"")
                .Append(navItem.ClassName).Append(navItem.ClassName != null ? " " : "")
                .Append(navItemCls)
                .AppendLine("\">");
                
            sb.Append("  <a href=\"")
                .Append(options.BaseHref?.TrimEnd('/'))
                .Append(navItem.Href)
                .Append("\"");

            sb.Append(" class=\"")
                .Append(navLinkCls).Append(ActiveClass(navItem,options.ActivePath))
                .Append("\"");

            if (id != null)
                sb.Append(" id=\"").Append(id).Append("\"");

            if (hasChildren)
            {
                sb.Append(" role=\"button\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"false\"");
            }
            
            sb.Append(">")
                .Append(navItem.Label)
                .AppendLine("</a>");

            if (hasChildren)
            {
                sb.Append("  <div class=\"")
                    .Append(options.ChildNavMenuClass)
                    .Append("\" aria-labelledby=\"").Append(id).AppendLine("\">");

                foreach (var childNav in navItem.Children)
                {
                    if (childNav.Label == "-")
                    {
                        sb.AppendLine("    <div class=\"dropdown-divider\"></div>");
                    }
                    else
                    {
                        sb.Append("    <a class=\"")
                            .Append(options.ChildNavMenuItemClass)
                            .Append(ActiveClass(childNav,options.ActivePath))
                            .Append("\"")
                            .Append(" href=\"")
                            .Append(options.BaseHref?.TrimEnd('/'))
                            .Append(childNav.Href)
                            .Append("\">")
                            .Append(childNav.Label)
                            .AppendLine("</a>");
                    }
                }
                sb.AppendLine("</div");
            }

            sb.Append("</lI>");
        }

        public static string NavButtonGroup(List<NavItem> navItems, NavOptions options)
        {
            if (navItems.IsEmpty())
                return string.Empty;
            
            var sb = StringBuilderCache.Allocate();
            sb.Append("<div class=\"")
                .Append(options.NavClass)
                .AppendLine("\">");

            foreach (var navItem in navItems)
            {
                NavLinkButton(sb, navItem, options);
            }

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        public static string NavButtonGroup(NavItem navItem, NavOptions options)
        {
            var sb = StringBuilderCache.Allocate();
            NavLinkButton(sb, navItem, options);
            return StringBuilderCache.ReturnAndFree(sb);
        }
        
        public static void NavLinkButton(StringBuilder sb, NavItem navItem, NavOptions options)
        {
            if (!navItem.ShowNav(options.Attributes))
                return;
            
            sb.Append("<a href=\"")
                .Append(options.BaseHref?.TrimEnd('/'))
                .Append(navItem.Href)
                .Append("\"");

            sb.Append(" class=\"")
                .Append(navItem.ClassName).Append(navItem.ClassName != null ? " " : "")
                .Append(options.NavItemClass).Append(ActiveClass(navItem, options.ActivePath))
                .Append("\"");

            if (navItem.Id != null)
                sb.Append(" id=\"").Append(navItem.Id).Append("\"");

            sb.Append(">")
                .Append(!string.IsNullOrEmpty(navItem.IconClass) 
                    ? $"<i class=\"{navItem.IconClass}\"></i>" : "")
                .Append(navItem.Label)
                .AppendLine("</a>");
        }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(object test) => test == null || test == JsNull.Value;
        
        public static CultureInfo GetDefaultCulture(this DefaultScripts defaultScripts) => 
            defaultScripts?.Context?.Args[ScriptConstants.DefaultCulture] as CultureInfo ?? ScriptConfig.DefaultCulture;
        
        public static string GetDefaultTableClassName(this DefaultScripts defaultScripts) => 
            defaultScripts?.Context?.Args[ScriptConstants.DefaultTableClassName] as string;

        public static string TextDump(this object target) => DefaultScripts.TextDump(target, null); 
        public static string TextDump(this object target, TextDumpOptions options) => DefaultScripts.TextDump(target, options); 
        public static string DumpTable(this object target) => DefaultScripts.TextDump(target, null); 
        public static void PrintDumpTable(this object target) => DumpTable(target).Print(); 
        public static string DumpTable(this object target, TextDumpOptions options) => DefaultScripts.TextDump(target, options);
        public static void PrintDumpTable(this object target, TextDumpOptions options) => DumpTable(target, options).Print(); 
        
        public static string HtmlDump(object target) => HtmlScripts.HtmlDump(target, null); 
        public static string HtmlDump(object target, HtmlDumpOptions options) => HtmlScripts.HtmlDump(target, options); 
        
        public static string StyleText(string text, TextStyle textStyle)
        {
            if (text == null) return null;
            switch (textStyle)
            {
                case TextStyle.SplitCase:
                    return DefaultScripts.splitCase(text);
                case TextStyle.Humanize:
                    return DefaultScripts.humanize(text);
                case TextStyle.TitleCase:
                    return DefaultScripts.titleCase(text);
                case TextStyle.PascalCase:
                    return DefaultScripts.pascalCase(text);
                case TextStyle.CamelCase:
                    return DefaultScripts.camelCase(text);
            }
            return text;
        }
        
        /// <summary>
        /// Emit HTML hidden input field for each specified Key/Value pair entry
        /// </summary>
        public static string HtmlHiddenInputs(IEnumerable<KeyValuePair<string,object>> inputValues)
        {
            if (inputValues != null)
            {
                var sb = StringBuilderCache.Allocate();
                foreach (var entry in inputValues)
                {
                    sb.AppendLine($"<input type=\"hidden\" name=\"{entry.Key.HtmlEncode()}\" value=\"{entry.Value?.ToString().HtmlEncode()}\">");
                }
                return StringBuilderCache.ReturnAndFree(sb);
            }
            return null;
        }

        internal static object GetItem(this IRequest httpReq, string key)
        {
            if (httpReq == null) return null;

            httpReq.Items.TryGetValue(key, out var value);
            return value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ResponseStatus GetErrorStatus(IRequest req) => 
            req.GetItem("__errorStatus") as ResponseStatus; // Keywords.ErrorStatus

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasErrorStatus(IRequest req) => GetErrorStatus(req) != null; 
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormQuery(IRequest req, string name) => req.FormData[name] ?? req.QueryString[name];
        public static string[] FormQueryValues(IRequest req, string name)
        {
            var values = req.Verb == HttpMethods.Post 
                ? req.FormData.GetValues(name) 
                : req.QueryString.GetValues(name);

            return values?.Length == 1 // if it's only a single item can be returned in comma-delimited list
                ? values[0].Split(',') 
                : values ?? TypeConstants.EmptyStringArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormValue(IRequest req, string name) => FormValue(req, name, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormValue(IRequest req, string name, string defaultValue) => HasErrorStatus(req) 
            ? FormQuery(req, name) 
            : defaultValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] FormValues(IRequest req, string name) => HasErrorStatus(req) 
            ? FormQueryValues(req, name) 
            : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FormCheckValue(IRequest req, string name)
        {
            var value = FormValue(req, name);
            return value is "true" or "True" or "t" or "on" or "1";
        }

        public static string GetParam(IRequest req, string name) //sync with IRequest.GetParam()
        {
            string value;
            if ((value = req.Headers[HttpHeaders.XParamOverridePrefix + name]) != null) return value;
            if ((value = req.QueryString[name]) != null) return value;
            if ((value = req.FormData[name]) != null) return value;

            //IIS will assign null to params without a name: .../?some_value can be retrieved as req.Params[null]
            //TryGetValue is not happy with null dictionary keys, so we should bail out here
            if (string.IsNullOrEmpty(name)) return null;

            if (req.Cookies.TryGetValue(name, out var cookie)) return cookie.Value;

            if (req.Items.TryGetValue(name, out var oValue)) return oValue.ToString();

            return null;
        }

        /// <summary>
        /// Comma delimited field names
        /// </summary>
        public static List<string> ToVarNames(string fieldNames) =>
            fieldNames.Split(',').Map(x => x.Trim());

        public static IEnumerable<string> ToStrings(string filterName, object arg)
        {
            if (arg == null)
                return TypeConstants.EmptyStringArray;
            
            var strings = arg is IEnumerable<string> ls
                ? ls
                : arg is string s
                    ? new [] { s }
                    : arg is IEnumerable<object> e
                        ? e.Map(x => x.AsString())
                        : throw new NotSupportedException($"{filterName} expected a collection of strings but was '{arg.GetType().Name}'");

            return strings;
        }

        /// <summary>
        /// Show validation summary error message unless there's an error in exceptFor list of fields
        /// as validation errors will be displayed along side the field instead
        /// </summary>
        public static string ValidationSummary(ResponseStatus errorStatus, string exceptFor) =>
            ValidationSummary(errorStatus, ToVarNames(exceptFor), null); 
        public static string ValidationSummary(ResponseStatus errorStatus, ICollection<string> exceptFields, Dictionary<string,object> divAttrs)
        {
            var errorSummaryMsg = exceptFields != null
                ? ErrorResponseExcept(errorStatus, exceptFields)
                : ErrorResponseSummary(errorStatus);

            if (string.IsNullOrEmpty(errorSummaryMsg))
                return null;

            if (divAttrs == null)
                divAttrs = new Dictionary<string, object>();
            
            if (!divAttrs.ContainsKey("class") && !divAttrs.ContainsKey("className"))
                divAttrs["class"] = ValidationSummaryCssClassNames;
            
            return HtmlScripts.htmlDiv(errorSummaryMsg, divAttrs).ToRawString();
        }
        
        public static string ValidationSummaryCssClassNames = "alert alert-danger";
        public static string ValidationSuccessCssClassNames = "alert alert-success";

        /// <summary>
        /// Display a "Success Alert Box"
        /// </summary>
        public static string ValidationSuccess(string message, Dictionary<string,object> divAttrs)
        {
            if (divAttrs == null)
                divAttrs = new Dictionary<string, object>();
            
            if (!divAttrs.ContainsKey("class") && !divAttrs.ContainsKey("className"))
                divAttrs["class"] = ValidationSuccessCssClassNames;
            
            return HtmlScripts.htmlDiv(message, divAttrs).ToRawString();
        }
        
        /// <summary>
        /// Return an error message unless there's an error in fieldNames
        /// </summary>
        public static string ErrorResponseExcept(ResponseStatus errorStatus, string fieldNames) =>
            ErrorResponseExcept(errorStatus, ToVarNames(fieldNames));
        public static string ErrorResponseExcept(ResponseStatus errorStatus, ICollection<string> fieldNames)
        {
            if (errorStatus == null)
                return null;
            
            var fieldNamesLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fieldName in fieldNames)
            {
                fieldNamesLookup.Add(fieldName);
            }

            if (!fieldNames.IsEmpty() && !errorStatus.Errors.IsEmpty())
            {
                foreach (var fieldError in errorStatus.Errors)
                {
                    if (fieldNamesLookup.Contains(fieldError.FieldName))
                        return null;
                }

                var firstFieldError = errorStatus.Errors[0];
                return firstFieldError.Message ?? firstFieldError.ErrorCode;
            }

            return errorStatus.Message ?? errorStatus.ErrorCode;
        }
        
        /// <summary>
        /// Return an error message unless there are field errors
        /// </summary>
        public static string ErrorResponseSummary(ResponseStatus errorStatus)
        {
            if (errorStatus == null)
                return null;

            return errorStatus.Errors.IsEmpty()
                ? errorStatus.Message ?? errorStatus.ErrorCode
                : null;
        }
        
        /// <summary>
        /// Return an error for the specified field (if any)  
        /// </summary>
        public static string ErrorResponse(ResponseStatus errorStatus, string fieldName)
        {
            if (fieldName == null)
                return ErrorResponseSummary(errorStatus);
            if (errorStatus == null || errorStatus.Errors.IsEmpty())
                return null;

            foreach (var fieldError in errorStatus.Errors)
            {
                if (fieldName.EqualsIgnoreCase(fieldError.FieldName))
                    return fieldError.Message ?? fieldError.ErrorCode;
            }

            return null;
        }

        public static List<KeyValuePair<string, string>> ToKeyValues(object values)
        {
            var to = new List<KeyValuePair<string, string>>();
            if (values != null)
            {
                if (values is IEnumerable<KeyValuePair<string, object>> kvps)
                    foreach (var kvp in kvps) to.Add(new KeyValuePair<string,string>(kvp.Key, kvp.Value?.ToString()));
                else if (values is IEnumerable<KeyValuePair<string, string>> kvpsStr)
                    foreach (var kvp in kvpsStr) to.Add(new KeyValuePair<string,string>(kvp.Key, kvp.Value));
                else if (values is IEnumerable<object> list)
                    to.AddRange(from string item in list select item.AsString() into s select new KeyValuePair<string, string>(s, s));
            }
            return to;
        }
        
        public static List<string> SplitStringList(IEnumerable strings) => strings is null
            ? TypeConstants.EmptyStringList
            : strings is List<string> strList
                ? strList
                : strings is IEnumerable<string> strEnum
                    ? strEnum.ToList()
                    : strings is IEnumerable<object> objEnum
                        ? objEnum.Map(x => x.AsString())
                        : strings is string strFields
                            ? strFields.Split(',').Map(x => x.Trim())
                            : throw new NotSupportedException($"Cannot convert '{strings.GetType().Name}' to List<string>");
        
        public static List<string> ToStringList(IEnumerable strings) => strings is List<string> l ? l
            : strings is string s 
            ? new List<string> { s } 
            : strings is IEnumerable<string> e
            ? new List<string>(e)
            : strings.Map(x => x.AsString());

        public static string FormControl(IRequest req, Dictionary<string,object> args, string tagName, InputOptions inputOptions)
        {
            if (tagName == null)
                tagName = "input";
            
            var options = inputOptions ?? new InputOptions();

            string id = null;
            string type = null;
            string name = null;
            string label = null;
            string size = options.Size;
            bool inline = options.Inline;

            if (args.TryGetValue("type", out var oType))
                type = oType as string;
            else
                args["type"] = type = "text";

            var notInput = tagName != "input";
            if (notInput)
            {
                type = tagName;
                args.RemoveKey("type");
            }

            var inputClass = "form-control";
            var labelClass = "form-label";
            var helpClass = "text-muted";
            var isCheck = type == "checkbox" || type == "radio";
            if (isCheck)
            {
                inputClass = "form-check-input";
                labelClass = "form-check-label";
                if (!args.ContainsKey("value"))
                    args["value"] = "true";
            }
            else if (type == "range")
            {
                inputClass = "form-control-range";
            }
            if (options.LabelClass != null)
                labelClass = options.LabelClass;

            if (args.TryGetValue("id", out var oId))
            {
                if (!args.ContainsKey("name"))
                    args["name"] = id = oId as string;

                if (args.TryGetValue("name", out var oName))
                    name = oName as string;
            }

            string help = options.Help;
            string helpId = help != null ? (id ?? name) + "-help" : null;
            if (helpId != null)
                args["aria-describedby"] = helpId;

            if (options.Label != null)
            {
                label = options.Label;
                if (!args.ContainsKey("placeholder"))
                    args["placeholder"] = label;
            }

            var values = options.Values;
            var isSingleCheck = isCheck && values == null;

            object oValue = null;
            string formValue = null;
            var isGet = req.Verb == HttpMethods.Get;
            var preserveValue = options.PreserveValue;
            if (preserveValue)
            {
                var strValue = args.TryGetValue("value", out oValue) ? oValue as string : null;
                formValue = FormValue(req, name, strValue);
                if (!isGet || !string.IsNullOrEmpty(formValue)) //only override value if POST or GET queryString has value
                {
                    if (!isCheck)
                        args["value"] = formValue;
                    else if (isSingleCheck)
                        args["checked"] = formValue == "true";
                }
            }
            else if (!isGet)
            {
                if (!isCheck)
                {
                    args["value"] = null;
                }
            }

            var className = args.TryGetValue("class", out var oCls) || args.TryGetValue("className", out oCls)
                ? HtmlScripts.htmlClassList(oCls)
                : "";
            
            className = HtmlScripts.htmlAddClass(className, inputClass);

            if (size != null)
                className = HtmlScripts.htmlAddClass(className, inputClass + "-" + size);

            var errorMsg = ErrorResponse(GetErrorStatus(req), name);
            if (errorMsg != null)
                className = HtmlScripts.htmlAddClass(className, "is-invalid");

            args["class"] = className;

            string inputHtml = null, labelHtml = null;
            var sb = StringBuilderCache.Allocate();

            if (label != null)
            {
                var labelArgs = new Dictionary<string, object>
                {
                    ["html"] = label,
                    ["class"] = labelClass,
                };
                if (id != null)
                    labelArgs["for"] = id;

                labelHtml = HtmlScripts.htmlLabel(labelArgs).AsString();
            }

            var value = (args.TryGetValue("value", out oValue)
                    ? oValue as string
                    : null)
                ?? (oValue?.GetType().IsValueType == true
                    ? oValue.ToString()
                    : null);

            if (type == "radio")
            {
                if (values != null)
                {
                    var sbInput = StringBuilderCacheAlt.Allocate();
                    var kvps = ToKeyValues(values);
                    foreach (var kvp in kvps)
                    {
                        var cls = inline ? " custom-control-inline" : "";
                        sbInput.AppendLine($"<div class=\"custom-control custom-radio{cls}\">");
                        var inputId = name + "-" + kvp.Key;
                        var selected = kvp.Key == formValue || kvp.Key == value ? " checked" : "";
                        sbInput.AppendLine($"  <input type=\"radio\" id=\"{inputId}\" name=\"{name}\" value=\"{kvp.Key}\" class=\"custom-control-input\"{selected}>");
                        sbInput.AppendLine($"  <label class=\"custom-control-label\" for=\"{inputId}\">{kvp.Value}</label>");
                        sbInput.AppendLine("</div>");
                    }
                    inputHtml = StringBuilderCacheAlt.ReturnAndFree(sbInput);
                }
                else throw new NotSupportedException($"input type=radio requires 'values' inputOption containing a collection of Key/Value Pairs");
            }
            else if (type == "checkbox")
            {
                if (values != null)
                {
                    var sbInput = StringBuilderCacheAlt.Allocate();
                    var kvps = ToKeyValues(values);

                    var selectedValues = value != null && value != "true"
                        ? new HashSet<string> {value}
                        : oValue == null
                            ? TypeConstants<string>.EmptyHashSet
                            : (FormValues(req, name) ?? ToStringList(oValue as IEnumerable).ToArray())
                                  .ToSet();
                                
                    foreach (var kvp in kvps)
                    {
                        var cls = inline ? " custom-control-inline" : "";
                        sbInput.AppendLine($"<div class=\"custom-control custom-checkbox{cls}\">");
                        var inputId = name + "-" + kvp.Key;
                        var selected = kvp.Key == formValue || selectedValues.Contains(kvp.Key) ? " checked" : "";
                        sbInput.AppendLine($"  <input type=\"checkbox\" id=\"{inputId}\" name=\"{name}\" value=\"{kvp.Key}\" class=\"form-check-input\"{selected}>");
                        sbInput.AppendLine($"  <label class=\"form-check-label\" for=\"{inputId}\">{kvp.Value}</label>");
                        sbInput.AppendLine("</div>");
                    }
                    inputHtml = StringBuilderCacheAlt.ReturnAndFree(sbInput);
                }
            }
            else if (type == "select")
            {
                if (values != null)
                {
                    args["html"] = HtmlScripts.htmlOptions(values, 
                        new Dictionary<string, object> { {"selected",formValue ?? value} });
                }
                else if (!args.ContainsKey("html"))
                    throw new NotSupportedException($"<select> requires either 'values' inputOption containing a collection of Key/Value Pairs or 'html' argument containing innerHTML <option>'s");
            }

            if (inputHtml == null)
                inputHtml = HtmlScripts.htmlTag(args, tagName).AsString();

            if (isCheck)
            {
                sb.AppendLine(inputHtml);
                if (isSingleCheck) 
                    sb.AppendLine(labelHtml);
            }
            else
            {
                sb.AppendLine(labelHtml).AppendLine(inputHtml);
            }

            if (help != null)
            {
                sb.AppendLine($"<small id='{helpId}' class='{helpClass}'>{help.HtmlEncode()}</small>");
            }

            string htmlError = null;
            if (options.ShowErrors && errorMsg != null)
            {
                var errorClass = "invalid-feedback";
                if (options.ErrorClass != null)
                    errorClass = options.ErrorClass ?? "";
                htmlError = $"<div class='{errorClass}'>{errorMsg.HtmlEncode()}</div>";
            }

            if (!isCheck)
            {
                sb.AppendLine(htmlError);
            }
            else
            {
                var cls = htmlError != null ? " is-invalid form-control" : "";
                sb.Insert(0, $"<div class=\"form-check{cls}\">");
                sb.AppendLine("</div>");
                if (htmlError != null)
                    sb.AppendLine(htmlError);
            }

            if (isCheck && !isSingleCheck) // multi-value checkbox/radio
                sb.Insert(0, labelHtml);

            var html = StringBuilderCache.ReturnAndFree(sb);
            return html;
        }
        
        private static IVirtualFiles ResolveWriteVfs(string filterName, IVirtualPathProvider webVfs, IVirtualPathProvider contentVfs, string outFile, bool toDisk, out string useOutFile)
        {
            if (outFile.IndexOf(':') >= 0)
            {
                ResolveVfsAndSource(filterName, webVfs, contentVfs, outFile, out var useVfs, out useOutFile);
                return (IVirtualFiles)useVfs;
            }

            useOutFile = outFile;
            var vfs = !toDisk
                ? (IVirtualFiles)webVfs.GetMemoryVirtualFiles() ??
                    throw new NotSupportedException($"{nameof(MemoryVirtualFiles)} is required in {filterName} when disk=false")
                : webVfs.GetFileSystemVirtualFiles() ??
                    throw new NotSupportedException($"{nameof(FileSystemVirtualFiles)} is required in {filterName} when disk=true");
            return vfs;
        }

        private static void ResolveVfsAndSource(string filterName, IVirtualPathProvider webVfs, IVirtualPathProvider contentVfs, string source, out IVirtualPathProvider useVfs, out string useSource)
        {
            useVfs = webVfs;
            useSource = source;

            var parts = source.SplitOnFirst(':');
            if (parts.Length != 2)
                return;

            useSource = parts[1];
            var name = parts[0];
            useVfs = name == "content"
                ? contentVfs
                : name == "web"
                    ? webVfs
                    : name == "filesystem"
                        ? (IVirtualPathProvider) webVfs.GetFileSystemVirtualFiles()
                        : name == "memory"
                            ? webVfs.GetMemoryVirtualFiles()
                            : throw new NotSupportedException($"Unknown Virtual File System provider '{name}' used in '{filterName}'. Valid providers: web,content,filesystem,memory");
        }

        public static IEnumerable<IVirtualFile> GetBundleFiles(string filterName, IVirtualPathProvider webVfs, IVirtualPathProvider contentVfs, IEnumerable<string> virtualPaths, string assetExt)
        {
            var excludeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var source in virtualPaths)
            {
                ResolveVfsAndSource(filterName, webVfs, contentVfs, source, out var vfs, out var virtualPath);

                if (virtualPath.StartsWith("!"))
                {
                    excludeFiles.Add(virtualPath.Substring(1).TrimStart('/'));
                    continue;
                }
                
                var dir = vfs.GetDirectory(virtualPath);
                if (dir != null)
                {
                    var files = dir.GetAllFiles().OrderBy(x => x.VirtualPath);
                    foreach (var dirFile in files)
                    {
                        if (!assetExt.EqualsIgnoreCase(dirFile.Extension))
                            continue;
                        if (excludeFiles.Contains(dirFile.VirtualPath))
                            continue;
                        
                        yield return dirFile;
                    }
                    continue;
                }

                var file = vfs.GetFile(virtualPath);
                if (file != null)
                {
                    if (excludeFiles.Contains(file.VirtualPath))
                        continue;
                    
                    yield return file;
                }
                else throw new NotSupportedException($"Could not find resource at virtual path '{source}' in '{filterName}'");
            }
        }

        public static string BundleJs(string filterName, 
            IVirtualPathProvider webVfs, 
            IVirtualPathProvider contentVfs, 
            ICompressor jsCompressor,
            BundleOptions options)
        {
            var assetExt = "js";
            var outFile = options.OutputTo ?? (options.Minify 
                  ? $"/{assetExt}/bundle.min.{assetExt}" : $"/{assetExt}/bundle.{assetExt}");
            var htmlTagFmt = "<script src=\"{0}\"></script>";

            return BundleAsset(filterName, 
                webVfs, 
                contentVfs,
                jsCompressor, options, outFile, options.OutputWebPath, htmlTagFmt, assetExt, options.PathBase);
        }

        public static string BundleCss(string filterName, 
            IVirtualPathProvider webVfs, 
            IVirtualPathProvider contentVfs, 
            ICompressor cssCompressor,
            BundleOptions options)
        {
            var assetExt = "css";
            var outFile = options.OutputTo ?? (options.Minify 
                ? $"/{assetExt}/bundle.min.{assetExt}" : $"/{assetExt}/bundle.{assetExt}");
            var htmlTagFmt = "<link rel=\"stylesheet\" href=\"{0}\">";

            return BundleAsset(filterName, 
                webVfs, 
                contentVfs, 
                cssCompressor, options, outFile, options.OutputWebPath, htmlTagFmt, assetExt, options.PathBase);
        }

        public static string BundleHtml(string filterName, 
            IVirtualPathProvider webVfs, 
            IVirtualPathProvider contentVfs, 
            ICompressor htmlCompressor,
            BundleOptions options)
        {
            var assetExt = "html";
            var outFile = options.OutputTo ?? (options.Minify 
                  ? $"/{assetExt}/bundle.min.{assetExt}" : $"/{assetExt}/bundle.{assetExt}");
            var id = options.OutputTo != null
                ? $" id=\"{options.OutputTo.LastRightPart('/').LeftPart('.')}\"" : "";
            var htmlTagFmt = "<link rel=\"import\" href=\"{0}\"" + id + ">";

            return BundleAsset(filterName, 
                webVfs, 
                contentVfs,
                htmlCompressor, options, outFile, options.OutputWebPath, htmlTagFmt, assetExt, options.PathBase);
        }

        private static string BundleAsset(string filterName, 
            IVirtualPathProvider webVfs, 
            IVirtualPathProvider contentVfs, 
            ICompressor jsCompressor,
            BundleOptions options, 
            string origOutFile, 
            string outWebPath, 
            string htmlTagFmt, 
            string assetExt, 
            string pathBase)
        {
            try
            {
                var writeVfs = ResolveWriteVfs(filterName, webVfs, contentVfs, origOutFile, options.SaveToDisk, out var outFilePath);

                var outHtmlTag = htmlTagFmt.Replace("{0}", pathBase == null ? outFilePath : pathBase.CombineWith(outFilePath));

                var maxDate = DateTime.MinValue;
                var hasHash = outFilePath.IndexOf("[hash]", StringComparison.Ordinal) >= 0;

                if (!options.Sources.IsEmpty() && options.Bundle && options.Cache)
                {
                    if (hasHash)
                    {
                        var memFs = webVfs.GetMemoryVirtualFiles();

                        var existingBundleTag = webVfs.GetFile(outFilePath); 
                        if (existingBundleTag == null)
                        {
                            // use existing bundle if file with matching hash pattern is found
                            var outDirPath = outFilePath.LastLeftPart('/');
                            var outFileName = outFilePath.LastRightPart('/');
                            var outGlobFile = outFileName.Replace("[hash]", ".*");

                            // use glob search to avoid unnecessary file scans
                            var outDir = webVfs.GetDirectory(outDirPath);
                            if (outDir != null)
                            {
                                var outDirFiles = outDir.GetFiles().OrderBy(x => x.VirtualPath);
                                foreach (var file in outDirFiles)
                                {
                                    if (file.Name.Glob(outGlobFile))
                                    {
                                        outHtmlTag = htmlTagFmt.Replace("{0}", "/" + file.VirtualPath);
                                        memFs.WriteFile(outFilePath, outHtmlTag); //cache lookup
                                        return outHtmlTag;
                                    }
                                }
                            }
                        }
                        else
                        {
                            return existingBundleTag.ReadAllText();
                        }
                    }
                    else if (webVfs.FileExists(outWebPath ?? outFilePath))
                    {
                        return outHtmlTag;
                    }
                }

                var sources = GetBundleFiles(filterName, webVfs, contentVfs, options.Sources, assetExt);

                var existing = new HashSet<string>();
                var sb = StringBuilderCache.Allocate();
                var sbLog = StringBuilderCacheAlt.Allocate();

                void LogWarning(string msg)
                {
                    sbLog.AppendLine()
                        .Append(assetExt == "html" ? "<!--" : "/*")
                        .Append(" WARNING: ")
                        .Append(msg)
                        .Append(assetExt == "html" ? "-->" : "*/");
                }

                
                var minExt = ".min." + assetExt;
                if (options.Bundle)
                {
                    foreach (var file in sources)
                    {
                        if (hasHash)
                        {
                            file.Refresh();
                            if (file.LastModified > maxDate)
                                maxDate = file.LastModified;
                        }
                        
                        string src;
                        try
                        {
                            src = file.ReadAllText();
                        }
                        catch (Exception e)
                        {
                            LogWarning($"Could not read '{file.VirtualPath}': {e.Message}");
                            continue;
                        }
                        
                        if (file.Name.EndsWith("bundle." + assetExt) ||
                            file.Name.EndsWith("bundle.min." + assetExt) ||
                            existing.Contains(file.VirtualPath))
                            continue;

                        if (options.IIFE) sb.AppendLine("(function(){");
                        
                        if (options.Minify && !file.Name.EndsWith(minExt))
                        {
                            string minified;
                            try
                            {
                                minified = jsCompressor.Compress(src);
                            }
                            catch (Exception e)
                            {
                                LogWarning($"Could not Compress '{file.VirtualPath}': {e.Message}");
                                minified = src;
                            }
                            
                            sb.Append(minified).Append(assetExt == "js" ? ";" : "").AppendLine();
                        }
                        else
                        {
                            sb.AppendLine(src);
                        }
    
                        if (options.IIFE) sb.AppendLine("})();");
                        
                        // Also define ES6 module in AMD's define(), required by /js/ss-require.js
                        if (options.RegisterModuleInAmd && assetExt == "js")
                        {
                            sb.AppendLine("if (typeof define === 'function' && define.amd && typeof module !== 'undefined') define('" +
                                          file.Name.WithoutExtension() + "', [], function(){ return module.exports; });");
                        }

                        existing.Add(file.VirtualPath);
                    }

                    var bundled = StringBuilderCache.ReturnAndFree(sb);
                    if (hasHash)
                    {
                        var hash = "." + maxDate.ToUnixTimeMs();
                        outHtmlTag = outHtmlTag.Replace("[hash]", hash);
                        webVfs.GetMemoryVirtualFiles().WriteFile(outFilePath, outHtmlTag); //have bundle[hash].ext return rendered html
                        
                        outFilePath = outFilePath.Replace("[hash]", hash);
                    }
                    
                    try
                    {
                        writeVfs.WriteFile(outFilePath, bundled);
                    }
                    catch (Exception e)
                    {
                        LogWarning($"Could not write to '{origOutFile}': {e.Message}");
                    }

                    if (sbLog.Length != 0) 
                        return outHtmlTag + StringBuilderCacheAlt.ReturnAndFree(sbLog);
                    
                    StringBuilderCacheAlt.Free(sbLog);
                    return outHtmlTag;
                }
                else
                {
                    var filePaths = new List<string>();
                    
                    foreach (var file in sources)
                    {
                        if (file.Name.EndsWith("bundle." + assetExt) ||
                            file.Name.EndsWith("bundle.min." + assetExt) ||
                            existing.Contains(file.VirtualPath))
                            continue;
                        
                        filePaths.Add("/".CombineWith(file.VirtualPath));
                        existing.Add(file.VirtualPath);
                    }

                    foreach (var filePath in filePaths)
                    {
                        if (filePath.EndsWith(minExt))
                        {
                            var withoutMin = filePath.Substring(0, filePath.Length - minExt.Length) + "." + assetExt;
                            if (filePaths.Contains(withoutMin))
                                continue;
                        }

                        sb.AppendLine(htmlTagFmt.Replace("{0}", filePath));
                    }
                    
                    return StringBuilderCache.ReturnAndFree(sb);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    
}