using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
#if !NET6_0
using ServiceStack.Extensions;
#endif

namespace ServiceStack.Script 
{
    /// <summary>
    /// #Script Language which processes ```lang ... ``` blocks
    /// </summary>
    public sealed class SharpScript : ScriptLanguage
    {
        private SharpScript() {} // force usage of singleton

        public static readonly ScriptLanguage Language = new SharpScript();

        public override string Name => "script";
        
        public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
        {
            return context.ParseScript(body);
        }
    }
    
    /// <summary>
    /// The #Script Default Template Language (does not process ```lang ... ``` blocks)
    /// </summary>
    public sealed class ScriptTemplate : ScriptLanguage
    {
        private ScriptTemplate() {} // force usage of singleton

        public static readonly ScriptLanguage Language = new ScriptTemplate();
        
        public override string Name => "template";
        
        public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
        {
            var pageFragments = context.ParseTemplate(body);
            return pageFragments;
        }
        
        public override async Task<bool> WritePageFragmentAsync(ScriptScopeContext scope, PageFragment fragment, CancellationToken token)
        {
            if (fragment is PageStringFragment str)
            {
                await scope.OutputStream.WriteAsync(str.ValueUtf8, token).ConfigAwait();
            }
            else if (fragment is PageVariableFragment var)
            {
                if (var.Binding?.Equals(ScriptConstants.Page) == true
                    && !scope.ScopedParams.ContainsKey(ScriptConstants.PartialArg))
                {
                    if (scope.PageResult.PageProcessed)
                        throw new NotSupportedException("{{page}} can only be called once per render, in the Layout page.");
                    scope.PageResult.PageProcessed = true;
                    
                    await scope.PageResult.WritePageAsync(scope.PageResult.Page, scope.PageResult.CodePage, scope, token).ConfigAwait();

                    if (scope.PageResult.HaltExecution)
                        scope.PageResult.HaltExecution = false; //break out of page but continue evaluating layout
                }
                else
                {
                    await scope.PageResult.WriteVarAsync(scope, var, token).ConfigAwait();
                }
            }
            else if (fragment is PageBlockFragment blockFragment)
            {
                var block = scope.PageResult.GetBlock(blockFragment.Name);
                await block.WriteAsync(scope, blockFragment, token).ConfigAwait();
            }
            else return false;

            return true; 
        }
    }

    public static class ScriptTemplateUtils
    {
        /// <summary>
        /// Create SharpPage configured to use #Script 
        /// </summary>
        public static SharpPage SharpScriptPage(this ScriptContext context, string code) 
            => context.Pages.OneTimePage(code, context.PageFormats[0].Extension,
                p => p.ScriptLanguage = SharpScript.Language);

        /// <summary>
        /// Create SharpPage configured to use #Script Templates 
        /// </summary>
        public static SharpPage TemplateSharpPage(this ScriptContext context, string code) 
            => context.Pages.OneTimePage(code, context.PageFormats[0].Extension,
                p => p.ScriptLanguage = ScriptTemplate.Language);

        /// <summary>
        /// Render #Script output to string
        /// </summary>
        public static string RenderScript(this ScriptContext context, string script, out ScriptException error) => 
            context.RenderScript(script, null, out error);
        /// <summary>
        /// Alias for EvaluateScript 
        /// </summary>
        public static string EvaluateScript(this ScriptContext context, string script, out ScriptException error) => 
            context.EvaluateScript(script, null, out error);

        /// <summary>
        /// Render #Script output to string
        /// </summary>
        public static string RenderScript(this ScriptContext context, string script, Dictionary<string, object> args, out ScriptException error) =>
            context.EvaluateScript(script, args, out error);
        /// <summary>
        /// Alias for RenderScript 
        /// </summary>
        public static string EvaluateScript(this ScriptContext context, string script, Dictionary<string, object> args, out ScriptException error)
        {
            var pageResult = new PageResult(context.SharpScriptPage(script));
            args.Each((x,y) => pageResult.Args[x] = y);
            try { 
                var output = pageResult.Result;
                error = pageResult.LastFilterError != null ? new ScriptException(pageResult) : null;
                return output;
            }
            catch (Exception e)
            {
                pageResult.LastFilterError = e;
                error = new ScriptException(pageResult);
                return null;
            }
        }

        /// <summary>
        /// Render #Script output to string
        /// </summary>
        public static string RenderScript(this ScriptContext context, string script, Dictionary<string, object> args = null) =>
            context.EvaluateScript(script, args);
        /// <summary>
        /// Alias for RenderScript 
        /// </summary>
        public static string EvaluateScript(this ScriptContext context, string script, Dictionary<string, object> args=null)
        {
            var pageResult = new PageResult(context.SharpScriptPage(script));
            args.Each((x,y) => pageResult.Args[x] = y);
            return pageResult.RenderScript();
        }

        /// <summary>
        /// Render #Script output to string asynchronously
        /// </summary>
        public static Task<string> RenderScriptAsync(this ScriptContext context, string script, Dictionary<string, object> args = null) =>
            context.EvaluateScriptAsync(script, args);
        /// <summary>
        /// Alias for RenderScriptAsync 
        /// </summary>
        public static async Task<string> EvaluateScriptAsync(this ScriptContext context, string script, Dictionary<string, object> args=null)
        {
            var pageResult = new PageResult(context.SharpScriptPage(script));
            args.Each((x,y) => pageResult.Args[x] = y);
            return await pageResult.RenderScriptAsync().ConfigAwait();
        }
        
        /// <summary>
        /// Evaluate #Script and convert returned value to T 
        /// </summary>
        public static T Evaluate<T>(this ScriptContext context, string script, Dictionary<string, object> args = null) =>
            context.Evaluate(script, args).ConvertTo<T>();
        
        /// <summary>
        /// Evaluate #Script and return value 
        /// </summary>
        public static object Evaluate(this ScriptContext context, string script, Dictionary<string, object> args=null)
        {
            var pageResult = new PageResult(context.SharpScriptPage(script));
            args.Each((x,y) => pageResult.Args[x] = y);

            if (!pageResult.EvaluateResult(out var returnValue))
                throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
            
            return ScriptLanguage.UnwrapValue(returnValue);
        }

        /// <summary>
        /// Evaluate #Script and convert returned value to T asynchronously
        /// </summary>
        public static async Task<T> EvaluateAsync<T>(this ScriptContext context, string script, Dictionary<string, object> args = null) =>
            (await context.EvaluateAsync(script, args).ConfigAwait()).ConvertTo<T>();
        
        /// <summary>
        /// Evaluate #Script and convert returned value to T asynchronously
        /// </summary>
        public static async Task<object> EvaluateAsync(this ScriptContext context, string script, Dictionary<string, object> args=null)
        {
            var pageResult = new PageResult(context.SharpScriptPage(script));
            args.Each((x,y) => pageResult.Args[x] = y);

            var ret = await pageResult.EvaluateResultAsync().ConfigAwait();
            if (!ret.Item1)
                throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
            
            return ScriptLanguage.UnwrapValue(ret.Item2);
        }
        
        
        public static List<PageFragment> ParseTemplate(string text)
        {
            return new ScriptContext().Init().ParseTemplate(text.AsMemory());
        }

        internal const char FilterSep = '|';
        internal const char StatementsSep = ';';

        // {{#name}}  {{else if a=b}}  {{else}}  {{/name}}
        //          ^
        // returns    ^                         ^
        public static ReadOnlyMemory<char> ParseTemplateBody(this ReadOnlyMemory<char> literal, ReadOnlyMemory<char> blockName, out ReadOnlyMemory<char> body)
        {
            var inStatements = 0;
            var pos = 0;
            
            while (true)
            {
                pos = literal.IndexOf("{{", pos);
                if (pos == -1)
                    throw new SyntaxErrorException($"End block for '{blockName}' not found.");

                var c = literal.SafeGetChar(pos + 2);

                if (c == '#')
                {
                    inStatements++;
                    pos = literal.IndexOf("}}", pos) + 2; //end of expression
                    continue;
                }

                if (c == '/')
                {
                    if (inStatements == 0)
                    {
                        literal.Slice(pos + 2 + 1).ParseVarName(out var name);
                        if (name.EqualsOrdinal(blockName))
                        {
                            body = literal.Slice(0, pos).TrimFirstNewLine();
                            return literal.Slice(pos);
                        }
                    }

                    inStatements--;
                }
                else if (literal.Slice(pos + 2).StartsWith("else"))
                {
                    if (inStatements == 0)
                    {
                        body = literal.Slice(0, pos).TrimFirstNewLine();
                        return literal.Slice(pos);
                    }
                }

                pos += 2;
            }
        }

        //   {{else if a=b}}  {{else}}  {{/name}}
        //  ^
        // returns           ^         ^
        public static ReadOnlyMemory<char> ParseTemplateElseBlock(this ReadOnlyMemory<char> literal, string blockName, 
            out ReadOnlyMemory<char> elseArgument, out ReadOnlyMemory<char> elseBody)
        {
            var inStatements = 0;
            var pos = 0;
            var statementPos = -1;
            elseBody = default;
            elseArgument = default;
            
            while (true)
            {
                pos = literal.IndexOf("{{", pos);
                if (pos == -1)
                    throw new SyntaxErrorException($"End block for 'else' not found.");

                var c = literal.SafeGetChar(pos + 2);
                if (c == '#')
                {
                    inStatements++;
                    pos = literal.IndexOf("}}", pos) + 2; //end of expression                    
                }
                else if (c == '/')
                {
                    if (inStatements == 0)
                    {
                        literal.Slice(pos + 2 + 1).ParseVarName(out var name);
                        if (name.EqualsOrdinal(blockName))
                        {
                            elseBody = literal.Slice(statementPos, pos - statementPos).TrimFirstNewLine();
                            return literal.Slice(pos);
                        }
                    }

                    inStatements--;
                }
                else if (literal.Slice(pos + 2).StartsWith("else"))
                {
                    if (inStatements == 0)
                    {
                        if (statementPos >= 0)
                        {
                            elseBody = literal.Slice(statementPos, pos - statementPos).TrimFirstNewLine();
                            return literal.Slice(pos);
                        }
                        
                        var endExprPos = literal.IndexOf("}}", pos);
                        if (endExprPos == -1)
                            throw new SyntaxErrorException($"End expression for 'else' not found.");

                        var exprStartPos = pos + 2 + 4; //= {{else...

                        elseArgument = literal.Slice(exprStartPos, endExprPos - exprStartPos).Trim();
                        statementPos = endExprPos + 2;
                    }
                }

                pos += 2;
            }
        }
        
        public static List<PageFragment> ParseScript(this ScriptContext context, ReadOnlyMemory<char> text)
        {
            var to = new List<PageFragment>();
            ScriptLanguage scriptLanguage = null;
            ReadOnlyMemory<char> modifiers = default;
            ReadOnlyMemory<char> prevBlock = default;
            int startBlockPos = -1;
            var cursorPos = 0;
            var lastBlockPos = 0;
            var inRawBlock = false;
            
            const int delim = 3; // '```'.length

            while (text.TryReadLine(out var line, ref cursorPos))
            {
                var lineLength = line.Length;
                line = line.AdvancePastWhitespace();

                if (line.IndexOf("{{#raw") >= 0 && line.IndexOf("{{/raw}}") < 0)
                {
                    inRawBlock = true;
                    continue;
                }
                if (line.IndexOf("{{/raw}}") >= 0)
                {
                    inRawBlock = false;
                    continue;
                }
                if (inRawBlock)
                    continue;

                if (line.StartsWith("```"))
                {
                    if (scriptLanguage != null && startBlockPos >= 0 && line.Slice(delim).AdvancePastWhitespace().IsEmpty) //is end block
                    {
                        var templateFragments = ScriptTemplate.Language.Parse(context, prevBlock);
                        to.AddRange(templateFragments);

                        var blockBody = text.ToLineStart(cursorPos, lineLength, startBlockPos);
                        var blockFragments = scriptLanguage.Parse(context, blockBody, modifiers);
                        to.AddRange(blockFragments);

                        prevBlock = default;
                        startBlockPos = -1;
                        scriptLanguage = null;
                        modifiers = null;
                        lastBlockPos = cursorPos;
                        continue;
                    }

                    if (line.SafeGetChar(delim).IsValidVarNameChar())
                    {
                        line = line.Slice(delim).ParseVarName(out var blockNameSpan);

                        var blockName = blockNameSpan.ToString();
                        scriptLanguage = context.GetScriptLanguage(blockName);
                        if (scriptLanguage == null)
                            continue;

                        modifiers = line.AdvancePastChar('|');
                        var delimLen = text.Span.SafeCharEquals(cursorPos - 2, '\r') ? 2 : 1;
                        prevBlock = text.Slice(lastBlockPos, cursorPos - lastBlockPos - lineLength - delimLen);
                        startBlockPos = cursorPos;
                    }
                }
            }

            var remainingBlock = text.Slice(lastBlockPos);
            if (!remainingBlock.IsEmpty)
            {
                var templateFragments = ScriptTemplate.Language.Parse(context, remainingBlock);
                to.AddRange(templateFragments);
            }
            
            return to;
        }

        public static List<PageFragment> ParseTemplate(this ScriptContext context, ReadOnlyMemory<char> text)
        {
            var to = new List<PageFragment>();

            if (text.IsNullOrWhiteSpace())
                return to;
            
            int pos;
            var lastPos = 0;

            int nextPos()
            {
                var c1 = text.IndexOf("{{", lastPos);
                var c2 = text.IndexOf("{|", lastPos);

                if (c2 == -1)
                    return c1;
                
                return c1 == -1 ? c2 : c1 < c2 ? c1 : c2;
            }
            
            while ((pos = nextPos()) != -1)
            {
                var block = text.Slice(lastPos, pos - lastPos);
                if (!block.IsNullOrEmpty())
                    to.Add(new PageStringFragment(block));
                
                var varStartPos = pos + 2;
                
                if (varStartPos >= text.Span.Length)
                    throw new SyntaxErrorException($"Unterminated '{{{{' expression, near '{text.Slice(lastPos).DebugLiteral()}'");

                if (text.Span.SafeCharEquals(varStartPos - 1, '|')) // lang expression syntax {|lang ... |} https://flow.org/en/docs/types/objects/#toc-exact-object-types
                {
                    var literal = text.Slice(varStartPos);

                    ScriptLanguage lang = null;
                    if (literal.SafeGetChar(0).IsValidVarNameChar())
                    {
                        literal = literal.ParseVarName(out var langSpan);
                    
                        lang = context.GetScriptLanguage(langSpan.ToString());
                        if (lang != null)
                        {
                            var endPos = literal.IndexOf("|}");
                            if (endPos == -1)
                                throw new SyntaxErrorException($"Unterminated '|}}' expression, near '{text.Slice(varStartPos).DebugLiteral()}'");

                            var exprStr = literal.Slice(0, endPos);
                            var langExprFragment = lang.Parse(context, exprStr);
                            to.AddRange(langExprFragment);
                        }
                    }
                    if (lang == null)
                    {
                        var nextLastPos = text.IndexOf("|}", varStartPos) + 2;
                        block = text.Slice(pos, nextLastPos - pos);
                        if (!block.IsNullOrEmpty())
                            to.Add(new PageStringFragment(block));
                    }

                    lastPos = text.IndexOf("|}", varStartPos) + 2;
                    continue;
                }

                var firstChar = text.Span[varStartPos];
                if (firstChar == '*') //comment
                {
                    lastPos = text.IndexOf("*}}", varStartPos) + 3;
                    if (text.Span.SafeCharEquals(lastPos,'\r')) lastPos++;
                    if (text.Span.SafeCharEquals(lastPos,'\n')) lastPos++;
                }
                else if (firstChar == '#') //block statement
                {
                    var literal = text.Slice(varStartPos + 1);

                    literal = literal.ParseTemplateScriptBlock(context, out var blockFragment);

                    var length = text.Length - pos - literal.Length;
                    blockFragment.OriginalText = text.Slice(pos, length);
                    lastPos = pos + length;
                    
                    to.Add(blockFragment);
                }
                else
                {
                    var literal = text.Slice(varStartPos).Span;
                    literal = literal.ParseJsExpression(out var expr, filterExpression: true);

                    var filters = new List<JsCallExpression>();

                    if (!literal.StartsWith("}}"))
                    {
                        literal = literal.AdvancePastWhitespace();
                        if (literal.FirstCharEquals(FilterSep))
                        {
                            literal = literal.AdvancePastPipeOperator();

                            while (true)
                            {
                                literal = literal.ParseJsCallExpression(out var filter, filterExpression: true);

                                filters.Add(filter);

                                literal = literal.AdvancePastWhitespace();

                                if (literal.IsNullOrEmpty())
                                    throw new SyntaxErrorException("Unterminated filter expression");

                                if (literal.StartsWith("}}"))
                                {
                                    literal = literal.Advance(2);
                                    break;
                                }

                                if (!literal.FirstCharEquals(FilterSep))
                                    throw new SyntaxErrorException(
                                        $"Expected pipeline operator '|>' but was {literal.DebugFirstChar()}");

                                literal = literal.AdvancePastPipeOperator();
                            }
                        }
                        else if (!literal.AdvancePastWhitespace().IsNullOrEmpty())
                        {
                            throw new SyntaxErrorException($"Unexpected syntax '{literal.ToString()}', Expected pipeline operator '|>'");
                        }
                    }
                    else
                    {
                        literal = literal.Advance(2);
                    }

                    var length = text.Length - pos - literal.Length;
                    var originalText = text.Slice(pos, length);
                    lastPos = pos + length;

                    var varFragment = new PageVariableFragment(originalText, expr, filters);
                    to.Add(varFragment);

                    var newLineLen = literal.StartsWith("\n")
                        ? 1
                        : literal.StartsWith("\r\n")
                            ? 2
                            : 0;

                    if (newLineLen > 0)
                    {
                        var lastExpr = varFragment.FilterExpressions?.LastOrDefault();
                        var filterName = lastExpr?.Name ??
                                         varFragment?.InitialExpression?.Name ?? varFragment.Binding;
                        if ((filterName != null && context.RemoveNewLineAfterFiltersNamed.Contains(filterName))
                            || expr is JsVariableDeclaration)
                        {
                            lastPos += newLineLen;
                        }
                    }
                }
            }

            if (lastPos != text.Length)
            {
                var lastBlock = lastPos == 0 ? text : text.Slice(lastPos);
                to.Add(new PageStringFragment(lastBlock));
            }

            return to;
        }

        // {{#if ...}}
        //    ^
        public static ReadOnlyMemory<char> ParseTemplateScriptBlock(this ReadOnlyMemory<char> literal, ScriptContext context, out PageBlockFragment blockFragment)
        {
            literal = literal.ParseVarName(out var blockNameSpan);

            var blockName = blockNameSpan.ToString();
            var endBlock = "{{/" + blockName + "}}";
            var endExprPos = literal.IndexOf("}}");
            if (endExprPos == -1)
                throw new SyntaxErrorException($"Unterminated '{blockName}' block expression, near '{literal.DebugLiteral()}'" );

            var argument = literal.Slice(0, endExprPos).Trim();
            literal = literal.Advance(endExprPos + 2);

            var language = context.ParseAsLanguage.TryGetValue(blockName, out var lang)
                ? lang
                : ScriptTemplate.Language;
            
            if (language.Name == ScriptVerbatim.Language.Name)
            {
                var endBlockPos = literal.IndexOf(endBlock);
                if (endBlockPos == -1)
                    throw new SyntaxErrorException($"Unterminated end block '{endBlock}'");

                var body = literal.Slice(0, endBlockPos);
                literal = literal.Advance(endBlockPos + endBlock.Length).TrimFirstNewLine();

                blockFragment = language.ParseVerbatimBlock(blockName, argument, body);
                return literal;
            }

            literal = literal.ParseTemplateBody(blockNameSpan, out var bodyText);
            var bodyFragments = language.Parse(context, bodyText);
                
            var elseBlocks = new List<PageElseBlock>();
            while (literal.StartsWith("{{else"))
            {
                literal = literal.ParseTemplateElseBlock(blockName, out var elseArgument,  out var elseBody);

                var elseBlock = new PageElseBlock(elseArgument, language.Parse(context, elseBody));
                elseBlocks.Add(elseBlock);
            }

            literal = literal.Advance(2 + 1 + blockName.Length + 2);

            //remove new line after partial block end tag
            literal = literal.TrimFirstNewLine();

            blockFragment = new PageBlockFragment(blockName, argument, bodyFragments, elseBlocks);

            return literal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRawString ToRawString(this string value) => value != null
            ? new RawString(value)
            : RawString.Empty;
        
        public static ConcurrentDictionary<string, Func<ScriptScopeContext, object, object>> BinderCache { get; } = new();

        public static Func<ScriptScopeContext, object, object> GetMemberExpression(Type targetType, ReadOnlyMemory<char> expression)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (expression.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(expression));

            var key = targetType.FullName + ':' + expression;

            if (BinderCache.TryGetValue(key, out var fn))
                return fn;

            BinderCache[key] = fn = Compile(targetType, expression);

            return fn;
        }
        
        public static Func<ScriptScopeContext, object, object> Compile(Type type, ReadOnlyMemory<char> expr)
        {
            var scope = Expression.Parameter(typeof(ScriptScopeContext), "scope");
            var param = Expression.Parameter(typeof(object), "instance");
            var body = CreateBindingExpression(type, expr, scope, param);

            body = Expression.Convert(body, typeof(object));
            return Expression.Lambda<Func<ScriptScopeContext, object, object>>(body, scope, param).Compile();
        }

        private static Expression CreateBindingExpression(Type type, ReadOnlyMemory<char> expr, ParameterExpression scope, ParameterExpression instance)
        {
            Expression body = Expression.Convert(instance, type);

            var currType = type;

            var pos = 0;
            var depth = 0;
            var delim = ".".AsMemory();
            while (expr.TryReadPart(delim, out ReadOnlyMemory<char> member, ref pos))
            {
                try
                {
                    if (member.IndexOf('(') >= 0)
                        throw new BindingExpressionException(
                            $"Calling methods in '{expr}' is not allowed in binding expressions, use a filter instead.",
                            member.ToString(), expr.ToString());

                    var indexerPos = member.IndexOf('[');
                    if (indexerPos >= 0)
                    {
                        var prop = member.LeftPart('[');
                        var indexer = member.RightPart('[');
                        indexer.Span.ParseJsExpression(out var token);

                        if (token is JsCallExpression)
                            throw new BindingExpressionException($"Only constant binding expressions are supported: '{expr}'",
                                member.ToString(), expr.ToString());

                        var value = JsToken.UnwrapValue(token);

                        var valueExpr = value == null
                            ? (Expression) Expression.Call(
                                typeof(ScriptTemplateUtils).GetStaticMethod(nameof(EvaluateBinding)),
                                scope,
                                Expression.Constant(token))
                            : Expression.Constant(value);

                        if (currType == typeof(string))
                        {
                            body = CreateStringIndexExpression(body, token, scope, valueExpr, ref currType);
                        }
                        else if (currType.IsArray)
                        {
                            if (token != null)
                            {
                                var evalAsInt = typeof(ScriptTemplateUtils).GetStaticMethod(nameof(EvaluateBindingAs))
                                    .MakeGenericMethod(typeof(int));
                                body = Expression.ArrayIndex(body,
                                    Expression.Call(evalAsInt, scope, Expression.Constant(token)));
                            }
                            else
                            {
                                body = Expression.ArrayIndex(body, valueExpr);
                            }
                        }
                        else if (depth == 0)
                        {
                            var pi = AssertProperty(currType, "Item", expr);
                            currType = pi.PropertyType;

                            if (token != null)
                            {
                                var indexType = pi.GetGetMethod()?.GetParameters().FirstOrDefault()?.ParameterType;
                                if (indexType != typeof(object) && !(valueExpr is ConstantExpression ce && ce.Type == indexType))
                                {
                                    var evalAsIndexType = typeof(ScriptTemplateUtils).GetStaticMethod(nameof(EvaluateBindingAs))
                                        .MakeGenericMethod(indexType);
                                    valueExpr = Expression.Call(evalAsIndexType, scope, Expression.Constant(token));
                                }
                            }

                            body = Expression.Property(body, "Item", valueExpr);
                        }
                        else
                        {
                            var pi = AssertProperty(currType, prop.ToString(), expr);
                            currType = pi.PropertyType;
                            body = Expression.PropertyOrField(body, prop.ToString());

                            if (currType == typeof(string))
                            {
                                body = CreateStringIndexExpression(body, token, scope, valueExpr, ref currType);
                            }
                            else
                            {
                                var indexMethod = currType.GetMethod("get_Item", new[] {value.GetType()});
                                body = Expression.Call(body, indexMethod, valueExpr);
                                currType = indexMethod.ReturnType;
                            }
                        }
                    }
                    else
                    {
                        if (depth >= 1)
                        {
                            var memberName = member.ToString();
                            if (typeof(IDictionary).IsAssignableFrom(currType))
                            {
                                var pi = AssertProperty(currType, "Item", expr);
                                currType = pi.PropertyType;
                                body = Expression.Property(body, "Item", Expression.Constant(memberName));
                            }
                            else
                            {
                                body = Expression.PropertyOrField(body, memberName);
                                var pi = currType.GetProperty(memberName);
                                if (pi != null)
                                {
                                    currType = pi.PropertyType;
                                }
                                else
                                {
                                    var fi = currType.GetField(memberName);
                                    if (fi != null)
                                        currType = fi.FieldType;
                                }
                                
                            }
                        }
                    }

                    depth++;
                }
                catch (BindingExpressionException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new BindingExpressionException($"Could not compile '{member}' from expression '{expr}'", 
                        member.ToString(), expr.ToString(), e);
                }
            }
            return body;
        }

        private static readonly Type[] ObjectArg = { typeof(object) };
        public static MethodInfo CreateConvertMethod(Type toType) =>
            typeof(AutoMappingUtils).GetStaticMethod(nameof(AutoMappingUtils.ConvertTo), ObjectArg).MakeGenericMethod(toType);

        public static Action<ScriptScopeContext, object, object> CompileAssign(Type type, ReadOnlyMemory<char> expr)
        {
            var scope = Expression.Parameter(typeof(ScriptScopeContext), "scope");
            var instance = Expression.Parameter(typeof(object), "instance");
            var valueToAssign = Expression.Parameter(typeof(object), "valueToAssign");

            var body = CreateBindingExpression(type, expr, scope, instance);
            if (body is IndexExpression propItemExpr)
            {
                var mi = propItemExpr.Indexer.GetSetMethod();
                var indexExpr = propItemExpr.Arguments[0];
                if (propItemExpr.Indexer.PropertyType != typeof(object))
                {
                    body = Expression.Call(propItemExpr.Object, mi, indexExpr, 
                        Expression.Call(CreateConvertMethod(propItemExpr.Indexer.DeclaringType.GetCollectionType()), valueToAssign));
                }
                else
                {
                    body = Expression.Call(propItemExpr.Object, mi, indexExpr, valueToAssign);
                }
            }
            else if (body is BinaryExpression binaryExpr && binaryExpr.NodeType == ExpressionType.ArrayIndex)
            {
                var arrayInstance = binaryExpr.Left;
                var indexExpr = binaryExpr.Right;

                if (arrayInstance.Type != typeof(object))
                {
                    body = Expression.Assign(
                        Expression.ArrayAccess(arrayInstance, indexExpr), 
                        Expression.Call(CreateConvertMethod(arrayInstance.Type.GetElementType()), valueToAssign));
                }
                else
                {
                    body = Expression.Assign(
                        Expression.ArrayAccess(arrayInstance, indexExpr), 
                        valueToAssign);
                }
            }
            else if (body is MemberExpression propExpr)
            {
                if (propExpr.Type != typeof(object))
                {
                    body = Expression.Assign(propExpr, Expression.Call(CreateConvertMethod(propExpr.Type), valueToAssign));
                }
                else
                {
                    body = Expression.Assign(propExpr, valueToAssign);
                }
            }
            else 
                throw new BindingExpressionException($"Assignment expression for '{expr}' not supported yet", "valueToAssign", expr.ToString());

            return Expression.Lambda<Action<ScriptScopeContext, object, object>>(body, scope, instance, valueToAssign).Compile();
        }

        private static Expression CreateStringIndexExpression(Expression body, JsToken binding, ParameterExpression scope,
            Expression valueExpr, ref Type currType)
        {
            body = Expression.Call(body, typeof(string).GetMethod(nameof(string.ToCharArray), Type.EmptyTypes));
            currType = typeof(char[]);

            if (binding != null)
            {
                var evalAsInt = typeof(ScriptTemplateUtils).GetStaticMethod(nameof(EvaluateBindingAs))
                    .MakeGenericMethod(typeof(int));
                body = Expression.ArrayIndex(body, Expression.Call(evalAsInt, scope, Expression.Constant(binding)));
            }
            else
            {
                body = Expression.ArrayIndex(body, valueExpr);
            }
            return body;
        }

        public static object EvaluateBinding(ScriptScopeContext scope, JsToken token)
        {
            var result = token.Evaluate(scope);
            return result;
        }

        public static T EvaluateBindingAs<T>(ScriptScopeContext scope, JsToken token)
        {
            var result = EvaluateBinding(scope, token);
            var converted = result.ConvertTo<T>();
            return converted;
        }
        
        private static PropertyInfo AssertProperty(Type currType, string prop, ReadOnlyMemory<char> expr)
        {
            var pi = currType.GetProperty(prop);
            if (pi == null)
                throw new ArgumentException(
                    $"Property '{prop}' does not exist on Type '{currType.Name}' from binding expression '{expr}'");
            return pi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(this char c) =>
            c == ' ' || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085';        
    }
}