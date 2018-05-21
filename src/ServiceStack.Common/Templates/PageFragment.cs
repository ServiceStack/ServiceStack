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
        public CallExpression InitialExpression { get; }
        
        public CallExpression[] FilterExpressions { get; set; }

        public PageVariableFragment(StringSegment originalText, JsToken expr, List<CallExpression> filterCommands)
        {
            OriginalText = originalText;
            Expression = expr;
            FilterExpressions = filterCommands?.ToArray() ?? TypeConstants<CallExpression>.EmptyArray;

            if (expr is JsConstant initialValue)
            {
                InitialValue = initialValue.Value;
            }
            else if (expr is JsNull)
            {
                InitialValue = expr;
            }
            else if (expr is CallExpression initialExpr)
            {
                InitialExpression = initialExpr;
            }
            else if (expr is JsBinding initialBinding)
            {
                Binding = initialBinding.Binding;
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
}