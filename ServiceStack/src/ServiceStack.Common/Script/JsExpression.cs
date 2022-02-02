using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public abstract class JsExpression : JsToken
    {
        public abstract Dictionary<string, object> ToJsAst();

        public virtual string ToJsAstType() => GetType().ToJsAstType();
    }
    
    public class JsIdentifier : JsExpression
    {
        public string Name { get; }

        public JsIdentifier(string name) => Name = name;
        public JsIdentifier(ReadOnlySpan<char> name) => Name = name.Value();
        public override string ToRawString() => ":" + Name;
        
        public override object Evaluate(ScriptScopeContext scope)
        {
            var ret = scope.PageResult.GetValue(Name, scope);
            return ret;
        }

        protected bool Equals(JsIdentifier other) => string.Equals(Name, other.Name);

        public override Dictionary<string, object> ToJsAst() => new Dictionary<string, object> {
            ["type"] = ToJsAstType(),
            ["name"] = Name,
        };

        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsIdentifier) obj);
        }

        public override string ToString() => ToRawString();
    }

    public class JsLiteral : JsExpression
    {
        public static JsLiteral True = new JsLiteral(true);
        public static JsLiteral False = new JsLiteral(false);
        
        public object Value { get; }
        public JsLiteral(object value) => Value = value;
        public override string ToRawString() => JsonValue(Value);

        public override int GetHashCode() => (Value != null ? Value.GetHashCode() : 0);
        protected bool Equals(JsLiteral other) => Equals(Value, other.Value);
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((JsLiteral) obj);
        }

        public override string ToString() => ToRawString();

        public override object Evaluate(ScriptScopeContext scope) => Value;

        public override Dictionary<string, object> ToJsAst() => new Dictionary<string, object> {
            ["type"] = ToJsAstType(),
            ["value"] = Value,
            ["raw"] = JsonValue(Value),
        };
    }

    public class JsArrayExpression : JsExpression
    {
        public JsToken[] Elements { get; }

        public JsArrayExpression(params JsToken[] elements) => Elements = elements.ToArray();
        public JsArrayExpression(IEnumerable<JsToken> elements) : this(elements.ToArray()) {}

        public override object Evaluate(ScriptScopeContext scope)
        {
            var to = new List<object>();
            foreach (var element in Elements)
            {
                if (element is JsSpreadElement spread)
                {
                    if (!(spread.Argument.Evaluate(scope) is IEnumerable arr))
                        continue;

                    foreach (var value in arr)
                    {
                        to.Add(value);
                    }
                }
                else
                {
                    var value = element.Evaluate(scope);
                    to.Add(value);
                }
            }
            return to;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("[");
            for (var i = 0; i < Elements.Length; i++)
            {
                if (i > 0) 
                    sb.Append(",");
                
                var element = Elements[i];
                sb.Append(element.ToRawString());
            }
            sb.Append("]");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var elements = new List<object>();
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["elements"] = elements
            };

            foreach (var element in Elements)
            {
                elements.Add(element.ToJsAst());
            }

            return to;
        }

        protected bool Equals(JsArrayExpression other)
        {
            return Elements.EquivalentTo(other.Elements);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsArrayExpression) obj);
        }

        public override int GetHashCode()
        {
            return (Elements != null ? Elements.GetHashCode() : 0);
        }
    }

    public class JsObjectExpression : JsExpression
    {
        public JsProperty[] Properties { get; }

        public JsObjectExpression(params JsProperty[] properties) => Properties = properties;
        public JsObjectExpression(IEnumerable<JsProperty> properties) : this(properties.ToArray()) {}

        public static string GetKey(JsToken token)
        {
            if (token is JsLiteral literalKey)
                return literalKey.Value.ToString();
            if (token is JsIdentifier identifierKey)
                return identifierKey.Name;
            if (token is JsMemberExpression memberExpr && memberExpr.Property is JsIdentifier prop)
                return prop.Name;
            
            throw new SyntaxErrorException($"Invalid Key. Expected a Literal or Identifier but was {token.DebugToken()}");
        }

        public override object Evaluate(ScriptScopeContext scope)
        {
            var to = new Dictionary<string, object>();
            foreach (var prop in Properties)
            {
                if (prop.Key == null)
                {
                    if (prop.Value is JsSpreadElement spread)
                    {
                        var value = spread.Argument.Evaluate(scope);
                        var obj = value.ToObjectDictionary();
                        if (obj != null)
                        {
                            foreach (var entry in obj)
                            {
                                to[entry.Key] = entry.Value;
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Object Expressions does not have a key");
                    }
                }
                else
                {
                    var keyString = GetKey(prop.Key);
                    var value = prop.Value.Evaluate(scope);
                    to[keyString] = value;
                }
            }
            return to;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("{");
            for (var i = 0; i < Properties.Length; i++)
            {
                if (i > 0) 
                    sb.Append(",");
                
                var prop = Properties[i];
                if (prop.Key != null)
                {
                    sb.Append(prop.Key.ToRawString());
                    if (!prop.Shorthand)
                    {
                        sb.Append(":");
                        sb.Append(prop.Value.ToRawString());
                    }
                }
                else //.... spread operator
                {
                    sb.Append(prop.Value.ToRawString());
                }
            }
            sb.Append("}");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var properties = new List<object>();
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["properties"] = properties
            };

            var propType = typeof(JsProperty).ToJsAstType();
            foreach (var prop in Properties)
            {
                properties.Add(new Dictionary<string, object> {
                    ["type"] = propType,
                    ["key"] = prop.Key?.ToJsAst(), 
                    ["computed"] = false, //syntax not supported: { ["a" + 1]: 2 }
                    ["value"] = prop.Value.ToJsAst(), 
                    ["kind"] = "init",
                    ["method"] = false,
                    ["shorthand"] = prop.Shorthand,
                });
            }

            return to;
        }

        protected bool Equals(JsObjectExpression other)
        {
            return Properties.EquivalentTo(other.Properties);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsObjectExpression) obj);
        }

        public override int GetHashCode()
        {
            return (Properties != null ? Properties.GetHashCode() : 0);
        }
    }

    public class JsProperty
    {
        public JsToken Key { get; }
        public JsToken Value { get; }
        public bool Shorthand { get; }

        public JsProperty(JsToken key, JsToken value) : this(key, value, shorthand:false){}
        public JsProperty(JsToken key, JsToken value, bool shorthand)
        {
            Key = key;
            Value = value;
            Shorthand = shorthand;
        }

        protected bool Equals(JsProperty other)
        {
            return Equals(Key, other.Key) && 
                   Equals(Value, other.Value) && 
                   Shorthand == other.Shorthand;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsProperty) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Shorthand.GetHashCode();
                return hashCode;
            }
        }
    }

    public class JsSpreadElement : JsExpression
    {
        public JsToken Argument { get; }
        public JsSpreadElement(JsToken argument)
        {
            Argument = argument;
        }

        public override object Evaluate(ScriptScopeContext scope)
        {
            return Argument.Evaluate(scope);
        }

        public override string ToRawString()
        {
            return "..." + Argument.ToRawString();
        }

        public override Dictionary<string, object> ToJsAst() => new Dictionary<string, object>
        {
            ["type"] = ToJsAstType(),
            ["argument"] = Argument.ToJsAst()
        };

        protected bool Equals(JsSpreadElement other)
        {
            return Equals(Argument, other.Argument);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsSpreadElement) obj);
        }

        public override int GetHashCode()
        {
            return (Argument != null ? Argument.GetHashCode() : 0);
        }
    }

    public class JsArrowFunctionExpression : JsExpression
    {
        public JsIdentifier[] Params { get; }
        public JsToken Body { get; }

        public JsArrowFunctionExpression(JsIdentifier param, JsToken body) : this(new[] {param}, body) {}
        public JsArrowFunctionExpression(JsIdentifier[] @params, JsToken body)
        {
            Params = @params ?? throw new SyntaxErrorException($"Params missing in Arrow Function Expression");
            Body = body ?? throw new SyntaxErrorException($"Body missing in Arrow Function Expression");
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();

            sb.Append("(");
            for (var i = 0; i < Params.Length; i++)
            {
                var identifier = Params[i];
                if (i > 0)
                    sb.Append(",");

                sb.Append(identifier.Name);
            }
            sb.Append(") => ");

            sb.Append(Body.ToRawString());
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override object Evaluate(ScriptScopeContext scope) => this;

        public object Invoke(params object[] @params) => Invoke(JS.CreateScope(), @params);
        public object Invoke(ScriptScopeContext scope, params object[] @params)
        {
            var args = new Dictionary<string, object>();
            for (var i = 0; i < Params.Length; i++)
            {
                if (@params.Length < i)
                    break;

                var param = Params[i];
                args[param.Name] = @params[i];
            }

            var exprScope = scope.ScopeWithParams(args);
            var ret = Body.Evaluate(exprScope);
            return ret;
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["id"] = null,
            };
            var args = new List<object>();
            to["params"] = args;

            foreach (var param in Params)
            {
                args.Add(param.ToJsAst());
            }

            to["body"] = Body.ToJsAst();
            return to;
        }

        protected bool Equals(JsArrowFunctionExpression other)
        {
            return Params.EquivalentTo(other.Params) && Equals(Body, other.Body);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsArrowFunctionExpression) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Params != null ? Params.GetHashCode() : 0) * 397) ^ (Body != null ? Body.GetHashCode() : 0);
            }
        }
    }
    
}