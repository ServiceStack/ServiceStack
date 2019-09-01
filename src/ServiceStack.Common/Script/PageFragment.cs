using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public abstract class PageFragment {}

    public class PageVariableFragment : PageFragment
    {
        public ReadOnlyMemory<char> OriginalText { get; set; }
        
        private ReadOnlyMemory<byte> originalTextUtf8;
        public ReadOnlyMemory<byte> OriginalTextUtf8 => originalTextUtf8.IsEmpty ? (originalTextUtf8 = OriginalText.ToUtf8()) : OriginalTextUtf8;
        
        public JsToken Expression { get; }
        
        public string Binding { get; }
        
        public object InitialValue { get; }
        public JsCallExpression InitialExpression { get; }
        
        public JsCallExpression[] FilterExpressions { get; }

        public PageVariableFragment(ReadOnlyMemory<char> originalText, JsToken expr, List<JsCallExpression> filterCommands)
        {
            OriginalText = originalText;
            Expression = expr;
            FilterExpressions = filterCommands?.ToArray() ?? TypeConstants<JsCallExpression>.EmptyArray;

            if (expr is JsLiteral initialValue)
            {
                InitialValue = initialValue.Value;
            }
            else if (ReferenceEquals(expr, JsNull.Value))
            {
                InitialValue = expr;
            }
            else if (expr is JsCallExpression initialExpr)
            {
                InitialExpression = initialExpr;
            }
            else if (expr is JsIdentifier initialBinding)
            {
                Binding = initialBinding.Name;
            }
        }

        public object Evaluate(ScriptScopeContext scope)
        {
            if (ReferenceEquals(Expression, JsNull.Value))
                return Expression;
            
            return Expression.Evaluate(scope);
        }

        protected bool Equals(PageVariableFragment other)
        {
            return OriginalText.Span.SequenceEqual(other.OriginalText.Span) 
                   && Equals(Expression, other.Expression)
                   && FilterExpressions.EquivalentTo(other.FilterExpressions);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PageVariableFragment) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OriginalText.GetHashCode();
                hashCode = (hashCode * 397) ^ (Expression != null ? Expression.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FilterExpressions != null ? FilterExpressions.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class PageStringFragment : PageFragment
    {
        public ReadOnlyMemory<char> Value { get; set; }
        
        private string valueString;
        public string ValueString => valueString ?? (valueString = Value.ToString());

        private ReadOnlyMemory<byte> valueUtf8;
        public ReadOnlyMemory<byte> ValueUtf8 => valueUtf8.IsEmpty ? (valueUtf8 = Value.ToUtf8()) : valueUtf8;

        public PageStringFragment(string value) : this(value.AsMemory()) {}
        public PageStringFragment(ReadOnlyMemory<char> value) => Value = value;

        protected bool Equals(PageStringFragment other)
        {
            return Value.Span.SequenceEqual(other.Value.Span);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PageStringFragment) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class PageBlockFragment : PageFragment
    {
        public ReadOnlyMemory<char> OriginalText { get; }
        public string Name { get; }

        public ReadOnlyMemory<char> Argument { get; }
        private string argumentString;
        public string ArgumentString => argumentString ?? (argumentString = Argument.ToString());

        public PageFragment[] Body { get; }
        public JsBlockStatement BodyStatement { get; }
        public PageElseBlock[] ElseBlocks { get; }

        public PageBlockFragment(string originalText, string name, string argument,
            JsBlockStatement body, List<PageElseBlock> elseStatements=null) 
            : this (originalText.AsMemory(), name, argument.AsMemory(), body, elseStatements) {}
        public PageBlockFragment(ReadOnlyMemory<char> originalText, string name, ReadOnlyMemory<char> argument,
            JsBlockStatement body, List<PageElseBlock> elseStatements=null)
        {
            OriginalText = originalText;
            Name = name;
            Argument = argument;
            BodyStatement = body;
            ElseBlocks = elseStatements?.ToArray() ?? TypeConstants<PageElseBlock>.EmptyArray;
        }
        
        public PageBlockFragment(string originalText, string name, string argument,
            List<PageFragment> body, List<PageElseBlock> elseStatements=null) 
            : this (originalText.AsMemory(), name, argument.AsMemory(), body, elseStatements) {}
        public PageBlockFragment(ReadOnlyMemory<char> originalText, string name, ReadOnlyMemory<char> argument, 
            List<PageFragment> body, List<PageElseBlock> elseStatements=null)
        {
            OriginalText = originalText;
            Name = name;
            Argument = argument;
            Body = body.ToArray();
            ElseBlocks = elseStatements?.ToArray() ?? TypeConstants<PageElseBlock>.EmptyArray;
        }

        protected bool Equals(PageBlockFragment other)
        {
            return Name == other.Name && 
                   Argument.Span.SequenceEqual(other.Argument.Span) && 
                   Body.EquivalentTo(other.Body) && 
                   ElseBlocks.EquivalentTo(other.ElseBlocks);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PageBlockFragment) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Argument.GetHashCode();
                hashCode = (hashCode * 397) ^ (Body != null ? Body.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ElseBlocks != null ? ElseBlocks.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class PageElseBlock : PageFragment
    {
        public ReadOnlyMemory<char> Argument { get; }
        public PageFragment[] Body { get; }
        public JsBlockStatement BodyStatement { get; }

        public PageElseBlock(ReadOnlyMemory<char> argument, List<PageFragment> body)
        {
            Argument = argument;
            Body = body.ToArray();
        }

        public PageElseBlock(ReadOnlyMemory<char> argument, JsBlockStatement bodyStatement)
        {
            Argument = argument;
            BodyStatement = bodyStatement;
        }

        public PageElseBlock(string argument, JsBlockStatement bodyStatement) 
            : this(argument.AsMemory(), bodyStatement) {}

        protected bool Equals(PageElseBlock other)
        {
            return Argument.Span.SequenceEqual(other.Argument.Span) &&
                   Body.EquivalentTo(other.Body) &&
                   Equals(BodyStatement, other.BodyStatement);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PageElseBlock) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Argument.GetHashCode();
                hashCode = (hashCode * 397) ^ (Body != null ? Body.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BodyStatement != null ? BodyStatement.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class PageJsBlockStatementFragment : PageFragment
    {
        public JsBlockStatement Block { get; }
        public PageJsBlockStatementFragment(JsBlockStatement statement)
        {
            Block = statement;
        }

        protected bool Equals(PageJsBlockStatementFragment other)
        {
            return Equals(Block, other.Block);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PageJsBlockStatementFragment) obj);
        }

        public override int GetHashCode()
        {
            return (Block != null ? Block.GetHashCode() : 0);
        }
    }

}