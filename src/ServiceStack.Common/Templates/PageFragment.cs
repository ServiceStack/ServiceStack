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
        
        public StringSegment Name { get; set; }
        private string nameString;
        public string NameString => nameString ?? (nameString = Name.Value);
        
        public object Value { get; set; }
        
        public JsExpression Expression { get; set; }
        
        public JsExpression[] FilterExpressions { get; set; }

        public PageVariableFragment(StringSegment originalText, StringSegment name, List<JsExpression> filterCommands)
        {
            OriginalText = originalText;
            FilterExpressions = filterCommands?.ToArray() ?? TypeConstants<JsExpression>.EmptyArray;

            ParseLiteral(name, out StringSegment outName, out object value, out JsExpression expr);

            Name = outName;
            Value = value;
            Expression = expr;
        }

        public void ParseLiteral(StringSegment literal, out StringSegment name, out object value, out JsExpression cmd)
        {
            try
            {
                literal.ParseNextToken(out name, out value, out cmd);
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