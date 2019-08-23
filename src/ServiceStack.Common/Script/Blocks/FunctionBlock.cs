using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    /// <summary>
    /// Define a reusable
    /// Usage: {{#function add(a, b) }}
    ///           a * b | to => c
    ///           a + b + c | return
    ///        {{/function}}
    /// </summary>
    public class FunctionBlock : ScriptBlock
    {
        public override string Name => "function";

        public override Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var invokerCtx = (Tuple<string,MethodInvoker>)scope.Context.CacheMemory.GetOrAdd(block.Argument, key => {
                var literal = block.Argument.Span.ParseVarName(out var name);
                literal = literal.AdvancePastWhitespace();

                literal = literal.AdvancePastWhitespace();
                var args = TypeConstants.EmptyStringList;
                if (!literal.IsEmpty)
                {
                    literal = literal.ParseArgumentsList(out var argIdentifiers);
                    args = argIdentifiers.Map(x => x.Name);
                }

                var strFragment = (PageStringFragment) block.Body[0];

                var script = ScriptPreprocessors.TransformCodeBlocks($"```code\n{strFragment.ValueString}\n```");
                var parsedScript = scope.Context.OneTimePage(script);

                MethodInvoker invoker = (instance, paramValues) => {
                    var pageResult = new PageResult(parsedScript);

                    var len = Math.Min(paramValues.Length, args.Count);
                    for (int i = 0; i < len; i++)
                    {
                        var paramValue = paramValues[i];
                        pageResult.Args[args[i]] = paramValue;
                    }
                    
                    var discard = ScriptContextUtils.GetPageResultOutput(pageResult);
                    if (pageResult.ReturnValue == null)
                        throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
            
                    return pageResult.ReturnValue.Result;
                };
                
                return Tuple.Create(name.ToString(), invoker);
            });

            scope.PageResult.Args[invokerCtx.Item1] = invokerCtx.Item2;

            return TypeConstants.EmptyTask;
        }

        public class FunctionContext
        {
            public string Name { get; }
            public List<string> Args { get; }
            public string OriginalSource { get; }
            public SharpPage Body { get; }

            public FunctionContext(string originalSource, string name, List<string> args, SharpPage body)
            {
                Name = name;
                Args = args;
                OriginalSource = originalSource;
                Body = body;
            }
        }
    }
}