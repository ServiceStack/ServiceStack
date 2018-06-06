using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    /// <summary>
    /// Special block which captures the body as a string fragment
    ///
    /// Usages: {{#raw}}emit verbatim{{/raw}}
    ///         {{#raw varname}}assigned to varname{{/raw}}
    /// </summary>
    public class TemplateRawBlock : TemplateBlock
    {
        public override string Name => "raw";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            var strFragment = (PageStringFragment)fragment.Body[0];

            if (!fragment.Argument.IsNullOrWhiteSpace())
            {
                var literal = fragment.Argument.ParseVarName(out var name);
                scope.PageResult.Args[name.Value] = strFragment.Value.Value; 
            }
            else
            {
                await scope.OutputStream.WriteAsync(strFragment.Value, cancel);
            }
        }
    }
}