using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script;

public abstract class ScriptBlock : IConfigureScriptContext
{
    /// <summary>
    /// Parse Body using Specified Language. Uses host language if unspecified.
    /// </summary>
    public virtual ScriptLanguage Body { get; }
        
    public void Configure(ScriptContext context)
    {
        if (Body != null)
            context.ParseAsLanguage[Name] = Body;
    }

    public ScriptContext Context { get; set; }
    public ISharpPages Pages { get; set; }
    public abstract string Name { get; }
        
    protected virtual string GetCallTrace(PageBlockFragment fragment) => "Block: " + Name + 
                                                                         (fragment.Argument.IsNullOrEmpty() ? "" : " (" + fragment.Argument + ")");

    protected virtual string GetElseCallTrace(PageElseBlock fragment) => "Block: " + Name + " > Else" + 
                                                                         (fragment.Argument.IsNullOrEmpty() ? "" : " (" + fragment.Argument + ")");

    public abstract Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token);

    protected virtual async Task WriteAsync(ScriptScopeContext scope, PageFragment[] body, string callTrace, CancellationToken cancel)
    {
        await scope.PageResult.WriteFragmentsAsync(scope, body, callTrace, cancel).ConfigAwait();
    }

    protected virtual async Task WriteAsync(ScriptScopeContext scope, JsStatement[] body, string callTrace, CancellationToken cancel)
    {
        await scope.PageResult.WriteStatementsAsync(scope, body, callTrace, cancel).ConfigAwait();
    }

    protected virtual async Task WriteBodyAsync(ScriptScopeContext scope, PageBlockFragment fragment, CancellationToken token)
    {
        await WriteAsync(scope, fragment.Body, GetCallTrace(fragment), token).ConfigAwait();
    }

    protected virtual async Task WriteElseAsync(ScriptScopeContext scope, PageElseBlock fragment, CancellationToken token)
    {
        await WriteAsync(scope, fragment.Body, GetElseCallTrace(fragment), token).ConfigAwait();
    }

    protected async Task WriteElseAsync(ScriptScopeContext scope, PageElseBlock[] elseBlocks, CancellationToken cancel)
    {
        foreach (var elseBlock in elseBlocks)
        {
            if (elseBlock.Argument.IsNullOrEmpty())
            {
                await WriteElseAsync(scope, elseBlock, cancel).ConfigAwait();
                return;
            }

            var argument = elseBlock.Argument;
            if (argument.StartsWith("if "))
                argument = argument.Advance(3);

            var result = await argument.GetJsExpressionAndEvaluateToBoolAsync(scope,
                ifNone: () => throw new NotSupportedException("'else if' block does not have a valid expression")).ConfigAwait();
            if (result)
            {
                await WriteElseAsync(scope, elseBlock, cancel).ConfigAwait();
                return;
            }
        }
    }

    protected bool CanExportScopeArgs(object element) => 
        element != null && !(element is string) && (element.GetType().IsClass || element.GetType().Name == "KeyValuePair`2");

    protected int AssertWithinMaxQuota(int value) => Context.DefaultMethods.AssertWithinMaxQuota(value);

}

public class DefaultScriptBlocks : IScriptPlugin
{
    public void Register(ScriptContext context)
    {
        context.ScriptBlocks.AddRange(new ScriptBlock[] {
            new IfScriptBlock(),
            new EachScriptBlock(),
            new RawScriptBlock(),
            new CaptureScriptBlock(), 
            new PartialScriptBlock(),
            new WithScriptBlock(),
            new NoopScriptBlock(),
            new KeyValuesScriptBlock(),
            new CsvScriptBlock(),
            new FunctionScriptBlock(), 
            new WhileScriptBlock(),
        });
    }
}

public class ProtectedScriptBlocks : IScriptPlugin
{
    public void Register(ScriptContext context)
    {
        context.ScriptBlocks.AddRange(new ScriptBlock[] {
            new EvalScriptBlock(), // evalScript has same functionality and is registered by default 
        });
    }
}