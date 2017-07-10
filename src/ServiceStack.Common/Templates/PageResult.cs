using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public interface IPageResult {}

    // Render a Template Page to the Response OutputStream
    public class PageResult : IPageResult, IStreamWriterAsync, IHasOptions
    {
        public TemplatePage Page { get; set; }

        public TemplatePage LayoutPage { get; set; }

        public object Model { get; set;  }
        
        /// <summary>
        /// Add additional Args available to all pages 
        /// </summary>
        public Dictionary<string, object> Args { get; set; }

        /// <summary>
        /// Add additional template filters available to all pages
        /// </summary>
        public List<TemplateFilter> TemplateFilters { get; set; }

        public IDictionary<string, string> Options { get; set; }

        /// <summary>
        /// Specify the Content-Type of the Response 
        /// </summary>
        public string ContentType
        {
            get => Options.TryGetValue(HttpHeaders.ContentType, out string contentType) ? contentType : null;
            set => Options[HttpHeaders.ContentType] = value;
        }
        
        /// <summary>
        /// Transform the Page output using a chain of filters
        /// </summary>
        public List<Func<Stream,Task<Stream>>> PageFilters { get; set; }

        /// <summary>
        /// Transform the entire output using a chain of filters
        /// </summary>
        public List<Func<Stream,Task<Stream>>> OutputFilters { get; set; }

        public PageResult(TemplatePage page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Args = new Dictionary<string, object>();
            TemplateFilters = new List<TemplateFilter>();
            PageFilters = new List<Func<Stream,Task<Stream>>>();
            OutputFilters = new List<Func<Stream,Task<Stream>>>();
            Options = new Dictionary<string, string>
            {
                { HttpHeaders.ContentType, Page.Format.ContentType },
            };
        }

        public async Task WriteToAsync(Stream responseStream, CancellationToken token = default(CancellationToken))
        {
            if (OutputFilters.Count == 0)
            {
                await WriteToAsyncInternal(responseStream, token);
                return;
            }

            using (var ms = MemoryStreamFactory.GetStream())
            {
                await WriteToAsyncInternal(ms, token);
                Stream stream = ms;

                foreach (var filter in OutputFilters)
                {
                    stream.Position = 0;
                    stream = await filter(stream);
                }

                using (stream)
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(responseStream);
                }
            }
        }

        internal async Task WriteToAsyncInternal(Stream responseStream, CancellationToken token)
        {
            if (Model != null)
            {
                var explodeModel = Model.ToObjectDictionary();
                foreach (var entry in explodeModel)
                {
                    Args[entry.Key] = entry.Value;
                }
            }
            Args[TemplateConstants.Model] = Model ?? NullValue.Instance;
            
            if (LayoutPage != null)
            {
                await Task.WhenAll(LayoutPage.Init(), Page.Init());
            }
            else
            {
                await Page.Init();
                if (Page.LayoutPage != null)
                {
                    LayoutPage = Page.LayoutPage;
                    await LayoutPage.Init();
                }
            }

            token.ThrowIfCancellationRequested();

            if (LayoutPage != null)
            {
                foreach (var fragment in LayoutPage.PageFragments)
                {
                    if (fragment is PageStringFragment str)
                    {
                        await responseStream.WriteAsync(str.ValueBytes, token);
                    }
                    else if (fragment is PageVariableFragment var)
                    {
                        if (var.Name.Equals("page"))
                        {
                            await WritePageAsync(responseStream, token);
                        }
                        else
                        {
                            await responseStream.WriteAsync(EvaluateVarAsBytes(var), token);
                        }
                    }
                }
            }
            else
            {
                await WritePageAsync(responseStream, token);
            }
        }

        public async Task WritePageAsync(Stream responseStream, CancellationToken token = default(CancellationToken))
        {
            if (PageFilters.Count == 0)
            {
                await WritePageAsyncInternal(responseStream, token);
                return;
            }

            using (var ms = MemoryStreamFactory.GetStream())
            {
                await WritePageAsyncInternal(ms, token);
                Stream stream = ms;

                foreach (var filter in PageFilters)
                {
                    stream.Position = 0;
                    stream = await filter(stream);
                }

                using (stream)
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(responseStream);
                }
            }
        }

        internal async Task WritePageAsyncInternal(Stream responseStream, CancellationToken token = default(CancellationToken))
        {
            foreach (var fragment in Page.PageFragments)
            {
                if (fragment is PageStringFragment str)
                {
                    await responseStream.WriteAsync(str.ValueBytes, token);
                }
                else if (fragment is PageVariableFragment var)
                {
                    await responseStream.WriteAsync(EvaluateVarAsBytes(var), token);
                }
            }
        }

        private byte[] EvaluateVarAsBytes(PageVariableFragment var)
        {
            var value = Evaluate(var);
            var bytes = value != null
                ? Page.Format.EncodeValue(value).ToUtf8Bytes()
                : var.OriginalTextBytes;
            return bytes;
        }
        
        private object Evaluate(PageVariableFragment var)
        {
            var value = var.Value ?? GetValue(var.NameString);

            if (value == null)
                return null;

            for (var i = 0; i < var.FilterCommands.Length; i++)
            {
                var cmd = var.FilterCommands[i];
                var invoker = GetFilterInvoker(cmd.Name, 1 + cmd.Args.Count, out TemplateFilter filter);
                if (invoker == null)
                {
                    if (i == 0)
                        return null; // ignore on server if first filter is missing  
                    
                    throw new Exception($"Filter not found: {cmd} in '{Page.File.VirtualPath}'"); 
                }

                var args = new object[1 + cmd.Args.Count];
                args[0] = value;

                for (var cmdIndex = 0; cmdIndex < cmd.Args.Count; cmdIndex++)
                {
                    var arg = cmd.Args[cmdIndex];
                    var varValue = Evaluate(var, arg);

                    args[1 + cmdIndex] = varValue;
                }

                value = InvokeFilter(invoker, filter, args, cmd);
            }

            if (value == null)
                return string.Empty; // treat as empty value if evaluated to null

            return value;
        }

        private static object InvokeFilter(MethodInvoker invoker, TemplateFilter filter, object[] args, Command cmd)
        {
            try
            {
                return invoker(filter, args);
            }
            catch (Exception ex)
            {
                var argStr = args.Map(x => x.ToString()).Join(",");
                throw new TargetInvocationException($"Failed to invoke filter {cmd.Name}({argStr})", ex);
            }
        }

        private object Evaluate(PageVariableFragment var, StringSegment arg)
        {
            var.ParseLiteral(arg, out StringSegment outName, out object outValue, out Command cmd);

            if (!outName.IsNullOrEmpty())
            {
                return GetValue(outName.Value);
            }
            if (cmd != null)
            {
                var value = Evaluate(var, cmd);
                return value;
            }
            return outValue;
        }

        private object Evaluate(PageVariableFragment var, Command cmd)
        {
            var invoker = GetFilterInvoker(cmd.Name, cmd.Args.Count, out TemplateFilter filter);

            var args = new object[cmd.Args.Count];
            for (var i = 0; i < cmd.Args.Count; i++)
            {
                var arg = cmd.Args[i];
                var varValue = Evaluate(var, arg);
                args[i] = varValue;
            }
            
            var value = InvokeFilter(invoker, filter, args, cmd);
            return value;
        }
        
        private MethodInvoker GetFilterInvoker(StringSegment name, int argsCount, out TemplateFilter filter)
        {
            foreach (var tplFilter in TemplateFilters)
            {
                var invoker = tplFilter.GetInvoker(name, argsCount);
                if (invoker != null)
                {
                    filter = tplFilter;
                    return invoker;
                }
            }

            foreach (var tplFilter in Page.Context.TemplateFilters)
            {
                var invoker = tplFilter.GetInvoker(name, argsCount);
                if (invoker != null)
                {
                    filter = tplFilter;
                    return invoker;
                }
            }

            filter = null;
            return null;
        }

        private object GetValue(string name)
        {
            var value = Args.TryGetValue(name, out object obj)
                ? obj
                : Page.Args.TryGetValue(name, out string strValue)
                    ? strValue
                    : (LayoutPage != null && LayoutPage.Args.TryGetValue(name, out strValue))
                        ? strValue
                        : Page.Context.Args.TryGetValue(name, out obj)
                            ? obj
                            : null;
            return value;
        }
    }
}