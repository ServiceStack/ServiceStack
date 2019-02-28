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
        
        public string Binding { get; set; }
        
        public object InitialValue { get; }
        public JsCallExpression InitialExpression { get; }
        
        public JsCallExpression[] FilterExpressions { get; set; }

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
    }

    public class PageStringFragment : PageFragment
    {
        public ReadOnlyMemory<char> Value { get; set; }
        
        private string valueString;
        public string ValueString => valueString ?? (valueString = Value.ToString());

        private ReadOnlyMemory<byte> valueUtf8;
        public ReadOnlyMemory<byte> ValueUtf8 => valueUtf8.IsEmpty ? (valueUtf8 = Value.ToUtf8()) : valueUtf8;

        public PageStringFragment(ReadOnlyMemory<char> value)
        {
            Value = value;
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
        public PageElseBlock[] ElseBlocks { get; }

        public PageBlockFragment(ReadOnlyMemory<char> originalText, string name, ReadOnlyMemory<char> argument, 
            List<PageFragment> body, List<PageElseBlock> elseStatements=null)
        {
            OriginalText = originalText;
            Name = name;
            Argument = argument;
            Body = body.ToArray();
            ElseBlocks = elseStatements?.ToArray() ?? TypeConstants<PageElseBlock>.EmptyArray;
        }
    }

    public class PageElseBlock : PageFragment
    {
        public ReadOnlyMemory<char> Argument { get; }
        public PageFragment[] Body { get; }

        public PageElseBlock(ReadOnlyMemory<char> argument, List<PageFragment> body)
        {
            Argument = argument;
            Body = body.ToArray();
        }
    }
}