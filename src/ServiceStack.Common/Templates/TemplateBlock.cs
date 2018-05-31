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

        public abstract Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel);

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
    }
}