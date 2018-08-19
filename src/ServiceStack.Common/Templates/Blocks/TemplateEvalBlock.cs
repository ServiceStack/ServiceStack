using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    /// <summary>
    /// Special block which evaluates the rendered body as a ServiceStack Template
    ///
    /// Usages: {{#eval}}emit {{evaluateBodyOfArg}} at {{now}} {{/eval}}
    ///         {{#eval {scopeArg:1} }}emit {{evaluateBodyOfArg}} at {{now}} with {{scopeArg}} {{/eval}}
    ///         {{#eval {htmlDecode:true} }}emit htmldecoded {{evaluateBodyOfArg}} at {{now}} {{/eval}}
    ///         {{#eval {use:{filters:'TemplateServiceStackFilters',plugins:['MarkdownTemplatePlugin'],context:true} }}
    ///              emit {{evaluateBodyOfArg}} at {{now}} in new context
    ///          {{/eval}}
    /// </summary>
    public class TemplateEvalBlock : TemplateBlock
    {
        public override string Name => "eval";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var argValue = block.Argument.GetJsExpressionAndEvaluate(scope);
            var args = argValue as Dictionary<string, object> ?? new Dictionary<string, object>();

            var format = scope.Context.PageFormats.First().Extension;
            if (args.TryGetValue(TemplateConstants.Format, out var oFormat))
            {
                format = oFormat.ToString();
                args.Remove(TemplateConstants.Format);
            }
            
            var htmlDecode = false;
            if (args.TryGetValue(nameof(htmlDecode), out var oHtmlDecode) 
                && oHtmlDecode is bool b)
            {
                htmlDecode = b;
                args.Remove(nameof(htmlDecode));
            }

            var context = new TemplateContext();
            if (args.TryGetValue("use", out var oUse))
            {
                var use = (Dictionary<string, object>) oUse;
                if (use.TryGetValue("context", out var oContext) && oContext is bool useContext && useContext)
                {
                    context = scope.Context;
                }
                else
                {
                    // Use same ThreadSafe plugin instance to preserve configuration 
                    var plugins = use.TryGetValue("plugins", out var oPlugins)
                        ? ToStrings("plugins", oPlugins)
                        : null;
                    if (plugins != null)
                    {
                        foreach (var name in plugins)
                        {
                            var plugin = scope.Context.Plugins.FirstOrDefault(x => x.GetType().Name == name);
                            if (plugin == null)
                                throw new NotSupportedException($"Plugin '{name}' is not registered in parent context");
                            
                            context.Plugins.Add(plugin);
                        }
                    }

                    // Use new filter and block instances which cannot be shared between contexts
                    var filters = use.TryGetValue("filters", out var oFilters)
                        ? ToStrings("filters", oFilters)
                        : null;
                    if (filters != null)
                    {
                        foreach (var name in filters)
                        {
                            var filter = scope.Context.TemplateFilters.FirstOrDefault(x => x.GetType().Name == name);
                            if (filter == null)
                                throw new NotSupportedException($"Filter '{name}' is not registered in parent context");
                            
                            context.TemplateFilters.Add(filter.GetType().CreateInstance<TemplateFilter>());
                        }
                    }
                    
                    var blocks = use.TryGetValue("blocks", out var oBlocks)
                        ? ToStrings("blocks", oBlocks)
                        : null;
                    if (blocks != null)
                    {
                        foreach (var name in blocks)
                        {
                            var useBlock = scope.Context.TemplateBlocks.FirstOrDefault(x => x.GetType().Name == name);
                            if (useBlock == null)
                                throw new NotSupportedException($"Block '{name}' is not registered in parent context");
                            
                            context.TemplateBlocks.Add(useBlock.GetType().CreateInstance<TemplateBlock>());
                        }
                    }
                }

                args.Remove(nameof(use));
            }
            context.Init();
            
            var unrenderedBody = new TemplatePartialPage(scope.Context, "eval-page", block.Body, format, args);

            using (var ms = MemoryStreamFactory.GetStream())
            {
                var captureScope = scope.ScopeWith(outputStream:ms, scopedParams:args);
                await scope.PageResult.WritePageAsync(unrenderedBody, captureScope, token);
                var renderedBody = await ms.ReadToEndAsync();

                if (htmlDecode)
                {
                    renderedBody = renderedBody.HtmlDecode();
                }
                
                var pageResult = new PageResult(context.OneTimePage(renderedBody)) {
                    Args = args,
                };
                await pageResult.WriteToAsync(scope.OutputStream, token);
            }
        }

        IEnumerable<string> ToStrings(string name, object arg)
        {
            if (arg == null)
                return null;
            
            var strings = arg is IEnumerable<string> ls
                ? ls
                : arg is string s
                    ? new List<string> {s}
                    : arg is IEnumerable<object> e
                        ? e.Map(x => (string) x)
                        : throw new NotSupportedException($"{name} expected a collection of strings but was '{arg.GetType().Name}'");

            return strings;
        }
    }
}