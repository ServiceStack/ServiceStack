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
    ///         {{#each n in numbers where n > 5}} {{it}} {{else}} no numbers > 5 {{/each}}
    /// </summary>
    public class TemplateEachBlock : TemplateBlock
    {
        public override string Name => "each";

        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(fragment.ArgumentString))
                throw new NotSupportedException("'each' block requires the collection to iterate");

            var cache = (EachArg)scope.Context.Cache.GetOrAdd(fragment.ArgumentString, _ => ParseArgument(scope, fragment));
            
            IEnumerable collection = (IEnumerable) await cache.Source.EvaluateAsync(scope);

            var index = 0;
            var whereIndex = 0;
            if (collection != null)
            {
                foreach (var element in collection)
                {
                    // Add all properties into scope if called without explicit in argument 
                    var scopeArgs = !cache.HasExplicitBinding && CanExportScopeArgs(element)
                        ? element.ToObjectDictionary()
                        : new Dictionary<string, object>();

                    scopeArgs[cache.Binding] = element;
                    scopeArgs[nameof(index)] = AssertWithinMaxQuota(whereIndex++); 
                    var itemScope = scope.ScopeWithParams(scopeArgs);

                    if (cache.Where != null)
                    {
                        var result = await cache.Where.EvaluateToBoolAsync(itemScope);
                        if (!result)
                            continue;
                    }
                    
                    itemScope.ScopedParams[nameof(index)] = AssertWithinMaxQuota(index++);

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

            JsToken source, where = null;
            
            var hasExplicitBinding = literal.StartsWith("in "); 
            if (hasExplicitBinding)
            {
                if (!(token is JsIdentifier identifier))
                    throw new NotSupportedException($"'each' block expected identifier but was {token.DebugToken()}");

                binding = identifier.NameString;
                
                literal = literal.Advance(3);
                literal = literal.ParseJsExpression(out source);
                if (source == null)
                    throw new NotSupportedException("'each' block requires the collection to iterate");
            }
            else
            {
                source = token;
            }

            if (literal.StartsWith("where "))
            {
                literal = literal.Advance(6);
                literal = literal.ParseJsExpression(out where);
            }
            
            return new EachArg(binding, hasExplicitBinding, source, where);
        }

        class EachArg
        {
            public readonly string Binding;
            public readonly bool HasExplicitBinding;
            public readonly JsToken Source;
            public readonly JsToken Where;
            public EachArg(string binding, bool hasExplicitBinding, JsToken source, JsToken where)
            {
                Binding = binding;
                HasExplicitBinding = hasExplicitBinding;
                Source = source;
                Where = where;
            }
        }
    }
}