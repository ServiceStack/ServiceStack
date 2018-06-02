using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    /// <summary>
    /// Handlebars.js like each block
    /// Usages: {{#each customers}} {{Name}} {{/each}}
    ///         {{#each customers}} {{it.Name}} {{/each}}
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

            var cache = (EachArg)scope.Context.Cache.GetOrAdd(fragment.ArgumentString, _ => ParseArgument(scope, fragment));
            
            IEnumerable collection = (IEnumerable) cache.Source.Evaluate(scope);

            var index = 0;
            if (collection != null)
            {
                foreach (var element in collection)
                {
                    // Add all properties into scope if called without explicit in argument 
                    var scopeArgs = !cache.HasExplicitBinding && CanExportScopeArgs(element)
                        ? element.ToObjectDictionary()
                        : new Dictionary<string, object>();

                    scopeArgs[cache.Binding] = element;
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

        EachArg ParseArgument(TemplateScopeContext scope, PageBlockFragment fragment)
        {
            var literal = fragment.Argument.ParseJsExpression(out var token);
            if (token == null)
                throw new NotSupportedException("'each' block requires the collection to iterate");

            var binding = "it";

            literal = literal.AdvancePastWhitespace();

            JsToken source;
            
            var hasExplicitBinding = literal.StartsWith("in "); 
            if (hasExplicitBinding)
            {
                if (!(token is JsIdentifier identifier))
                    throw new NotSupportedException($"'each' block expected identifier but was {token.DebugToken()}");

                binding = identifier.NameString;
                
                literal = literal.Advance(3);
                literal.ParseJsExpression(out source);
                if (source == null)
                    throw new NotSupportedException("'each' block requires the collection to iterate");
            }
            else
            {
                source = token;
            }
            
            return new EachArg(binding, hasExplicitBinding, source);
        }

        class EachArg
        {
            public string Binding;
            public readonly bool HasExplicitBinding;
            public readonly JsToken Source;
            public EachArg(string binding, bool hasExplicitBinding, JsToken source)
            {
                Binding = binding;
                HasExplicitBinding = hasExplicitBinding;
                Source = source;
            }
        }
    }
}