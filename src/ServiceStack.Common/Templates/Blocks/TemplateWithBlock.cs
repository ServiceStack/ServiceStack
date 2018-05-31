using System;
using System.Threading;
using System.Threading.Tasks;

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
            var result = fragment.Argument.GetJsExpressionAndEvaluate(scope,
                ifNone: () => throw new NotSupportedException("'with' block does not have a valid expression"));

            var resultAsMap = result.ToObjectDictionary();

            var withScope = scope.ScopeWithParams(resultAsMap);
            
            await WriteBodyAsync(withScope, fragment, cancel);
        }
    }
}