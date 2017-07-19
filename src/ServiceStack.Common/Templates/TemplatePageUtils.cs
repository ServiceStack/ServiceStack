using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class RawString : IRawString
    {
        private string value;
        public RawString(string value) => this.value = value;
        public string ToRawString() => value;
    }

    public static class TemplatePageUtils
    {
        public static List<PageFragment> ParseTemplatePage(string text)
        {
            return ParseTemplatePage(new StringSegment(text));
        }

        public static List<PageFragment> ParseTemplatePage(StringSegment text)
        {
            var to = new List<PageFragment>();

            if (text.IsNullOrWhiteSpace())
                return to;
            
            int pos;
            var lastPos = 0;
            while ((pos = text.IndexOf("{{", lastPos)) != -1)
            {
                var block = text.Subsegment(lastPos, pos - lastPos);
                if (!block.IsNullOrEmpty())
                    to.Add(new PageStringFragment(block));
                
                var varStartPos = pos + 2;
                var varEndPos = text.IndexOfNextCharNotInQuotes(varStartPos, '|', '}');
                var initialExpr = text.Subsegment(varStartPos, varEndPos - varStartPos).Trim();
                if (varEndPos == -1 || varEndPos >= text.Length)
                    throw new ArgumentException($"Invalid Server HTML Template at '{text.SubstringWithElipsis(0, 50)}'", nameof(text));

                List<JsExpression> filterCommands = null;
                
                var isFilter = text.GetChar(varEndPos) == '|';
                if (isFilter)
                {
                    bool foundVarEnd = false;
                
                    filterCommands = text.Subsegment(varEndPos + 1).ParseExpression<JsExpression>(
                        separator: '|',
                        atEndIndex: (str, strPos) =>
                        {
                            while (str.Length > strPos && str.GetChar(strPos).IsWhiteSpace())
                                strPos++;

                            if (str.Length > strPos + 1 && str.GetChar(strPos) == '}' && str.GetChar(strPos + 1) == '}')
                            {
                                foundVarEnd = true;
                                varEndPos = varEndPos + 1 + strPos + 1;
                                return strPos;
                            }
                            return null;
                        },
                        allowWhitespaceSensitiveSyntax: true);
                
                    if (!foundVarEnd)
                        throw new ArgumentException($"Invalid syntax near '{text.Subsegment(pos).SubstringWithElipsis(0, 50)}'");
                }
                else
                {
                    varEndPos += 1;
                }

                lastPos = varEndPos + 1;
                var originalText = text.Subsegment(pos, lastPos - pos);

                to.Add(new PageVariableFragment(originalText, initialExpr, filterCommands));
            }

            if (lastPos != text.Length)
            {
                var lastBlock = lastPos == 0 ? text : text.Subsegment(lastPos);
                to.Add(new PageStringFragment(lastBlock));
            }

            return to;
        }

        internal static int IndexOfNextCharNotInQuotes(this StringSegment text, int varStartPos, char c1, char c2)
        {
            var inDoubleQuotes = false;
            var inSingleQuotes = false;
            var inBackTickQuotes = false;

            for (var i = varStartPos; i < text.Length; i++)
            {
                var c = text.GetChar(i);
                if (c.IsWhiteSpace())
                    continue;
                
                if (inDoubleQuotes)
                {
                    if (c == '"')
                        inDoubleQuotes = false;
                    continue;
                }
                if (inSingleQuotes)
                {
                    if (c == '\'')
                        inSingleQuotes = false;
                    continue;
                }
                if (inBackTickQuotes)
                {
                    if (c == '`')
                        inBackTickQuotes = false;
                    continue;
                }
                
                switch (c)
                {
                    case '"':
                        inDoubleQuotes = true;
                        continue;
                    case '\'':
                        inSingleQuotes = true;
                        continue;
                    case '`':
                        inBackTickQuotes = true;
                        continue;
                }

                if (c == c1 || c == c2)
                    return i;
            }

            return text.Length;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRawString ToRawString(this string value) => 
            new RawString(value ?? "");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRawString ToRawString(this StringSegment value) => 
            new RawString(value.HasValue ? value.Value : "");

        public static Func<TemplateScopeContext, object, object> Compile(Type type, StringSegment expr)
        {
            var scope = Expression.Parameter(typeof(TemplateScopeContext), "scope");
            var param = Expression.Parameter(typeof(object), "instance");
            Expression body = Expression.Convert(param, type);

            var currType = type;

            var pos = 0;
            var depth = 0;
            while (expr.TryReadPart(".", out StringSegment member, ref pos))
            {
                try
                {
                    if (member.IndexOf('(') >= 0)
                        throw new BindingExpressionException($"Calling methods in '{expr}' is not allowed in binding expressions, use a filter instead.", member.Value, expr.Value);
                    
                    var indexerPos = member.IndexOf('[');
                    if (indexerPos >= 0)
                    {
                        var prop = member.LeftPart('[');
                        var indexer = member.RightPart('[');
                        indexer.ParseNextToken(out object value, out JsBinding binding);
                        
                        if (binding is JsExpression)
                            throw new BindingExpressionException($"Only constant binding expressions are supported: '{expr}'", member.Value, expr.Value);

                        var valueExpr = binding != null
                            ? (Expression) Expression.Call(
                                typeof(TemplatePageUtils).GetStaticMethod(nameof(EvaluateBinding)), 
                                scope,
                                Expression.Constant(binding))
                            : Expression.Constant(value);

                        if (type == typeof(string))
                        {
                            body = Expression.Call(body, typeof(string).GetMethod("ToCharArray", Type.EmptyTypes));
                            type = typeof(char[]);
                        }
                        
                        if (type.IsArray)
                        {
                            if (binding != null)
                            {
                                var evalAsInt = typeof(TemplatePageUtils).GetStaticMethod(nameof(EvaluateBindingAs))
                                    .MakeGenericMethod(typeof(int));
                                body = Expression.ArrayIndex(body, Expression.Call(evalAsInt, scope, Expression.Constant(binding)));
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
                            body = Expression.Property(body, "Item", valueExpr);
                        }
                        else
                        {
                            var pi = AssertProperty(currType, prop.Value, expr);
                            currType = pi.PropertyType;
                            body = Expression.PropertyOrField(body, prop.Value);
                        
                            var indexMethod = currType.GetMethod("get_Item", new[]{ value.GetType() });
                            body = Expression.Call(body, indexMethod, valueExpr);
                        }
                    }
                    else
                    {
                        if (depth >= 1)
                        {
                            if (type == typeof(Dictionary<string, object>))
                            {
                                var pi = AssertProperty(currType, "Item", expr);
                                currType = pi.PropertyType;
                                body = Expression.Property(body, "Item", Expression.Constant(member.Value));
                            }
                            else
                            {
                                body = Expression.PropertyOrField(body, member.Value);
                            }
                        }
                    }
    
                    depth++;
                }
                catch (BindingExpressionException) { throw; }
                catch (Exception e)
                {
                    throw new BindingExpressionException($"Could not compile '{member}' from expression '{expr}'", member.Value, expr.Value, e);
                }
            }

            body = Expression.Convert(body, typeof(object));

            return Expression.Lambda<Func<TemplateScopeContext, object, object>>(body, scope, param).Compile();
        }

        public static object EvaluateBinding(TemplateScopeContext scope, JsBinding binding)
        {
            var result = scope.EvaluateToken(binding);
            return result;
        }

        public static T EvaluateBindingAs<T>(TemplateScopeContext scope, JsBinding binding)
        {
            var result = EvaluateBinding(scope, binding);
            var converted = result.ConvertTo<T>();
            return converted;
        }

        private static PropertyInfo AssertProperty(Type currType, string prop, StringSegment expr)
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