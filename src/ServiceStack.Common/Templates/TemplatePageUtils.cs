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
        static readonly char[] VarDelimiters = { '|', '}' };

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
                to.Add(new PageStringFragment(block));

                var varStartPos = pos + 2;
                var varEndPos = text.IndexOfAny(VarDelimiters, varStartPos);
                var varName = text.Subsegment(varStartPos, varEndPos - varStartPos).Trim();
                if (varEndPos == -1)
                    throw new ArgumentException($"Invalid Server HTML Template at '{text.SafeSubsegment(50)}...'", nameof(text));

                List<JsExpression> filterCommands = null;
                
                var isFilter = text.GetChar(varEndPos) == '|';
                if (isFilter)
                {
                    filterCommands = text.Subsegment(varEndPos + 1).ParseExpression<JsExpression>(
                        separator: '|',
                        atEndIndex: (str, strPos) =>
                        {
                            while (str.Length > strPos && char.IsWhiteSpace(str.GetChar(strPos)))
                                strPos++;

                            if (str.Length > strPos + 1 && str.GetChar(strPos) == '}' && str.GetChar(strPos + 1) == '}')
                            {
                                varEndPos = varEndPos + 1 + strPos + 1;
                                return strPos;
                            }
                            return null;
                        });
                }
                else
                {
                    varEndPos += 1;
                }

                lastPos = varEndPos + 1;
                var originalText = text.Subsegment(pos, lastPos - pos);

                to.Add(new PageVariableFragment(originalText, varName, filterCommands));
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

        public static Func<object, object> Compile(Type type, StringSegment expr)
        {
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
    
                        if (depth == 0)
                        {
                            var pi = AssertProperty(currType, "Item", expr);
                            currType = pi.PropertyType;
                            body = Expression.Property(body, "Item", Expression.Constant(value));
                        }
                        else
                        {
                            var pi = AssertProperty(currType, prop.Value, expr);
                            currType = pi.PropertyType;
                            body = Expression.PropertyOrField(body, prop.Value);
                        
                            var indexMethod = currType.GetMethod("get_Item", new[]{ value.GetType() });
                            body = Expression.Call(body, indexMethod, Expression.Constant(value));
                        }
                    }
                    else
                    {
                        if (depth >= 1)
                            body = Expression.PropertyOrField(body, member.Value);
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

            return Expression.Lambda<Func<object, object>>(body, param).Compile();
        }

        private static PropertyInfo AssertProperty(Type currType, string prop, StringSegment expr)
        {
            var pi = currType.GetProperty(prop);
            if (pi == null)
                throw new ArgumentException(
                    $"Property '{prop}' does not exist on Type '{currType.Name}' from binding expression '{expr}'");
            return pi;
        }
    }
}