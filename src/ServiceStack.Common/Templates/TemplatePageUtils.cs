using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class RawString : IRawString
    {
        public static RawString Empty = new RawString("");
        
        private readonly string value;
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

                var isComment = text.GetChar(varStartPos) == '*';
                if (!isComment)
                {
                    var literal = text.Subsegment(varStartPos).ParseNextToken(out object initialValue, out JsBinding initialBinding, allowWhitespaceSyntax:true);
    
                    List<JsExpression> filterCommands = null;
    
                    literal = literal.ParseNextToken(out _, out JsBinding filterOp);
                    if (filterOp == JsBitwiseOr.Operator)
                    {
                        var varEndPos = 0;
                        bool foundVarEnd = false;
                    
                        filterCommands = literal.ParseExpression<JsExpression>(
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
    
                        literal = literal.Advance(varEndPos);
                    }
                    else
                    {
                        literal = literal.Advance(1);
                    }
    
                    var length = text.Length - pos - literal.Length;
                    var originalText = text.Subsegment(pos, length);
                    lastPos = pos + length;
    
                    var varFragment = new PageVariableFragment(originalText, initialValue, initialBinding, filterCommands);
                    to.Add(varFragment);
    
                    var newLineLen = literal.StartsWith("\n")
                        ? 1
                        : literal.StartsWith("\r\n")
                            ? 2
                            : 0;
                    if (newLineLen > 0)
                    {
                        var lastExpr = varFragment.FilterExpressions?.LastOrDefault();
                        var filterName = lastExpr?.NameString ?? varFragment?.InitialExpression?.NameString ?? varFragment.BindingString;
                        if (filterName != null && TemplateConfig.RemoveNewLineAfterFiltersNamed.Contains(filterName))
                        {
                            lastPos += newLineLen;
                        }
                    }
                }
                else
                {
                    lastPos = text.IndexOf("*}}", varStartPos) + 3;
                }
            }

            if (lastPos != text.Length)
            {
                var lastBlock = lastPos == 0 ? text : text.Subsegment(lastPos);
                to.Add(new PageStringFragment(lastBlock));
            }

            return to;
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
            var body = CreateBindingExpression(type, expr, scope, param);

            body = Expression.Convert(body, typeof(object));
            return Expression.Lambda<Func<TemplateScopeContext, object, object>>(body, scope, param).Compile();
        }

        private static Expression CreateBindingExpression(Type type, StringSegment expr, ParameterExpression scope, ParameterExpression instance)
        {
            Expression body = Expression.Convert(instance, type);

            var currType = type;

            var pos = 0;
            var depth = 0;
            while (expr.TryReadPart(".", out StringSegment member, ref pos))
            {
                try
                {
                    if (member.IndexOf('(') >= 0)
                        throw new BindingExpressionException(
                            $"Calling methods in '{expr}' is not allowed in binding expressions, use a filter instead.",
                            member.Value, expr.Value);

                    var indexerPos = member.IndexOf('[');
                    if (indexerPos >= 0)
                    {
                        var prop = member.LeftPart('[');
                        var indexer = member.RightPart('[');
                        indexer.ParseNextToken(out object value, out JsBinding binding);

                        if (binding is JsExpression)
                            throw new BindingExpressionException($"Only constant binding expressions are supported: '{expr}'",
                                member.Value, expr.Value);

                        var valueExpr = binding != null
                            ? (Expression) Expression.Call(
                                typeof(TemplatePageUtils).GetStaticMethod(nameof(EvaluateBinding)),
                                scope,
                                Expression.Constant(binding))
                            : Expression.Constant(value);

                        if (currType == typeof(string))
                        {
                            body = CreateStringIndexExpression(body, binding, scope, valueExpr, ref currType);
                        }
                        else if (currType.IsArray)
                        {
                            if (binding != null)
                            {
                                var evalAsInt = typeof(TemplatePageUtils).GetStaticMethod(nameof(EvaluateBindingAs))
                                    .MakeGenericMethod(typeof(int));
                                body = Expression.ArrayIndex(body,
                                    Expression.Call(evalAsInt, scope, Expression.Constant(binding)));
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

                            if (binding != null)
                            {
                                var indexType = pi.GetGetMethod()?.GetParameters().FirstOrDefault()?.ParameterType;
                                if (indexType != typeof(object))
                                {
                                    var evalAsInt = typeof(TemplatePageUtils).GetStaticMethod(nameof(EvaluateBindingAs))
                                        .MakeGenericMethod(indexType);
                                    valueExpr = Expression.Call(evalAsInt, scope, Expression.Constant(binding));
                                }
                            }

                            body = Expression.Property(body, "Item", valueExpr);
                        }
                        else
                        {
                            var pi = AssertProperty(currType, prop.Value, expr);
                            currType = pi.PropertyType;
                            body = Expression.PropertyOrField(body, prop.Value);

                            if (currType == typeof(string))
                            {
                                body = CreateStringIndexExpression(body, binding, scope, valueExpr, ref currType);
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
                            var memberName = member.Value;
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
                    throw new BindingExpressionException($"Could not compile '{member}' from expression '{expr}'", member.Value,
                        expr.Value, e);
                }
            }
            return body;
        }

        public static Action<TemplateScopeContext, object, object> CompileAssign(Type type, StringSegment expr)
        {
            var scope = Expression.Parameter(typeof(TemplateScopeContext), "scope");
            var instance = Expression.Parameter(typeof(object), "instance");
            var valueToAssign = Expression.Parameter(typeof(object), "valueToAssign");

            var body = CreateBindingExpression(type, expr, scope, instance);
            if (body is IndexExpression propItemExpr)
            {
                var mi = propItemExpr.Indexer.GetSetMethod();
                var indexExpr = propItemExpr.Arguments[0];
                body = Expression.Call(propItemExpr.Object, mi, indexExpr, valueToAssign);
            }
            else if (body is System.Linq.Expressions.BinaryExpression binaryExpr && binaryExpr.NodeType == ExpressionType.ArrayIndex)
            {
                var arrayInstance = binaryExpr.Left;
                var indexExpr = binaryExpr.Right;
                
                body = Expression.Assign(
                    Expression.ArrayAccess(arrayInstance, indexExpr), 
                    valueToAssign);
            }
            else
            {
                throw new BindingExpressionException($"Assignment expression for '{expr}' not supported yet", "valueToAssign", expr.Value);
            }

            return Expression.Lambda<Action<TemplateScopeContext, object, object>>(body, scope, instance, valueToAssign).Compile();
        }

        private static Expression CreateStringIndexExpression(Expression body, JsBinding binding, ParameterExpression scope,
            Expression valueExpr, ref Type currType)
        {
            body = Expression.Call(body, typeof(string).GetMethod("ToCharArray", Type.EmptyTypes));
            currType = typeof(char[]);

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
            return body;
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