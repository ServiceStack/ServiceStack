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

    public class PageStatementFragment : PageFragment
    {
        public StringSegment OriginalText { get; }
        public StringSegment Name { get; }
        public StringSegment Expression { get; }
        public List<PageFragment> Body { get; }
        public List<PageElseStatement> ElseStatements { get; }

        public PageStatementFragment(StringSegment originalText, StringSegment name, StringSegment expression, 
            List<PageFragment> body, List<PageElseStatement> elseStatements=null)
        {
            OriginalText = originalText;
            Name = name;
            Expression = expression;
            Body = body;
            ElseStatements = elseStatements ?? new List<PageElseStatement>();
        }
    }

    public class PageElseStatement : PageFragment
    {
        public StringSegment Expression { get; }
        public List<PageFragment> Body { get; }

        public PageElseStatement(StringSegment expression, List<PageFragment> body)
        {
            Expression = expression;
            Body = body;
        }
    }
}