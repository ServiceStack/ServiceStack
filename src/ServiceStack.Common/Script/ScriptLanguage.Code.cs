using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script 
{
    public class CodeScriptLanguage : ScriptLanguage
    {
        public static ScriptLanguage Instance = new CodeScriptLanguage();
        
        public override string Name => "code";

        public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
        {
            var quiet = false;
            
            if (!modifiers.IsEmpty)
            {
                quiet = modifiers.EqualsOrdinal("q") || modifiers.EqualsOrdinal("quiet") || modifiers.EqualsOrdinal("silent");
                if (!quiet)
                    throw new NotSupportedException($"Unknown modifier '{modifiers.ToString()}', expected 'code|q', 'code|quiet' or 'code|silent'");
            }
            
            var statements = context.ParseCodeStatements(body);
            
            return new List<PageFragment> {
                new PageJsBlockStatementFragment(new JsBlockStatement(statements)) {
                    Quiet = quiet,
                },
            };
        }

        public override async Task<bool> WritePageFragmentAsync(ScriptScopeContext scope, PageFragment fragment, CancellationToken token)
        {
            var page = scope.PageResult;
            if (fragment is PageJsBlockStatementFragment blockFragment)
            {
                var blockStatements = blockFragment.Block.Statements;
                if (blockFragment.Quiet && scope.OutputStream != Stream.Null)
                    scope = scope.ScopeWithStream(Stream.Null);
                
                await page.WriteStatementsAsync(scope, blockStatements, token);
                
                return true;
            }
            return false;
        }
        
        public override async Task<bool> WriteStatementAsync(ScriptScopeContext scope, JsStatement statement, CancellationToken token)
        {
            var page = scope.PageResult;
            if (statement is JsExpressionStatement exprStatement)
            {
                var value = exprStatement.Expression.Evaluate(scope);
                if (value != null && value != JsNull.Value && value != StopExecution.Value && value != IgnoreResult.Value)
                {
                    var strValue = page.Format.EncodeValue(value);
                    if (!string.IsNullOrEmpty(strValue))
                    {
                        var bytes = strValue.ToUtf8Bytes();
                        await scope.OutputStream.WriteAsync(bytes, token);
                    }
                    await scope.OutputStream.WriteAsync(JsTokenUtils.NewLineUtf8, token);
                }
            }
            else if (statement is JsFilterExpressionStatement filterStatement)
            {
                await page.WritePageFragmentAsync(scope, filterStatement.FilterExpression, token);
                if (!page.Context.RemoveNewLineAfterFiltersNamed.Contains(filterStatement.FilterExpression.LastFilterName))
                {
                    await scope.OutputStream.WriteAsync(JsTokenUtils.NewLineUtf8, token);
                }
            }
            else if (statement is JsBlockStatement blockStatement)
            {
                await page.WriteStatementsAsync(scope, blockStatement.Statements, token);
            }
            else if (statement is JsPageBlockFragmentStatement pageFragmentStatement)
            {
                await page.WritePageFragmentAsync(scope, pageFragmentStatement.Block, token);
            }
            else return false;
            
            return true;
        }
    }
}