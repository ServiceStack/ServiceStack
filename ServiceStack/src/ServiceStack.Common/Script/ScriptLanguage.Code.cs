using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

#if !NET6_0_OR_GREATER
using ServiceStack.Extensions;
#endif

namespace ServiceStack.Script;

/// <summary>
/// Inverse of the #Script Language Template Syntax where each line is a statement
/// i.e. in contrast to #Script's default where text contains embedded template expressions {{ ... }} 
/// </summary>
public sealed class ScriptCode : ScriptLanguage
{
    private ScriptCode() {} // force usage of singleton

    public static readonly ScriptLanguage Language = new ScriptCode();
    
    public override string Name => "code";

    public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
    {
        var quiet = false;
        
        if (!modifiers.IsEmpty)
        {
            quiet = modifiers.EqualsOrdinal("q") || modifiers.EqualsOrdinal("quiet") || modifiers.EqualsOrdinal("mute");
            if (!quiet)
                throw new NotSupportedException($"Unknown modifier '{modifiers.ToString()}', expected 'code|q', 'code|quiet' or 'code|mute'");
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
            
            await page.WriteStatementsAsync(scope, blockStatements, token).ConfigAwait();
            
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
            if (value != null && !ReferenceEquals(value, JsNull.Value) && value != StopExecution.Value && value != IgnoreResult.Value)
            {
                var strValue = page.Format.EncodeValue(value);
                if (!string.IsNullOrEmpty(strValue))
                {
                    var bytes = strValue.ToUtf8Bytes();
                    await scope.OutputStream.WriteAsync(bytes, token).ConfigAwait();
                }
                await scope.OutputStream.WriteAsync(JsTokenUtils.NewLineUtf8, token).ConfigAwait();
            }
        }
        else if (statement is JsFilterExpressionStatement filterStatement)
        {
            await page.WritePageFragmentAsync(scope, filterStatement.FilterExpression, token).ConfigAwait();
            if (!page.Context.RemoveNewLineAfterFiltersNamed.Contains(filterStatement.FilterExpression.LastFilterName))
            {
                await scope.OutputStream.WriteAsync(JsTokenUtils.NewLineUtf8, token).ConfigAwait();
            }
        }
        else if (statement is JsBlockStatement blockStatement)
        {
            await page.WriteStatementsAsync(scope, blockStatement.Statements, token).ConfigAwait();
        }
        else if (statement is JsPageBlockFragmentStatement pageFragmentStatement)
        {
            await page.WritePageFragmentAsync(scope, pageFragmentStatement.Block, token).ConfigAwait();
        }
        else return false;
        
        return true;
    }
}

public static class ScriptCodeUtils
{
    [Obsolete("Use CodeSharpPage")]
    public static SharpPage CodeBlock(this ScriptContext context, string code) => context.CodeSharpPage(code);

    public static SharpPage CodeSharpPage(this ScriptContext context, string code) 
        => context.Pages.OneTimePage(code, context.PageFormats[0].Extension,p => p.ScriptLanguage = ScriptCode.Language);

    private static void AssertCode(this ScriptContext context)
    {
        if (!context.ScriptLanguages.Contains(ScriptCode.Language))
            throw new NotSupportedException($"ScriptCode.Language is not registered in {context.GetType().Name}.{nameof(context.ScriptLanguages)}");
    }

    private static PageResult GetCodePageResult(ScriptContext context, string code, Dictionary<string, object> args)
    {
        context.AssertCode();
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
        return pageResult.RenderScript();
    }

    public static async Task<string> RenderCodeAsync(this ScriptContext context, string code, Dictionary<string, object> args=null)
    {
        var pageResult = GetCodePageResult(context, code, args);
        return await pageResult.RenderScriptAsync().ConfigAwait();
    }

    public static JsBlockStatement ParseCode(this ScriptContext context, string code) =>
        context.ParseCode(code.AsMemory());

    public static JsBlockStatement ParseCode(this ScriptContext context, ReadOnlyMemory<char> code)
    {
        var statements = context.ParseCodeStatements(code);
        return new JsBlockStatement(statements);
    }

    public static string EnsureReturn(string code)
    {
        if (code == null)
            throw new ArgumentNullException(nameof(code));
        
        // if code doesn't contain a return, wrap and return the expression
        if (code.IndexOf(ScriptConstants.Return,StringComparison.Ordinal) == -1)
            code = ScriptConstants.Return + "(" + code + ")";
        return code;
    }

    public static T EvaluateCode<T>(this ScriptContext context, string code, Dictionary<string, object> args = null) =>
        context.EvaluateCode(code, args).ConvertTo<T>();
    
    public static object EvaluateCode(this ScriptContext context, string code, Dictionary<string, object> args=null)
    {
        var pageResult = GetCodePageResult(context, code, args);

        if (!pageResult.EvaluateResult(out var returnValue))
            throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
        
        return ScriptLanguage.UnwrapValue(returnValue);
    }

    public static async Task<T> EvaluateCodeAsync<T>(this ScriptContext context, string code, Dictionary<string, object> args = null) =>
        (await context.EvaluateCodeAsync(code, args).ConfigAwait()).ConvertTo<T>();
    
    public static async Task<object> EvaluateCodeAsync(this ScriptContext context, string code, Dictionary<string, object> args=null)
    {
        var pageResult = GetCodePageResult(context, code, args);

        var ret = await pageResult.EvaluateResultAsync().ConfigAwait();
        if (!ret.Item1)
            throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
        
        return ScriptLanguage.UnwrapValue(ret.Item2);
    }
    

    internal static JsStatement[] ParseCodeStatements(this ScriptContext context, ReadOnlyMemory<char> code)
    {
        var to = new List<JsStatement>();

        int startExpressionPos = -1;
        
        var cursorPos = 0;
        while (code.TryReadLine(out var line, ref cursorPos))
        {
            var lineLength = line.Length;
            line = line.TrimStart();
            var leftIndent = lineLength - line.Length;
            line = line.TrimEnd();
            var rightIndent = lineLength - leftIndent - line.Length;
                
            if (line.IsEmpty)
                continue;

            var firstChar = line.Span[0];

            // single-line comment
            if (firstChar == '*')
            {
                if (line.EndsWith("*"))
                    continue;
            }
            // multi-line comment
            if (line.StartsWith("{{*"))
            {
                var endPos = code.IndexOf("*}}", cursorPos - lineLength);
                if (endPos == -1)
                    throw new SyntaxErrorException($"Unterminated multi-line comment, near {line.DebugLiteral()}");

                cursorPos = endPos + 3; // "*}}".Length
                continue;
            }

            // template block statement
            if (firstChar == '{' && line.Span.SafeCharEquals(1, '{') && line.Span.SafeCharEquals(2, '#'))
            {
                var fromLineStart = code.ToLineStart(cursorPos, lineLength).AdvancePastWhitespace();
                var literal = fromLineStart.Slice(3);

                literal = literal.ParseTemplateScriptBlock(context, out var blockFragment);
                blockFragment.OriginalText = fromLineStart.Slice(0, fromLineStart.Length - literal.Length);
                to.Add(new JsPageBlockFragmentStatement(blockFragment));

                cursorPos = code.Length - literal.Length;
                continue;
            }
            
            // code block statement
            if (firstChar == '#') 
            {
                var fromLineStart = code.ToLineStart(cursorPos, lineLength).AdvancePastWhitespace();
                var literal = fromLineStart.Slice(1);

                literal = literal.ParseCodeScriptBlock(context, out var blockFragment);
                to.Add(new JsPageBlockFragmentStatement(blockFragment));
                
                blockFragment.OriginalText = fromLineStart.Slice(0, fromLineStart.Length - literal.Length);

                cursorPos = code.Length - literal.Length;
                continue;
            }

            const int delim = 2; // '}}'.length
            // multi-line expression
            if (startExpressionPos >= 0)
            {
                // multi-line end
                if (line.EndsWith("}}"))
                {
                    if (code.Span.SafeCharEquals(startExpressionPos, '*'))
                    {
                        if (!line.EndsWith("*}}")) // not a closing block comment, continue
                            continue;
                        
                        // ignore multi-line comment
                    }
                    else
                    {
                        var CRLF = code.Span.SafeCharEquals(cursorPos - 2, '\r') ? 2 : 1;
                        var exprStr = code.Slice(startExpressionPos,  cursorPos - startExpressionPos - rightIndent - delim - CRLF).Trim();
                        var afterExpr = exprStr.Span.ParseExpression(out var expr, out var filters);
                    
                        to.AddExpression(exprStr, expr, filters);
                    }
                    
                    startExpressionPos = -1;
                }
                continue;
            }

            if (firstChar == '{' && line.Span.SafeCharEquals(1, '{'))
            {
                // single-line {{ expr }}
                if (line.EndsWith("}}"))
                {
                    var exprStr = code.Slice(cursorPos - lineLength + leftIndent + delim);
                    exprStr = exprStr.Slice(0, exprStr.IndexOf("}}")).Trim();
                    var afterExpr = exprStr.Span.ParseExpression(out var expr, out var filters);
                    
                    to.AddExpression(exprStr, expr, filters);
                    continue;
                }
                
                // multi-line start
                var CRLF = code.Span.SafeCharEquals(cursorPos - 2, '\r') ? 2 : 1;
                startExpressionPos = cursorPos - lineLength - CRLF + leftIndent + delim;
                continue;
            }
            else
            {
                // treat line as an expression statement
                var afterExpr = line.Span.ParseExpression(out var expr, out var filters);
                afterExpr = afterExpr.AdvancePastWhitespace();

                var isStatementDelim = afterExpr.FirstCharEquals(ScriptTemplateUtils.StatementsSep);

                if (!afterExpr.IsEmpty && !isStatementDelim)
                    throw new SyntaxErrorException($"Unexpected syntax after expression: {afterExpr.ToString()}, near {line.DebugLiteral()}");

                var exprSrc = line.SafeSlice(0, line.Length - afterExpr.Length);
                to.AddExpression(exprSrc, expr, filters);

                if (isStatementDelim)
                    cursorPos = cursorPos - afterExpr.Length + 1;
            }
        }

        return to.ToArray();
    }
    
    // #if ...
    //  ^
    public static ReadOnlyMemory<char> ParseCodeScriptBlock(this ReadOnlyMemory<char> literal, ScriptContext context, 
        out PageBlockFragment blockFragment)
    {
        literal = literal.ParseVarName(out var blockNameSpan);
        var endArgumentPos = literal.IndexOf('\n');
        var argument = literal.Slice(0, endArgumentPos).Trim();

        literal = literal.Slice(endArgumentPos + 1);

        var blockName = blockNameSpan.ToString();

        var language = context.ParseAsLanguage.TryGetValue(blockName, out var lang)
            ? lang
            : ScriptCode.Language;

        if (language.Name == ScriptVerbatim.Language.Name)
        {
            literal = literal.ParseCodeBody(blockNameSpan, out var body);
            body = body.ChopNewLine();

            blockFragment = language.ParseVerbatimBlock(blockName, argument, body);
            return literal;
        }

        literal = literal.ParseCodeBody(blockNameSpan, out var bodyText);
        var bodyFragments = language.Parse(context, bodyText);

        var elseBlocks = new List<PageElseBlock>();

        literal = literal.AdvancePastWhitespace();
        while (literal.StartsWith("else"))
        {
            literal = literal.ParseCodeElseBlock(blockNameSpan, out var elseArgument,  out var elseBody);
                
            var elseBlock = new PageElseBlock(elseArgument, language.Parse(context, elseBody));
            elseBlocks.Add(elseBlock);

            literal = literal.AdvancePastWhitespace();
        }
        
        blockFragment = new PageBlockFragment(blockName, argument, bodyFragments, elseBlocks);

        return literal;
    }
    
    // cursorPos is after CRLF except at end where its at last char

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlyMemory<char> FromStartToPreviousLine(this ReadOnlyMemory<char> literal, int cursorPos, int lineLength)
    {
        var ret = literal.Slice(0, cursorPos - lineLength);
        while (!ret.Span.SafeCharEquals(ret.Length - 1, '\n'))
        {
            ret = ret.Slice(0, ret.Length - 1);

            if (ret.Length == 0) // no previous line, so return empty string
                return default;
        }
        
        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlyMemory<char> ToLineStart(this ReadOnlyMemory<char> literal, int cursorPos, int lineLength)
    {
        var CLRF = literal.Span.SafeCharEquals(cursorPos - 2, '\r');
        var ret = literal.Slice(cursorPos - lineLength -
             (cursorPos == literal.Length ? 0 : 1) -
             (CLRF ? 1 : 0));
        
        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlyMemory<char> ToLineStart(this ReadOnlyMemory<char> literal, int cursorPos, int lineLength, int statementPos)
    {
        var ret = literal.Slice(statementPos, cursorPos - statementPos - lineLength);
        while (!ret.Span.SafeCharEquals(ret.Length - 1, '\n'))
        {
            ret = ret.Slice(0, ret.Length - 1);

            if (ret.Length == 0) // no previous line, so return empty string
                return default;
        }
        
        return ret;
    }

    //  #block arg\n  
    //              ^
    //  else
    //  /block
    internal static ReadOnlyMemory<char> ParseCodeBody(this ReadOnlyMemory<char> literal, ReadOnlyMemory<char> blockName, out ReadOnlyMemory<char> body)
    {
        var inStatements = 0;
        
        var cursorPos = 0;
        while (literal.TryReadLine(out var line, ref cursorPos))
        {
            var lineLength = line.Length;
            line = line.Trim();
            if (line.IsEmpty)
                continue;

            var c = line.Span[0];

            if (c == '#')
            {
                inStatements++;
                continue;
            }

            if (c == '/')
            {
                if (inStatements == 0)
                {
                    line.Slice(1).ParseVarName(out var name);
                    if (name.EqualsOrdinal(blockName))
                    {
                        body = literal.FromStartToPreviousLine(cursorPos, lineLength);
                        var ret = literal.Slice(cursorPos);
                        return ret;
                    }
                }

                inStatements--;
            }
            else if (line.StartsWith("else"))
            {
                if (inStatements == 0)
                {
                    body = literal.FromStartToPreviousLine(cursorPos, lineLength);
                    var ret = literal.ToLineStart(cursorPos, lineLength);
                    return ret;
                }
            }
        }
        
        throw new SyntaxErrorException($"End block for '{blockName.ToString()}' not found.");
    }
    
    //  else if a=b  
    //  ^
    //  else
    //  ^
    //  /block
    internal static ReadOnlyMemory<char> ParseCodeElseBlock(this ReadOnlyMemory<char> literal, ReadOnlyMemory<char> blockName, 
        out ReadOnlyMemory<char> elseArgument, out ReadOnlyMemory<char> elseBody)
    {
        var inStatements = 0;
        var statementPos = -1;
        elseBody = default;
        elseArgument = default;
        
        var cursorPos = 0;
        while (literal.TryReadLine(out var line, ref cursorPos))
        {
            var lineLength = line.Length;
            line = line.Trim();
            if (line.IsEmpty)
                continue;

            var c = line.Span[0];

            if (c == '#')
            {
                inStatements++;
            }
            else if (c == '/')
            {
                if (inStatements == 0)
                {
                    line.Slice(1).ParseVarName(out var name);
                    if (name.EqualsOrdinal(blockName))
                    {
                        elseBody = literal.ToLineStart(cursorPos, lineLength, statementPos);
                        elseBody = elseBody.Trim();
                        var ret = literal.Slice(cursorPos);
                        return ret;
                    }
                }

                inStatements--;
            }
            else if (line.StartsWith("else"))
            {
                if (inStatements == 0)
                {
                    if (statementPos >= 0)
                    {
                        elseBody = literal.Slice(statementPos, (cursorPos - lineLength) - statementPos).Trim();
                        var ret = literal.Slice(cursorPos - lineLength);
                        return ret;
                    }

                    elseArgument = line.Slice(4).Trim();
                    statementPos = cursorPos;
                }
            }
        }
        
        throw new SyntaxErrorException($"End 'else' statement not found.");
    }

    internal static ReadOnlySpan<char> ParseExpression(this ReadOnlySpan<char> literal, out JsToken expr, out List<JsCallExpression> filters)
    {
        literal = literal.ParseJsExpression(out expr, filterExpression: true);
        filters = null;


        literal = literal.AdvancePastWhitespace();
        if (literal.FirstCharEquals(ScriptTemplateUtils.FilterSep))
        {
            filters = new List<JsCallExpression>();
            literal = literal.AdvancePastPipeOperator();

            while (true)
            {
                literal = literal.ParseJsCallExpression(out var filter, filterExpression: true);

                filters.Add(filter);

                literal = literal.AdvancePastWhitespace();

                if (literal.IsNullOrEmpty() || literal.FirstCharEquals(ScriptTemplateUtils.StatementsSep))
                    return literal;

                if (!literal.FirstCharEquals(ScriptTemplateUtils.FilterSep))
                    throw new SyntaxErrorException($"Expected filter separator '|' but was {literal.DebugFirstChar()}");

                literal = literal.AdvancePastPipeOperator();
            }
        }
        else if (literal.FirstCharEquals(ScriptTemplateUtils.StatementsSep))
        {
            return literal;
        }
        else if (!literal.AdvancePastWhitespace().IsNullOrEmpty())
        {
            throw new SyntaxErrorException($"Unexpected syntax '{literal.ToString()}', Expected pipeline operator '|>'");
        }

        return literal;
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddExpression(this List<JsStatement> ret, ReadOnlyMemory<char> originalText,
        JsToken expr, List<JsCallExpression> filters)
    {
        if (filters == null)
            ret.Add(new JsExpressionStatement(expr));
        else
            ret.Add(new JsFilterExpressionStatement(originalText, expr, filters));
    }        
}