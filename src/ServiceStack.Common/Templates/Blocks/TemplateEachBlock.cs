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
    /// </summary>
    public class TemplateEachBlock : TemplateBlock
    {
        public override string Name => "each";

        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(fragment.ArgumentString))
                throw new NotSupportedException("'each' block requires the collection to iterate");

            var cached = (EachCache)scope.Context.Cache.GetOrAdd(fragment.ArgumentString, key => ParseBlock(fragment));

            if (cached.Collection.Evaluate(scope) is IEnumerable collection)
            {
                foreach (var element in collection)
                {
                    var itemScope = scope.ScopeWithParams(new Dictionary<string, object> {
                        [cached.Binding] = element
                    });

                    await WriteBodyAsync(itemScope, fragment, cancel);
                }
            }
        }

        class EachCache
        {
            public readonly string Binding;
            public readonly JsToken Collection;

            public EachCache(string binding, JsToken collection)
            {
                Binding = binding;
                Collection = collection;
            }
        }
        
        private EachCache ParseBlock(PageBlockFragment fragment)
        {
            var literal = fragment.Argument.ParseJsExpression(out var token);
            if (token == null)
                throw new NotSupportedException("'each' block requires the collection to iterate");

            var binding = "it";
            
            literal = literal.AdvancePastWhitespace();
            if (literal.StartsWith("in "))
            {
                if (!(token is JsIdentifier identifier))
                    throw new NotSupportedException($"'each' block expected identifier but was {token.DebugToken()}");

                binding = identifier.NameString;
                
                literal = literal.Advance(3);
                literal = literal.ParseJsExpression(out token);
                if (token == null)
                    throw new NotSupportedException("'each' block requires the collection to iterate");

                literal = literal.AdvancePastWhitespace();
                if (!literal.IsNullOrEmpty())
                    throw new NotSupportedException($"invalid expression in 'each' block, near: {literal.DebugLiteral()}");
            }
            
            return new EachCache(binding, token);
        }
    }
}