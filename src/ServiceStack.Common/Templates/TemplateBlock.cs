using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    public abstract class TemplateBlock
    {
        public TemplateContext Context { get; set; }
        public ITemplatePages Pages { get; set; }
        public abstract string Name { get; }
        
        protected virtual string GetCallTrace(PageBlockFragment fragment) => "Block: " + Name + 
           (fragment.Argument.IsNullOrEmpty() ? "" : " (" + fragment.Argument + ")");

        protected virtual string GetElseCallTrace(PageElseBlock fragment) => "Block: " + Name + " > Else" + 
           (fragment.Argument.IsNullOrEmpty() ? "" : " (" + fragment.Argument + ")");

        public abstract Task WriteAsync(TemplateScopeContext scope, PageBlockFragment block, CancellationToken token);

        protected virtual async Task WriteAsync(TemplateScopeContext scope, PageFragment[] body, string callTrace, CancellationToken cancel)
        {
            await scope.PageResult.WriteFragmentsAsync(scope, body, callTrace, cancel);
        }

        protected virtual async Task WriteBodyAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken token)
        {
            await WriteAsync(scope, fragment.Body, GetCallTrace(fragment), token);
        }

        protected virtual async Task WriteElseAsync(TemplateScopeContext scope, PageElseBlock fragment, CancellationToken token)
        {
            await WriteAsync(scope, fragment.Body, GetElseCallTrace(fragment), token);
        }

        protected async Task WriteElseAsync(TemplateScopeContext scope, PageElseBlock[] elseBlocks, CancellationToken cancel)
        {
            foreach (var elseBlock in elseBlocks)
            {
                if (elseBlock.Argument.IsNullOrEmpty())
                {
                    await WriteElseAsync(scope, elseBlock, cancel);
                    return;
                }

                var argument = elseBlock.Argument;
                if (argument.StartsWith("if "))
                    argument = argument.Advance(3);

                var result = await argument.GetJsExpressionAndEvaluateToBoolAsync(scope,
                    ifNone: () => throw new NotSupportedException("'else if' block does not have a valid expression"));
                if (result)
                {
                    await WriteElseAsync(scope, elseBlock, cancel);
                    return;
                }
            }
        }

        protected bool CanExportScopeArgs(object element) => 
            element != null && !(element is string) && (element.GetType().IsClass || element.GetType().Name == "KeyValuePair`2");

        protected int AssertWithinMaxQuota(int value)
        {
            var maxQuota = (int)Context.Args[nameof(TemplateConfig.MaxQuota)];
            if (value > maxQuota)
                throw new NotSupportedException($"{value} exceeds Max Quota of {maxQuota}. \nMaxQuota can be changed in `Context.Args[nameof(TemplateConfig.MaxQuota)]` or globally in `TemplateConfig.MaxQuota`.");

            return value;
        }
    }

    public class TemplateDefaultBlocks : ITemplatePlugin
    {
        public void Register(TemplateContext context)
        {
            context.TemplateBlocks.AddRange(new TemplateBlock[] {
                new TemplateIfBlock(),
                new TemplateEachBlock(),
                new TemplateRawBlock(),
                new TemplateCaptureBlock(), 
                new TemplatePartialBlock(),
                new TemplateWithBlock(),
                new TemplateNoopBlock(),
            });
        }
    }

    public class TemplateProtectedBlocks : ITemplatePlugin
    {
        public void Register(TemplateContext context)
        {
            context.TemplateBlocks.AddRange(new TemplateBlock[] {
                new TemplateEvalBlock(), // evalTemplate filter has same functionality and is registered by default 
            });
        }
    }

}