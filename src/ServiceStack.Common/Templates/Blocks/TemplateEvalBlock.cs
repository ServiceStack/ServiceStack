using System;
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
    ///         {{#eval {safe:true} }}emit {{evaluateBodyOfArg}} at {{now}} in new context {{/eval}}
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

            var context = args.TryGetValue("safe", out var oSafe) && oSafe is bool safe && safe 
                ? new TemplateContext().Init()
                : scope.Context;
            
            var unrenderedBody = new TemplatePartialPage(scope.Context, "eval-page", block.Body, format, args);

            using (var ms = MemoryStreamFactory.GetStream())
            {
                var captureScope = scope.ScopeWith(outputStream:ms, scopedParams:args);
                await scope.PageResult.WritePageAsync(unrenderedBody, captureScope, token);
                var renderedBody = await ms.ReadToEndAsync();
                
                var pageResult = new PageResult(context.OneTimePage(renderedBody)) {
                    Args = args,
                };
                await pageResult.WriteToAsync(scope.OutputStream, token);
            }
        }
    }
}