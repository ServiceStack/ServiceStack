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
            if (fragment.Argument.IsNullOrEmpty())
                throw new NotSupportedException("'if' block requires an expression");

            var ifExpr = fragment.Argument.GetCachedJsExpression(scope);
            if (ifExpr == null)
                throw new NotSupportedException("'if' block does not have a valid expression");

            var result = ifExpr.EvaluateToBool(scope);
            if (result)
            {
                await WriteBodyAsync(scope, fragment, cancel);
                return;
            }

            foreach (var elseBlock in fragment.ElseBlocks)
            {
                if (elseBlock.Argument.IsNullOrEmpty())
                {
                    await WriteElseAsync(scope, elseBlock, cancel);
                    return;
                }
                
                var argument = elseBlock.Argument;
                if (argument.StartsWith("if "))
                    argument = argument.Advance(3);

                var elseExpr = argument.GetCachedJsExpression(scope);
                if (elseExpr == null)
                    throw new NotSupportedException("'else if' block does not have a valid expression");

                result = elseExpr.EvaluateToBool(scope);
                if (result)
                {
                    await WriteElseAsync(scope, elseBlock, cancel);
                    return;
                }
            }
        }
    }
}