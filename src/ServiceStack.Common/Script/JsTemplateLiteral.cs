using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public class JsTemplateLiteral : JsExpression
    {
        public JsTemplateElement[] Quasis { get; }
        public JsToken[] Expressions { get; }
        
        public JsTemplateLiteral(string cooked) 
            : this(new []{ new JsTemplateElement(cooked, cooked, tail:true) }){}

        public JsTemplateLiteral(JsTemplateElement[] quasis=null, JsToken[] expressions=null)
        {
            Quasis = quasis ?? TypeConstants<JsTemplateElement>.EmptyArray;
            Expressions = expressions ?? TypeConstants<JsToken>.EmptyArray;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();

            sb.Append("`");

            for (int i = 0; i < Quasis.Length; i++)
            {
                var quasi = Quasis[i];
                sb.Append(quasi.Value.Raw);
                if (quasi.Tail)
                    break;

                var expr = Expressions[i];
                sb.Append("${");
                sb.Append(expr.ToRawString());
                sb.Append("}");
            }

            sb.Append("`");

            var ret = StringBuilderCache.ReturnAndFree(sb);
            return ret;
        }

        public override object Evaluate(ScriptScopeContext scope)
        {
            var sb = StringBuilderCache.Allocate();
            
            for (int i = 0; i < Quasis.Length; i++)
            {
                var quasi = Quasis[i];
                sb.Append(quasi.Value.Cooked);
                if (quasi.Tail)
                    break;

                var expr = Expressions[i];
                var value = expr.Evaluate(scope);
                sb.Append(value);
            }

            var ret = StringBuilderCache.ReturnAndFree(sb);
            return ret;
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var to = new Dictionary<string, object> {
                ["type"] = ToJsAstType(),
            };

            var quasiType = typeof(JsTemplateElement).ToJsAstType();
            var quasis = new List<object>();
            foreach (var quasi in Quasis)
            {
                quasis.Add(new Dictionary<string, object> {
                    ["type"] = quasiType,
                    ["value"] = new Dictionary<string, object> {
                        ["raw"] = quasi.Value.Raw,
                        ["cooked"] = quasi.Value.Cooked,
                    },
                    ["tail"] = quasi.Tail,
                });
            }
            to["quasis"] = quasis;

            var expressions = new List<object>();
            foreach (var expression in Expressions)
            {
                expressions.Add(expression.ToJsAst());
            }
            to["expressions"] = expressions;

            return to;
        }

        protected bool Equals(JsTemplateLiteral other)
        {
            return Quasis.EquivalentTo(other.Quasis) && 
                   Expressions.EquivalentTo(other.Expressions);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsTemplateLiteral) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Quasis != null ? Quasis.GetHashCode() : 0) * 397) ^ (Expressions != null ? Expressions.GetHashCode() : 0);
            }
        }

        public override string ToString() => ToRawString();
    }

    public class JsTemplateElement
    {
        public JsTemplateElementValue Value { get; }
        public bool Tail { get; }
        
        public JsTemplateElement(string raw, string cooked, bool tail=false) : 
            this(new JsTemplateElementValue(raw, cooked), tail){}
        
        public JsTemplateElement(JsTemplateElementValue value, bool tail)
        {
            Value = value;
            Tail = tail;
        }

        protected bool Equals(JsTemplateElement other)
        {
            return Equals(Value, other.Value) && Tail == other.Tail;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsTemplateElement) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ Tail.GetHashCode();
            }
        }
    }

    public class JsTemplateElementValue
    {
        public string Raw { get; }
        public string Cooked { get; }
        
        public JsTemplateElementValue(string raw, string cooked)
        {
            Raw = raw;
            Cooked = cooked;
        }

        protected bool Equals(JsTemplateElementValue other)
        {
            return Raw.Equals(other.Raw) && Cooked.Equals(other.Cooked);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsTemplateElementValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Raw.GetHashCode() * 397) ^ Cooked.GetHashCode();
            }
        }
    }
}