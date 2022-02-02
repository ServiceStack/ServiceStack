using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Script
{
    /// <summary>
    /// Handlebars.js like with block
    /// Usages: {{#with person}} Hi {{name}}, I'm {{age}} years old{{/with}}
    ///         {{#with person}} Hi {{name}}, I'm {{age}} years old {{else}} no person {{/with}}
    /// </summary>
    public class WithScriptBlock : ScriptBlock
    {
        public override string Name => "with";

        public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var result = await block.Argument.GetJsExpressionAndEvaluateAsync(scope,
                ifNone: () => throw new NotSupportedException("'with' block does not have a valid expression"));

            if (result != null)
            {
                var resultAsMap = result.ToObjectDictionary();
    
                var withScope = scope.ScopeWithParams(resultAsMap);
                
                await WriteBodyAsync(withScope, block, token);
            }
            else
            {
                await WriteElseAsync(scope, block.ElseBlocks, token);
            }
        }
    }
}