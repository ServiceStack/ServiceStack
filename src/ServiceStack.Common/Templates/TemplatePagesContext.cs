using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Text;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class TemplatePagesContext
    {
        public List<PageFormat> PageFormats { get; set; } = new List<PageFormat>();
        
        public string IndexPage { get; set; } = "index";

        public string DefaultLayoutPage { get; set; } = "_layout";
        
        public string LayoutVarName { get; set; } = "layout";

        public bool CheckModifiedPages { get; set; } = false;

        public ITemplatePages Pages { get; set; }

        public IVirtualPathProvider VirtualFileSources { get; set; }
        
        public bool DebugMode { get; set; }

        public PageFormat GetFormat(string extension) => PageFormats.FirstOrDefault(x => x.Extension == extension);

        public List<Type> ScanTypes { get; set; } = new List<Type>();

        public List<Assembly> ScanAssemblies{ get; set; } = new List<Assembly>();

        public IResolver Resolver { get; set; }
        
        public List<TemplateFilter> TemplateFilters { get; set; } = new List<TemplateFilter>();

        public Dictionary<Type, Func<object>> Factory { get; } = new Dictionary<Type, Func<object>>();

        public TemplatePagesContext()
        {
            Resolver = new SingletonFactoryResolver(this);
            Pages = new TemplatePages(this);
            PageFormats.Add(new HtmlPageFormat());
            
            Factory[typeof(ITemplatePages)] = () => Pages;
        }

        public TemplatePagesContext Init()
        {
            foreach (var type in ScanTypes)
            {
                ScanType(type);
            }

            foreach (var assembly in ScanAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    ScanType(type);
                }
            }

            return this;
        }

        public void ScanType(Type type)
        {
            if (typeof(TemplateFilter).IsAssignableFromType(type))
            {
                var filter = (TemplateFilter)Resolver.TryResolve(type);
                AutoWire(filter);
                TemplateFilters.Add(filter.Init());
            }
            else if (typeof(TemplateCode).IsAssignableFromType(type))
            {
                var code = (TemplateCode)Resolver.TryResolve(type);
                AutoWire(code);
                code.Init();
//                TemplateFilters.Add(code);
            }
        }

        private Dictionary<Type, Action<object>[]> autoWireCache = new Dictionary<Type, Action<object>[]>();

        public HashSet<string> IgnorePropertyTypeFullNames { get; } = new HashSet<string>();

        protected bool IsPublicWritableUserPropertyType(PropertyInfo pi)
        {
            return pi.CanWrite
                   && !pi.PropertyType.IsValueType()
                   && pi.PropertyType != typeof(string)
                   && !IgnorePropertyTypeFullNames.Contains(pi.PropertyType.FullName);
        }

        protected void AutoWire(object instance)
        {
            var instanceType = instance.GetType();
            var props = TypeProperties.Get(instanceType);
            
            Action<object>[] setters;
            if (!autoWireCache.TryGetValue(instanceType, out setters))
            {
                setters = props.PublicPropertyInfos
                    .Where(IsPublicWritableUserPropertyType)
                    .Select(pi =>  {
                        var fn = props.GetPublicSetter(pi);
                        return (Action<object>)(o => fn(o, Resolver.TryResolve(pi.PropertyType)));
                    })
                    .ToArray();

                Dictionary<Type, Action<object>[]> snapshot, newCache;
                do
                {
                    snapshot = autoWireCache;
                    newCache = new Dictionary<Type, Action<object>[]>(autoWireCache) {
                        [instanceType] = setters
                    };
                } while (!ReferenceEquals(
                    Interlocked.CompareExchange(ref autoWireCache, newCache, snapshot), snapshot));
            }

            foreach (var setter in setters)
            {
                setter(instance);
            }
        }
    }

    public class PageFormat
    {
        public string ArgsPrefix { get; set; } = "---";

        public string ArgsSuffix { get; set; } = "---";
        
        public string Extension { get; set; }

        public string ContentType { get; set; } = MimeTypes.PlainText;

        public Func<object, string> EncodeValue { get; set; }

        public Func<TemplatePage, TemplatePage> ResolveLayout { get; set; }

        public PageFormat()
        {
            EncodeValue = DefaultEncodeValue;
            ResolveLayout = DefaultResolveLayout;
        }

        public string DefaultEncodeValue(object value)
        {
            if (value is IRawString rawString)
                return rawString.ToRawString();
            
            var str = value.ToString();
            if (str == string.Empty)
                return string.Empty;

            return str;
        }

        public TemplatePage DefaultResolveLayout(TemplatePage page)
        {
            page.PageVars.TryGetValue(TemplatePages.Layout, out string layout);
            return page.Context.Pages.ResolveLayoutPage(page, layout);
        }
    }

    public class HtmlPageFormat : PageFormat
    {
        public HtmlPageFormat()
        {
            ArgsPrefix = "<!--";
            ArgsSuffix = "-->";
            Extension = "html";
            ContentType = MimeTypes.Html;
            EncodeValue = HtmlEncodeValue;
            ResolveLayout = HtmlResolveLayout;
        }
        
        public static string HtmlEncodeValue(object value)
        {
            if (value == null)
                return string.Empty;
            
            if (value is IHtmlString htmlString)
                return htmlString.ToHtmlString();

            if (value is IRawString rawString)
                return rawString.ToRawString();
            
            var str = value.ToString();
            if (str == string.Empty)
                return string.Empty;

            return StringUtils.HtmlEncode(str);
        }

        public TemplatePage HtmlResolveLayout(TemplatePage page)
        {
            var isCompletePage = page.BodyContents.StartsWith("<!DOCTYPE HTML>") || page.BodyContents.StartsWithIgnoreCase("<html");
            if (isCompletePage)
                return null;

            return base.DefaultResolveLayout(page);
        }
    }
}
