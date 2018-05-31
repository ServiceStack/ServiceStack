using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates.Blocks
{
    public class TemplateIfBlock : TemplateBlock
    {
        public override string Name => "if";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken ct)
        {
            fragment.Argument.ParseJsExpression(out var token);
            if (token == null)
                throw new NotSupportedException("'if' block does not have an expression");

            var result = token.EvaluateToBool(scope);
            if (result)
            {
                await WriteBodyAsync(scope, fragment, ct);
                return;
            }

            foreach (var elseBlock in fragment.ElseBlocks)
            {
                if (elseBlock.Argument.IsNullOrEmpty())
                {
                    await WriteElseAsync(scope, elseBlock, ct);
                    return;
                }

                var argument = elseBlock.Argument;
                if (argument.StartsWith("if "))
                    argument = argument.Advance(3);

                argument.ParseJsExpression(out token);
                if (token == null)
                    throw new NotSupportedException("'if else' block does not have a valid expression");

                result = token.EvaluateToBool(scope);
                if (result)
                {
                    await WriteElseAsync(scope, elseBlock, ct);
                    return;
                }
            }
        }
    }
}