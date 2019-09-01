using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public static class SharpCodeUtils
    {
        static ReadOnlyMemory<char> ToPreviousLine(this ReadOnlyMemory<char> literal, int cursorPos, int lineLength) =>
            cursorPos == literal.Length // cursorPos is after CRLF except at end where its at last char
                ? literal.Slice(0, cursorPos - lineLength - 1)
                : literal.Slice(0, cursorPos - lineLength - 2);
        
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
                            body = literal.ToPreviousLine(cursorPos, lineLength);
                            return literal.Slice(cursorPos);
                        }
                    }

                    inStatements--;
                }
                else if (line.StartsWith("else"))
                {
                    if (inStatements == 0)
                    {
                        body = literal.ToPreviousLine(cursorPos, lineLength);
                        return literal.Slice(cursorPos - lineLength - 1);
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
        static ReadOnlyMemory<char> ParseCodeElseBlock(this ReadOnlyMemory<char> literal, ScriptContext context, ReadOnlyMemory<char> blockName, out PageElseBlock statement)
        {
            var inStatements = 0;
            statement = null;
            var statementPos = -1;
            var elseExpr = default(ReadOnlyMemory<char>);
            
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
                            var body = context.ParseCodeStatements(literal.Slice(statementPos, cursorPos - statementPos - lineLength - 1).Trim());
                            statement = new PageElseBlock(elseExpr, new JsBlockStatement(body));
                            return literal.Slice(cursorPos);
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
                            var bodyText = literal.Slice(statementPos, cursorPos - statementPos).Trim();
                            var body = context.ParseCodeStatements(bodyText);
                            statement = new PageElseBlock(elseExpr, new JsBlockStatement(body));
                            return literal.Slice(cursorPos);
                        }

                        statementPos = cursorPos - lineLength + 4;
                        elseExpr = line.Slice(4).Trim();
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
            if (literal.FirstCharEquals(SharpPageUtils.FilterSep))
            {
                filters = new List<JsCallExpression>();
                literal = literal.Advance(1);

                while (true)
                {
                    literal = literal.ParseJsCallExpression(out var filter, filterExpression: true);

                    filters.Add(filter);

                    literal = literal.AdvancePastWhitespace();

                    if (literal.IsNullOrEmpty())
                        return literal;

                    if (!literal.FirstCharEquals(SharpPageUtils.FilterSep))
                        throw new SyntaxErrorException(
                            $"Expected filter separator '|' but was {literal.DebugFirstChar()}");

                    literal = literal.Advance(1);
                }
            }
            else
            {
                if (!literal.IsNullOrEmpty())
                    literal = literal.Advance(1);
            }

            return literal;
        }

        internal static JsStatement[] ParseCodeStatements(this ScriptContext context, ReadOnlyMemory<char> code)
        {
            var ret = new List<JsStatement>();

            bool inComments = false;
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

                // comments
                if (firstChar == '*' || inComments)
                {
                    if (line.EndsWith("*"))
                    {
                        inComments = false;
                        continue;
                    }

                    inComments = true;
                    continue;
                }

                // block statement
                if (firstChar == '#') 
                {
                    var literal = line.Slice(1).ParseVarName(out var blockName);
                    var argument = literal.Trim();
                    var startPos = cursorPos - lineLength - 1;

                    ReadOnlyMemory<char> afterBlock;

                    if (!context.DontEvaluateBlocksNamed.Contains(blockName.ToString()))
                    {
                        var exprStr = code.Slice(cursorPos);
                        afterBlock = exprStr.ParseCodeBody(blockName, out var blockBody);
                        var blockStatements = context.ParseCodeStatements(blockBody);

                        var elseStatements = new List<PageElseBlock>();

                        afterBlock = afterBlock.AdvancePastWhitespace();
                        while (afterBlock.StartsWith("else"))
                        {
                            afterBlock = afterBlock.ParseCodeElseBlock(context, blockName, out var elseStatement);
                            elseStatements.Add(elseStatement);
                            afterBlock = afterBlock.AdvancePastWhitespace();
                        }
                    
                        var originalText = code.Slice(startPos, code.Length - afterBlock.Length - startPos);

                        ret.Add(new PageBlockFragmentStatement(
                            new PageBlockFragment(
                                originalText,
                                blockName.ToString(),
                                argument,
                                new JsBlockStatement(blockStatements),
                                elseStatements
                            )
                        ));
                    }
                    else
                    {
                        var exprStr = code.Slice(cursorPos);
                        afterBlock = exprStr.ParseCodeBody(blockName, out var blockBody);
                        
                        var originalText = code.Slice(startPos, code.Length - afterBlock.Length - startPos);

                        ret.Add(new PageBlockFragmentStatement(
                            new PageBlockFragment(
                                originalText,
                                blockName.ToString(),
                                argument,
                                new List<PageFragment> { new PageStringFragment(blockBody) }
                            )
                        ));
                    }
                    
                    cursorPos = code.Length - afterBlock.Length;
                    continue;
                }

                const int delim = 2; // '}}'.length
                // multi-line expression
                if (startExpressionPos >= 0)
                {
                    // multi-line end
                    if (line.EndsWith("}}"))
                    {
                        var exprStr = code.Slice(startExpressionPos,  cursorPos - startExpressionPos - leftIndent - rightIndent - delim).Trim();
                        var afterExpr = exprStr.Span.ParseExpression(out var expr, out var filters);
                        
                        ret.AddExpression(exprStr, expr, filters);
                        startExpressionPos = -1;
                    }
                    
                    continue;
                }

                if (firstChar == '{' && line.Span.SafeCharEquals(1, '{'))
                {
                    // single-line {{ expr }}
                    if (line.EndsWith("}}"))
                    {
                        var exprStr = code.Slice(cursorPos - lineLength + leftIndent + delim, lineLength - leftIndent - delim - delim).Trim();
                        var afterExpr = exprStr.Span.ParseExpression(out var expr, out var filters);
                        
                        ret.AddExpression(exprStr, expr, filters);
                        continue;
                    }
                    
                    // multi-line start
                    startExpressionPos = cursorPos - lineLength + leftIndent + delim;
                    continue;
                }
                else
                {
                    // treat line as an expression statement
                    var afterExpr = line.Span.ParseExpression(out var expr, out var filters);
                    afterExpr = afterExpr.AdvancePastWhitespace();

                    if (!afterExpr.IsEmpty)
                        throw new SyntaxErrorException($"Unexpected syntax after expression: {afterExpr.ToString()}, near {line.DebugLiteral()}");
                    
                    ret.AddExpression(line, expr, filters);
                }
            }

            return ret.ToArray();
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
    
    
}