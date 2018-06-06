using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    /// <summary>
    /// Handlebars.js like with block
    /// Usages: {{#with person}} Hi {{name}}, I'm {{age}} years old{{/with}}
    ///         {{#with person}} Hi {{name}}, I'm {{age}} years old {{else}} no person {{/with}}
    /// </summary>
    public class TemplateWithBlock : TemplateBlock
    {
        public override string Name => "with";

        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            var result = await fragment.Argument.GetJsExpressionAndEvaluateAsync(scope,
                ifNone: () => throw new NotSupportedException("'with' block does not have a valid expression"));

            if (result != null)
            {
                var resultAsMap = result.ToObjectDictionary();
    
                var withScope = scope.ScopeWithParams(resultAsMap);
                
                await WriteBodyAsync(withScope, fragment, cancel);
            }
            else
            {
                await WriteElseBlocks(scope, fragment.ElseBlocks, cancel);
            }
        }
    }
}