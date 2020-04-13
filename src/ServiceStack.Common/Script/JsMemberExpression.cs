using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public class JsMemberExpression : JsExpression
    {
        public JsToken Object { get; }
        public JsToken Property { get; }
        public bool Computed { get; } //indexer

        public JsMemberExpression(JsToken @object, JsToken property) : this(@object, property, false) { }

        public JsMemberExpression(JsToken @object, JsToken property, bool computed)
        {
            Object = @object;
            Property = property;
            Computed = computed;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append(Object.ToRawString());
            if (Computed)
            {
                sb.Append("[");
                sb.Append(Property.ToRawString());
                sb.Append("]");
            }
            else
            {
                sb.Append(".");
                sb.Append(Property.ToRawString());
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["computed"] = Computed,
                ["object"] = Object.ToJsAst(),
                ["property"] = Property.ToJsAst(),
            };
            return to;
        }

        public override object Evaluate(ScriptScopeContext scope)
        {
            var targetValue = Object.Evaluate(scope);
            var ret = GetValue(targetValue, scope);
            return Equals(ret, JsNull.Value) 
                ? null 
                : ret;
        }

        private static object PropValue(object targetValue, Type targetType, string name)
        {
            var memberFn = TypeProperties.Get(targetType).GetPublicGetter(name)
                           ?? TypeFields.Get(targetType).GetPublicGetter(name);

            if (memberFn != null)
            {
                return memberFn(targetValue);
            }

            var methods = targetType.GetInstanceMethods();
            var indexerMethod = 
                methods.FirstOrDefault(x => x.Name == "get_Item" && x.GetParameters().Any(p => p.ParameterType == typeof(string))) ??
                methods.FirstOrDefault(x => x.Name == "get_Item" && x.GetParameters().Any(p => p.ParameterType != typeof(string)));
            
            if (indexerMethod != null)
            {
                var fn = indexerMethod.GetInvoker();
                var ret = fn(targetValue, name);
                return ret;
            }

            throw new ArgumentException($"'{targetType.Name}' does not have a '{name}' property or field");
        }

        private object GetValue(object targetValue, ScriptScopeContext scope)
        {
            if (targetValue == null || targetValue == JsNull.Value)
                return JsNull.Value;
            var targetType = targetValue.GetType();

            try
            {
                if (!Computed)
                {
                    if (Property is JsIdentifier identifier)
                    {
                        var ret = PropValue(targetValue, targetType, identifier.Name);

                        // Don't emit member expression on null KeyValuePair
                        if (ret == null && targetType.Name == "KeyValuePair`2")
                            return JsNull.Value;

                        return ret;
                    }
                }
                else
                {
                    var indexValue = Property.Evaluate(scope);
                    if (indexValue == null)
                        return JsNull.Value;

                    if (targetType.IsArray)
                    {
                        var array = (Array) targetValue;
                        if (indexValue is long l)
                            return array.GetValue(l);
                        var intValue = indexValue.ConvertTo<int>();
                        return array.GetValue(intValue);
                    }
                    if (targetValue is IDictionary dict)
                    {
                        var ret = dict[indexValue];
                        return ret ?? JsNull.Value;
                    }
                    if (indexValue is string propName)
                    {
                        return PropValue(targetValue, targetType, propName);
                    }
                    if (targetValue is IList list)
                    {
                        var intValue = indexValue.ConvertTo<int>();
                        return list[intValue];
                    }
                    if (targetValue is IEnumerable e)
                    {
                        var intValue = indexValue.ConvertTo<int>();
                        var i = 0;
                        foreach (var item in e)
                        {
                            if (i++ == intValue)
                                return item;
                        }
                        return null;
                    }
                    if (DynamicNumber.IsNumber(indexValue.GetType()))
                    {
                        var indexerMethod = targetType.GetInstanceMethod("get_Item");
                        if (indexerMethod != null)
                        {
                            var fn = indexerMethod.GetInvoker();
                            var ret = fn(targetValue, indexValue);
                            return ret ?? JsNull.Value;
                        }
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                return JsNull.Value;
            }
            catch (Exception ex)
            {
                var exResult = scope.PageResult.Format.OnExpressionException(scope.PageResult, ex);
                if (exResult != null)
                    return exResult;

                var expr = ToRawString();
                throw new BindingExpressionException($"Could not evaluate expression '{expr}'", null, expr, ex);
            }

            throw new NotSupportedException($"'{targetValue.GetType()}' does not support access by '{Property}'");
        }

        protected bool Equals(JsMemberExpression other)
        {
            return Equals(Object, other.Object) &&
                   Equals(Property, other.Property) &&
                   Computed == other.Computed;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsMemberExpression) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Object != null ? Object.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Property != null ? Property.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Computed.GetHashCode();
                return hashCode;
            }
        }
    }
}