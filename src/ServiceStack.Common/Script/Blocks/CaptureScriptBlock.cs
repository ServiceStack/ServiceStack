using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    /// <summary>
    /// Captures the output and assigns it to the specified variable.
    /// Accepts an optional Object Dictionary as scope arguments when evaluating body.
    ///
    /// Usages: {{#capture output}} {{#each args}} - [{{it}}](/path?arg={{it}}) {{/each}} {{/capture}}
    ///         {{#capture output {nums:[1,2,3]} }} {{#each nums}} {{it}} {{/each}} {{/capture}}
    ///         {{#capture appendTo output {nums:[1,2,3]} }} {{#each nums}} {{it}} {{/each}} {{/capture}}
    /// </summary>
    public class CaptureScriptBlock : ScriptBlock
    {
        public override string Name => "capture";

        internal struct Tuple
        {
            internal string name;
            internal Dictionary<string, object> scopeArgs;
            internal bool appendTo;
            internal Tuple(string name, Dictionary<string, object> scopeArgs, bool appendTo)
            {
                this.name = name;
                this.scopeArgs = scopeArgs;
                this.appendTo = appendTo;
            }
        }

        public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var tuple = Parse(scope, block);
            var name = tuple.name;

            using (var ms = MemoryStreamFactory.GetStream())
            {
                var useScope = scope.ScopeWith(tuple.scopeArgs, ms);

                await WriteBodyAsync(useScope, block, token);

                var capturedOutput = ms.ReadToEnd();

                if (tuple.appendTo && scope.PageResult.Args.TryGetValue(name, out var oVar)
                             && oVar is string existingString)
                {
                    scope.PageResult.Args[name] = existingString + capturedOutput;
                    return;
                }
            
                scope.PageResult.Args[name] = capturedOutput;
            }
        }

        //Extract usages of Span outside of async method 
        private Tuple Parse(ScriptScopeContext scope, PageBlockFragment block)
        {
            if (block.Argument.IsNullOrWhiteSpace())
                throw new NotSupportedException("'capture' block is missing name of variable to assign captured output to");
            
            var literal = block.Argument.AdvancePastWhitespace();
            bool appendTo = false;
            if (literal.StartsWith("appendTo "))
            {
                appendTo = true;
                literal = literal.Advance("appendTo ".Length);
            }
                
            literal = literal.ParseVarName(out var name);
            if (name.IsNullOrEmpty())
                throw new NotSupportedException("'capture' block is missing name of variable to assign captured output to");

            literal = literal.AdvancePastWhitespace();

            var argValue = literal.GetJsExpressionAndEvaluate(scope);

            var scopeArgs = argValue as Dictionary<string, object>;

            if (argValue != null && scopeArgs == null)
                throw new NotSupportedException("Any 'capture' argument must be an Object Dictionary");

            return new Tuple(name.ToString(), scopeArgs, appendTo);
        }
    }
}