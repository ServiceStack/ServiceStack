using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates.Blocks
{
    public class TemplateIfBlock : TemplateBlock
    {
        public override string Name => "if";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(fragment.ArgumentString))
                throw new NotSupportedException("'if' block requires an expression");

            var cached = (IfCache)scope.Context.Cache.GetOrAdd(fragment.ArgumentString, key => ParseBlock(fragment));

            var result = cached.Expression.EvaluateToBool(scope);
            if (result)
            {
                await WriteBodyAsync(scope, fragment, cancel);
                return;
            }

            foreach (var elseBlock in cached.ElseBlocks)
            {
                result = elseBlock.Expression == null || elseBlock.Expression.EvaluateToBool(scope);
                if (result)
                {
                    await WriteAsync(scope, elseBlock.Body, elseBlock.CallTrace, cancel);
                    return;
                }
            }
        }

        class IfCache
        {
            public readonly string CallTrace;
            public readonly JsToken Expression;
            public readonly PageFragment[] Body;
            public readonly IfCache[] ElseBlocks;
            
            public IfCache(string callTrace, JsToken expression, PageFragment[] body, IfCache[] elseBlocks)
            {
                CallTrace = callTrace;
                Expression = expression;
                Body = body;
                ElseBlocks = elseBlocks;
            }
        }

        private IfCache ParseBlock(PageBlockFragment fragment)
        {
            fragment.Argument.ParseJsExpression(out var ifExpr);
            if (ifExpr == null)
                throw new NotSupportedException("'if' block does not have an expression");
            
            var elseBlocks = new List<IfCache>();
            
            foreach (var elseBlock in fragment.ElseBlocks)
            {
                if (elseBlock.Argument.IsNullOrEmpty())
                {
                    elseBlocks.Add(new IfCache(GetElseCallTrace(elseBlock), null, elseBlock.Body, null));
                    continue;
                }

                var argument = elseBlock.Argument;
                if (argument.StartsWith("if "))
                    argument = argument.Advance(3);

                argument.ParseJsExpression(out var elseExpr);
                if (elseExpr == null)
                    throw new NotSupportedException("'if else' block does not have a valid expression");
                
                elseBlocks.Add(new IfCache(GetElseCallTrace(elseBlock), elseExpr, elseBlock.Body, null));
            }
            
            var ret = new IfCache(GetCallTrace(fragment), ifExpr, fragment.Body, elseBlocks.ToArray());
            return ret;
        }
    }
}