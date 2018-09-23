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
    ///         {{/eval}}
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

            var context = scope.CreateNewContext(args);
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
    }
}