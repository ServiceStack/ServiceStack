using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    public abstract class TemplateCodePage : IDisposable
    {
        public string VirtualPath { get; set; }
        public string Layout { get; set; }
        public TemplatePage LayoutPage { get; set; }
        public PageFormat Format { get; set; }
        public Dictionary<string, object> Args { get; } = new Dictionary<string, object>();
        public TemplateContext Context { get; set; }
        public ITemplatePages Pages { get; set; }
        public TemplateScopeContext Scope { get; set; }

        private MethodInfo renderMethod;
        private MethodInvoker renderInvoker;

        protected TemplateCodePage(string layout = null)
        {
            Layout = layout;
        }

        public async Task WriteAsync(TemplateScopeContext scope)
        {
            var renderParams = renderMethod.GetParameters();
            var args = new object[renderParams.Length];
            for (var i = 0; i < renderParams.Length; i++)
            {
                var renderParam = renderParams[i];
                var arg = scope.GetValue(renderParam.Name);
                args[i] = arg;
            }

            try
            {
                var result = renderInvoker(this, args);
                if (result != null)
                {
                    var str = result.ToString();
                    await scope.OutputStream.WriteAsync(str);
                }
            }
            catch (Exception ex)
            {
                throw new TargetInvocationException($"Failed to invoke render method on {GetType().Name}", ex);
            }
        }
        
        public bool HasInit { get; private set; }

        public virtual TemplateCodePage Init()
        {
            if (!HasInit)
            {
                HasInit = true;
                var type = GetType();
                
                if (Format == null)
                    Format = Context.PageFormats.First();
                
                var pageAttr = type.FirstAttribute<PageAttribute>();
                VirtualPath = pageAttr.VirtualPath;
                if (Layout == null)
                    Layout = pageAttr?.Layout;

                LayoutPage = Pages.ResolveLayoutPage(this, Layout);
    
                var pageArgs = type.AllAttributes<PageArgAttribute>();
                foreach (var pageArg in pageArgs)
                {
                    Args[pageArg.Name] = pageArg.Value;
                }

                if (!Context.CodePageInvokers.TryGetValue(type, out Tuple<MethodInfo, MethodInvoker> tuple))
                {                    
                    var method = type.GetInstanceMethods().FirstOrDefault(x => x.Name.EndsWithIgnoreCase("render"));
                    if (method == null)
                        throw new NotSupportedException($"Template Code Page '{GetType().Name}' does not have a 'render' method");
                    
                    var invoker = TypeExtensions.GetInvokerToCache(method);
                    Context.CodePageInvokers[type] = tuple = Tuple.Create(method, invoker);
                }

                renderMethod = tuple.Item1;
                renderInvoker = tuple.Item2;
            }

            return this;
        }

        public virtual void Dispose()
        {
        }
    }
}