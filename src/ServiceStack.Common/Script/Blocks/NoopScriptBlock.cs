using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Script
{
    /// <summary>
    /// Handlebars.js like noop block
    /// Usage: Remove {{#noop}} contents in here {{/noop}}
    /// </summary>
    public class NoopScriptBlock : ScriptBlock
    {
        public override string Name => "noop";

        public override Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token) => 
            TypeConstants.EmptyTask;
    }
}