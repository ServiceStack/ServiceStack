using System;
using System.Collections.Generic;
using ServiceStack.Text;

#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public abstract class PageFragment {}

    public class PageVariableFragment : PageFragment
    {
        public StringSegment OriginalText { get; set; }
        private byte[] originalTextBytes;
        public byte[] OriginalTextBytes => originalTextBytes ?? (originalTextBytes = OriginalText.ToUtf8Bytes());
        
        public JsToken Expression { get; }
        
        public StringSegment Binding { get; set; }
        public string BindingString { get; }       
        
        public object InitialValue { get; }
        public JsCallExpression InitialExpression { get; }
        
        public JsCallExpression[] FilterExpressions { get; set; }

        public PageVariableFragment(StringSegment originalText, JsToken expr, List<JsCallExpression> filterCommands)
        {
            OriginalText = originalText;
            Expression = expr;
            FilterExpressions = filterCommands?.ToArray() ?? TypeConstants<JsCallExpression>.EmptyArray;

            if (expr is JsLiteral initialValue)
            {
                InitialValue = initialValue.Value;
            }
            else if (expr is JsNull)
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
                BindingString = Binding.Value;
            }
        }

        public object Evaluate(TemplateScopeContext scope)
        {
            if (Expression is JsNull)
                return Expression;
            
            return Expression.Evaluate(scope);
        }
    }

    public class PageStringFragment : PageFragment
    {
        public StringSegment Value { get; set; }

        private byte[] valueBytes;
        public byte[] ValueBytes => valueBytes ?? (valueBytes = Value.ToUtf8Bytes());

        public PageStringFragment(StringSegment value)
        {
            Value = value;
        }
    }

    public class PageBlockFragment : PageFragment
    {
        public StringSegment OriginalText { get; }
        public StringSegment Name { get; }
        private string name;
        
        public string NameString => name ?? (name = Name.Value);
        public StringSegment Argument { get; }

        private string argument;
        public string ArgumentString => argument ?? (argument = Argument.HasValue ? Argument.Value : null);
        
        public PageFragment[] Body { get; }
        public PageElseBlock[] ElseBlocks { get; }

        public PageBlockFragment(StringSegment originalText, StringSegment name, StringSegment argument, 
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
        public StringSegment Argument { get; }
        public PageFragment[] Body { get; }

        public PageElseBlock(StringSegment argument, List<PageFragment> body)
        {
            Argument = argument;
            Body = body.ToArray();
        }
    }
}