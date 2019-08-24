using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    /// <summary>
    /// Define a reusable
    /// Usage: {{#function calc(a, b) }}
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
                var strName = name.ToString();
                literal = literal.AdvancePastWhitespace();

                literal = literal.AdvancePastWhitespace();
                var args = TypeConstants.EmptyStringList;
                if (!literal.IsEmpty)
                {
                    literal = literal.ParseArgumentsList(out var argIdentifiers);
                    args = argIdentifiers.Map(x => x.Name);
                }

                var strFragment = (PageStringFragment) block.Body[0];

                var script = ScriptPreprocessors.TransformStatementBody(strFragment.ValueString);
                var parsedScript = scope.Context.OneTimePage(script);

                MethodInvoker invoker = null;

                // Allow recursion by initializing lazy Delegate
                object LazyInvoker(object instance, object[] paramValues)
                {
                    if (invoker == null) 
                        throw new NotSupportedException($"Uninitialized function '{strName}'");

                    return invoker(instance, paramValues);
                }

                invoker = (instance, paramValues) => {
                    scope.PageResult.StackDepth++;
                    try
                    {
                        var pageResult = new PageResult(parsedScript) {
                            Args = {
                                [strName] = (MethodInvoker) LazyInvoker
                            },
                            StackDepth = scope.PageResult.StackDepth
                        };

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
                    }
                    finally
                    {
                        scope.PageResult.StackDepth--;
                    }
                };
                
                return Tuple.Create(strName, invoker);
            });

            scope.PageResult.Args[invokerCtx.Item1] = invokerCtx.Item2;

            return TypeConstants.EmptyTask;
        }
    }
}