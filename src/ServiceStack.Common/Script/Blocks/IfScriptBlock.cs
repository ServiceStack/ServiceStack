using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Script
{
    /// <summary>
    /// Handlebars.js like if block
    /// Usages: {{#if a > b}} max {{a}} {{/if}}
    ///         {{#if a > b}} max {{a}} {{else}} max {{b}} {{/if}}
    ///         {{#if a > b}} max {{a}} {{else if b > c}} max {{b}} {{else}} max {{c}} {{/if}}
    /// </summary>
    public class IfScriptBlock : ScriptBlock
    {
        public override string Name => "if";
        
        public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var result = await block.Argument.GetJsExpressionAndEvaluateToBoolAsync(scope,
                ifNone: () => throw new NotSupportedException("'if' block does not have a valid expression"));

            if (result)
            {
                await WriteBodyAsync(scope, block, token);
            }
            else
            {
                await WriteElseAsync(scope, block.ElseBlocks, token);
            }
        }
    }
}