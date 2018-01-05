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
        
        public StringSegment Binding { get; set; }
        public string BindingString { get; }       
        
        public object InitialValue { get; }
        public JsExpression InitialExpression { get; }
        
        public JsExpression[] FilterExpressions { get; set; }

        public PageVariableFragment(StringSegment originalText, object initialValue, JsBinding initialBinding, List<JsExpression> filterCommands)
        {
            OriginalText = originalText;
            InitialValue = initialValue;
            FilterExpressions = filterCommands?.ToArray() ?? TypeConstants<JsExpression>.EmptyArray;

            if (initialBinding is JsExpression initialExpr)
            {
                InitialExpression = initialExpr;
            }
            else if (initialBinding != null)
            {
                Binding = initialBinding.Binding;
                BindingString = Binding.Value;
            }
        }

        public StringSegment ParseNextToken(StringSegment literal, out object value, out JsBinding binding)
        {
            try
            {
                return literal.ParseNextToken(out value, out binding);
            }
            catch (ArgumentException e)
            {
                throw new Exception($"Invalid literal: {literal} in '{OriginalText}'", e);
            }
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