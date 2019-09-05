using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script 
{
    
    /// <summary>
    /// Inverse of the #Script Language Template Syntax where each line is a statement
    /// i.e. in contrast to #Script's default where text contains embedded template expressions {{ ... }} 
    /// </summary>
    public class ScriptCode : ScriptLanguage
    {
        public static readonly ScriptLanguage Language = new ScriptCode();
        
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

    public static class CodeScriptLanguageUtils
    {
        [Obsolete("Use CodeSharpPage")]
        public static SharpPage CodeBlock(this ScriptContext context, string code) => context.CodeSharpPage(code);

        public static SharpPage CodeSharpPage(this ScriptContext context, string code) 
            => context.Pages.OneTimePage(code, context.PageFormats[0].Extension,p => p.ScriptLanguage = ScriptCode.Language);

        private static PageResult GetCodePageResult(ScriptContext context, string code, Dictionary<string, object> args)
        {
            PageResult pageResult = null;
            try
            {
                var page = context.CodeSharpPage(code);
                pageResult = new PageResult(page);
                args.Each((x, y) => pageResult.Args[x] = y);
                return pageResult;
            }
            catch (Exception e)
            {
                if (ScriptContextUtils.ShouldRethrow(e))
                    throw;
                throw ScriptContextUtils.HandleException(e, pageResult ?? new PageResult(context.EmptyPage));
            }
        }

        public static string RenderCode(this ScriptContext context, string code, Dictionary<string, object> args=null)
        {
            var pageResult = GetCodePageResult(context, code, args);
            return pageResult.EvaluateScript();
        }

        public static async Task<string> RenderCodeAsync(this ScriptContext context, string code, Dictionary<string, object> args=null)
        {
            var pageResult = GetCodePageResult(context, code, args);
            return await pageResult.EvaluateScriptAsync();
        }

        public static JsBlockStatement ParseCode(this ScriptContext context, string code) =>
            context.ParseCode(code.AsMemory());

        public static JsBlockStatement ParseCode(this ScriptContext context, ReadOnlyMemory<char> code)
        {
            var statements = context.ParseCodeStatements(code);
            return new JsBlockStatement(statements);
        }

        public static T EvaluateCode<T>(this ScriptContext context, string code, Dictionary<string, object> args = null) =>
            context.EvaluateCode(code, args).ConvertTo<T>();
        
        public static object EvaluateCode(this ScriptContext context, string code, Dictionary<string, object> args=null)
        {
            var pageResult = GetCodePageResult(context, code, args);

            if (!pageResult.EvaluateResult(out var returnValue))
                throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
            
            return returnValue;
        }

        public static async Task<T> EvaluateCodeAsync<T>(this ScriptContext context, string code, Dictionary<string, object> args = null) =>
            (await context.EvaluateCodeAsync(code, args)).ConvertTo<T>();
        
        public static async Task<object> EvaluateCodeAsync(this ScriptContext context, string code, Dictionary<string, object> args=null)
        {
            var pageResult = GetCodePageResult(context, code, args);

            var ret = await pageResult.EvaluateResultAsync();
            if (!ret.Item1)
                throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
            
            return ret.Item2;
        }
        
    }
}