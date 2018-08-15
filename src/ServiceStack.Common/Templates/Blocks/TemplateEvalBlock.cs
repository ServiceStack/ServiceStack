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
    /// Special block which evaluates the raw body as a ServiceStack Template in a new TemplateContext
    ///
    /// Usages: {{#eval}}emit {{`evaluated body`}} at {{now}} {{/eval}}
    ///         {{#eval {scopeArg:1} }}emit {{`evaluated body`}} at {{now}} with {{scopeArg}} {{/eval}}
    /// </summary>
    public class TemplateEvalBlock : TemplateBlock
    {
        public override string Name => "eval";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var context = new TemplateContext().Init();
            
            var argValue = block.Argument.GetJsExpressionAndEvaluate(scope);
            var args = argValue as Dictionary<string, object>;

            var format = scope.Context.PageFormats.First().Extension;
            if (args != null && args.TryGetValue("format", out var oFormat))
            {
                format = oFormat.ToString();
                args.Remove("format");
            }
            
            var page = new TemplatePartialPage(context, "eval-page", block.Body, format, args);
            var pageResult = new PageResult(page) {
                Args = args
            };

            await pageResult.WriteToAsync(scope.OutputStream, token);
        }
    }
}