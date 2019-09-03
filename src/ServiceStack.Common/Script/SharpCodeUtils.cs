using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public static class SharpCodeUtils
    {
        // cursorPos is after CRLF except at end where its at last char
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyMemory<char> ToPreviousLine(this ReadOnlyMemory<char> literal, int cursorPos, int lineLength)
        {
            var CLRF = literal.Span.SafeCharEquals(cursorPos - 2, '\r');
            var ret = literal.Slice(0, cursorPos - lineLength -
               (cursorPos == literal.Length ? 0 : (CLRF ? 2 : 1)) -
               (CLRF ? 2 : 1));
            
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
            var CLRF = literal.Span.SafeCharEquals(cursorPos - 2, '\r');
            var ret = literal.Slice(statementPos, cursorPos - statementPos - lineLength -
               (cursorPos == literal.Length ? 0 : (CLRF ? 2 : 1)) -
               (CLRF ? 2 : 1));
            
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
                            body = literal.ToPreviousLine(cursorPos, lineLength);
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
                        body = literal.ToPreviousLine(cursorPos, lineLength);
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
                            elseBody = literal.Slice(statementPos, cursorPos - statementPos).Trim();
                            var ret = literal.Slice(cursorPos);
                            return ret;
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
                if (firstChar == '*')
                {
                    if (line.EndsWith("*"))
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
                        var exprStr = code.Slice(cursorPos - lineLength + leftIndent + delim, lineLength - leftIndent - delim - delim).Trim();
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

                    if (!afterExpr.IsEmpty)
                        throw new SyntaxErrorException($"Unexpected syntax after expression: {afterExpr.ToString()}, near {line.DebugLiteral()}");
                    
                    to.AddExpression(line, expr, filters);
                }
            }

            return to.ToArray();
        }
        
        // #if ...
        //  ^
        public static ReadOnlyMemory<char> ParseCodeScriptBlock(this ReadOnlyMemory<char> literal, ScriptContext context, 
            out PageBlockFragment blockFragment)
        {
            literal = literal.ParseVarName(out var blockName);
            var endArgumentPos = literal.IndexOf('\n');
            var argument = literal.Slice(0, endArgumentPos).Trim();

            literal = literal.Slice(endArgumentPos + 1);

            var blockNameString = blockName.ToString();
            if (context.ParseAsTemplateBlock.Contains(blockNameString))
                throw new NotSupportedException("Must use '{{" + blockNameString + "}} ... {{/" + blockNameString + "}}'"
                                                + $" Template Syntax to use Template-only `#{blockNameString}` in Code Statements");
            
            if (context.ParseAsVerbatimBlock.Contains(blockNameString))
            {
                literal = literal.ParseCodeBody(blockName, out var blockBody);

                blockFragment = new PageBlockFragment(
                    blockName.ToString(),
                    argument,
                    new List<PageFragment> {new PageStringFragment(blockBody)}
                );
            }
            else
            {
                literal = literal.ParseCodeBody(blockName, out var blockBody);

                var elseBlocks = new List<PageElseBlock>();

                literal = literal.AdvancePastWhitespace();
                while (literal.StartsWith("else"))
                {
                    literal = literal.ParseCodeElseBlock(blockName, out var elseArgument,  out var elseBody);
                    
                    var elseBlock = new PageElseBlock(elseArgument, new JsBlockStatement(context.ParseCodeStatements(elseBody)));
                    elseBlocks.Add(elseBlock);

                    literal = literal.AdvancePastWhitespace();
                }


                var blockStatements = context.ParseCodeStatements(blockBody);
                blockFragment = new PageBlockFragment(
                    blockName.ToString(),
                    argument,
                    new JsBlockStatement(blockStatements),
                    elseBlocks
                );
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
    
    
}