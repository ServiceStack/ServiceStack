using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates.Blocks
{
    /// <summary>
    /// Handlebars.js like each block
    /// Usages: {{#each collection}} {{it}} {{/each}}
    ///         {{#each num in numbers}} {{num}} {{/each}}
    ///         {{#each num in [1,2,3]}} {{num}} {{/each}}
    ///         {{#each numbers}} {{it}} {{else}} no numbers {{/each}}
    ///         {{#each numbers}} {{it}} {{else if letters != null}} has letters {{else}} no numbers {{/each}}
    /// </summary>
    public class TemplateEachBlock : TemplateBlock
    {
        public override string Name => "each";

        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(fragment.ArgumentString))
                throw new NotSupportedException("'each' block requires the collection to iterate");

            var literal = fragment.Argument.ParseJsExpression(out var token);
            if (token == null)
                throw new NotSupportedException("'each' block requires the collection to iterate");

            var binding = "it";

            IEnumerable collection = null;
            
            literal = literal.AdvancePastWhitespace();
            var hasExplicitBinding = literal.StartsWith("in "); 
            if (hasExplicitBinding)
            {
                if (!(token is JsIdentifier identifier))
                    throw new NotSupportedException($"'each' block expected identifier but was {token.DebugToken()}");

                binding = identifier.NameString;
                
                literal = literal.Advance(3);
                collection = literal.GetJsExpressionAndEvaluate(scope,
                    ifNone: () => throw new NotSupportedException("'each' block requires the collection to iterate")) as ICollection;
            }
            else
            {
                collection = token.Evaluate(scope) as ICollection;
            }

            var index = 0;
            if (collection != null)
            {
                foreach (var element in collection)
                {
                    // Add all properties into scope if called without explicit in argument 
                    var scopeArgs = !hasExplicitBinding && element?.GetType().IsClass == true
                        ? element.ToObjectDictionary()
                        : new Dictionary<string, object>();

                    scopeArgs[binding] = element;
                    scopeArgs[nameof(index)] = index++; 

                    var itemScope = scope.ScopeWithParams(scopeArgs);

                    await WriteBodyAsync(itemScope, fragment, cancel);
                }
            }

            if (index == 0)
            {
                await WriteElseBlocks(scope, fragment.ElseBlocks, cancel);
            }
        }
    }
}