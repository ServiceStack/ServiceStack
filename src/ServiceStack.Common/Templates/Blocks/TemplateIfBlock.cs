using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates.Blocks
{
    /// <summary>
    /// Handlebars.js like if block
    /// Usages: {{#if a > b}} max {{a}} {{/if}}
    ///         {{#if a > b}} max {{a}} {{else}} max {{b}} {{/if}}
    ///         {{#if a > b}} max {{a}} {{else if b > c}} max {{b}} {{else}} max {{c}} {{/if}}
    /// </summary>
    public class TemplateIfBlock : TemplateBlock
    {
        public override string Name => "if";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            var result = fragment.Argument.GetJsExpressionAndEvaluateToBool(scope,
                ifNone: () => throw new NotSupportedException("'if' block does not have a valid expression"));
            if (result)
            {
                await WriteBodyAsync(scope, fragment, cancel);
                return;
            }

            await WriteElseBlocks(scope, fragment.ElseBlocks, cancel);
        }
    }
}