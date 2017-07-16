using System;
using System.Collections.Generic;
using ServiceStack.Text;

#if NETSTANDARD1_3
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

        private string bindingString;
        public string BindingString => bindingString ?? (bindingString = Binding.Value);
        
        public object Value { get; set; }
        
        public JsExpression Expression { get; set; }
        
        public JsExpression[] FilterExpressions { get; set; }

        public PageVariableFragment(StringSegment originalText, StringSegment name, List<JsExpression> filterCommands)
        {
            OriginalText = originalText;
            FilterExpressions = filterCommands?.ToArray() ?? TypeConstants<JsExpression>.EmptyArray;

            ParseNextToken(name, out object value, out JsBinding binding);

            Value = value;

            if (binding is JsExpression expr)
                Expression = expr;
            else if (binding != null)
                Binding = binding.Binding;
        }

        public void ParseNextToken(StringSegment literal, out object value, out JsBinding binding)
        {
            try
            {
                literal.ParseNextToken(out value, out binding);
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