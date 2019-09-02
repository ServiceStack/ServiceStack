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
                            elseBody = literal.Slice(statementPos, cursorPos - statementPos - lineLength - 1).Trim();
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
                            elseBody = literal.Slice(statementPos, cursorPos - statementPos).Trim();
                            return literal.Slice(cursorPos);
                        }

                        statementPos = cursorPos - lineLength + 4;
                        elseArgument = line.Slice(4).Trim();
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
            var to = new List<JsStatement>();

            bool inComments = false;
            int startExpressionPos = -1;

            // parsing assumes only \n
            if (code.IndexOf('\r') >= 0)
            {
                code = code.ToString().Replace("\r", "").AsMemory();
            }
            
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

                    if (context.ParseAsVerbatimBlock.Contains(blockName.ToString()))
                    {
                        var exprStr = code.Slice(cursorPos);
                        afterBlock = exprStr.ParseCodeBody(blockName, out var blockBody);

                        var originalText = code.Slice(startPos, code.Length - afterBlock.Length - startPos);

                        to.Add(new JsPageBlockFragmentStatement(
                            new PageBlockFragment(
                                originalText,
                                blockName.ToString(),
                                argument,
                                new List<PageFragment> {new PageStringFragment(blockBody)}
                            )
                        ));
                    }
                    else
                    {
                        var exprStr = code.Slice(cursorPos);
                        afterBlock = exprStr.ParseCodeBody(blockName, out var blockBody);

                        var elseBlocks = new List<PageElseBlock>();

                        afterBlock = afterBlock.AdvancePastWhitespace();
                        while (afterBlock.StartsWith("else"))
                        {
                            afterBlock = afterBlock.ParseCodeElseBlock(blockName, out var elseArgument,  out var elseBody);
                            
                            var elseBlock = new PageElseBlock(elseArgument, new JsBlockStatement(context.ParseCodeStatements(elseBody)));
                            elseBlocks.Add(elseBlock);

                            afterBlock = afterBlock.AdvancePastWhitespace();
                        }

                        var originalText = code.Slice(startPos, code.Length - afterBlock.Length - startPos);

                        var blockStatements = context.ParseCodeStatements(blockBody);
                        to.Add(new JsPageBlockFragmentStatement(
                            new PageBlockFragment(
                                originalText,
                                blockName.ToString(),
                                argument,
                                new JsBlockStatement(blockStatements),
                                elseBlocks
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
                        
                        to.AddExpression(exprStr, expr, filters);
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
                        
                        to.AddExpression(exprStr, expr, filters);
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
                    
                    to.AddExpression(line, expr, filters);
                }
            }

            return to.ToArray();
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