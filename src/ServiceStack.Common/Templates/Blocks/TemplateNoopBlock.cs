using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    /// <summary>
    /// Handlebars.js like noop block
    /// Usage: Remove {{#noop}} contents in here {{/noop}}
    /// </summary>
    public class TemplateNoopBlock : TemplateBlock
    {
        public override string Name => "noop";

        public override Task WriteAsync(TemplateScopeContext scope, PageBlockFragment block, CancellationToken token) => 
            TypeConstants.EmptyTask;
    }
}