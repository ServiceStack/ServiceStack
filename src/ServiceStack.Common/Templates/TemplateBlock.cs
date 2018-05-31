using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    public abstract class TemplateBlock
    {
        public TemplateContext Context { get; set; }
        public ITemplatePages Pages { get; set; }
        public abstract string Name { get; }

        public abstract Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken token);

        protected virtual async Task WriteBodyAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken token)
        {
            await scope.PageResult.WriteFragmentsAsync(scope, fragment.Body, "Block: " + Name, token);
        }
    }
}