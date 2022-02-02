using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    /// <summary>
    /// while block
    /// Usages: {{#while times > 0}} {{times}}. {{times - 1 | to => times}} {{/while}}
    ///         {{#while b}} {{ false | to => b }} {{else}} {{b}} was false {{/while}}
    /// 
    /// Max Iterations = Context.Args[ScriptConstants.MaxQuota]
    /// </summary>
    public class WhileScriptBlock : ScriptBlock
    {
        public override string Name => "while";
        
        public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken ct)
        {
            var result = await block.Argument.GetJsExpressionAndEvaluateToBoolAsync(scope,
                ifNone: () => throw new NotSupportedException("'while' block does not have a valid expression")).ConfigAwait();

            var iterations = 0;
            
            if (result)
            {
                do
                {
                    await WriteBodyAsync(scope, block, ct);
                    
                    result = await block.Argument.GetJsExpressionAndEvaluateToBoolAsync(scope,
                        ifNone: () => throw new NotSupportedException("'while' block does not have a valid expression"));

                    Context.DefaultMethods.AssertWithinMaxQuota(iterations++);
                    
                } while (result);
            }
            else
            {
                await WriteElseAsync(scope, block.ElseBlocks, ct);
            }
        }
    }
}