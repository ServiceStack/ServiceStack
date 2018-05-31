using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates.Blocks
{
    /// <summary>
    /// Handlebars.js like with block
    /// Usage: {{#with person}} Hi {{name}}, I'm {{age}} years old{{/with}}
    /// </summary>
    public class TemplateWithBlock : TemplateBlock
    {
        public override string Name => "with";

        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            if (fragment.Argument.IsNullOrEmpty())
                throw new NotSupportedException("'with' block requires an expression");

            var expr = fragment.Argument.GetCachedJsExpression(scope);
            if (expr == null)
                throw new NotSupportedException("'with' block does not have a valid expression");

            var result = expr.Evaluate(scope);
            var resultAsMap = result.ToObjectDictionary();

            var withScope = scope.ScopeWithParams(resultAsMap);
            
            await WriteBodyAsync(withScope, fragment, cancel);
        }
    }
}