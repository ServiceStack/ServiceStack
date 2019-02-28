using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    /// <summary>
    /// Special block which captures the raw body as a string fragment
    ///
    /// Usages: {{#raw}}emit {{ verbatim }} body{{/raw}}
    ///         {{#raw varname}}assigned to varname{{/raw}}
    ///         {{#raw appendTo varname}}appended to varname{{/raw}}
    /// </summary>
    public class RawScriptBlock : ScriptBlock
    {
        public override string Name => "raw";
        
        public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var strFragment = (PageStringFragment)block.Body[0];

            if (!block.Argument.IsNullOrWhiteSpace())
            {
                Capture(scope, block, strFragment);
            }
            else
            {
                await scope.OutputStream.WriteAsync(strFragment.Value.Span, token);
            }
        }

        private static void Capture(ScriptScopeContext scope, PageBlockFragment block, PageStringFragment strFragment)
        {
            var literal = block.Argument.Span.AdvancePastWhitespace();
            bool appendTo = false;
            if (literal.StartsWith("appendTo "))
            {
                appendTo = true;
                literal = literal.Advance("appendTo ".Length);
            }

            literal = literal.ParseVarName(out var name);
            var nameString = name.Value();
            if (appendTo && scope.PageResult.Args.TryGetValue(nameString, out var oVar)
                         && oVar is string existingString)
            {
                scope.PageResult.Args[nameString] = existingString + strFragment.Value;
                return;
            }

            scope.PageResult.Args[nameString] = strFragment.Value.ToString();
        }
    }
}