using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    /// <summary>
    /// Partial Block doesn't emit anything it only creates and saves a partial in the PageResult
    /// 
    /// Usages: {{#partial mypartial}} contents {{/partial}}
    ///         {{#partial mypartial {format:'html'} }} contents {{/partial}}
    ///         {{#partial mypartial {format:'html', pageArg:1} }} contents {{/partial}}
    /// </summary>
    public class PartialScriptBlock : ScriptBlock
    {
        public override string Name => "partial";

        public override Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var literal = block.Argument.ParseVarName(out var name);
            if (name.IsNullOrEmpty())
                throw new NotSupportedException("'partial' block is missing name of partial");
            
            literal = literal.AdvancePastWhitespace();

            var argValue = literal.GetJsExpressionAndEvaluate(scope);
            var args = argValue as Dictionary<string, object>;

            if (argValue != null && args == null)
                throw new NotSupportedException("Any 'partial' argument must be an Object Dictionary");

            var format = scope.Context.PageFormats.First().Extension;
            if (args != null && args.TryGetValue(ScriptConstants.Format, out var oFormat))
            {
                format = oFormat.ToString();
                args.Remove(ScriptConstants.Format);
            }

            var nameString = name.ToString();
            var partial = new SharpPartialPage(scope.Context, nameString, block.Body, format, args);
            scope.PageResult.Partials[nameString] = partial;

            return TypeConstants.EmptyTask;
        }
    }
}