using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Script
{
    public abstract class ScriptLanguage
    {
        public static ScriptLanguage Verbatim => ScriptVerbatim.Language;
        
        public abstract string Name { get; }

        public List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body) => Parse(context, body, default);

        public abstract List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers);
        
        public virtual Task<bool> WritePageFragmentAsync(ScriptScopeContext scope, PageFragment fragment, CancellationToken token) => TypeConstants.FalseTask;

        public virtual Task<bool> WriteStatementAsync(ScriptScopeContext scope, JsStatement statement, CancellationToken token) => TypeConstants.FalseTask;

        public virtual PageBlockFragment ParseVerbatimBlock(string blockName, ReadOnlyMemory<char> argument, ReadOnlyMemory<char> body)
        {
            var bodyFragment = new List<PageFragment> { new PageStringFragment(body) };
            var blockFragment = new PageBlockFragment(blockName, argument, bodyFragment);
            return blockFragment;
        }
    }

    public sealed class ScriptVerbatim : ScriptLanguage
    {
        public static readonly ScriptLanguage Language = new ScriptVerbatim();

        public override string Name => "verbatim";

        public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers) => 
            new List<PageFragment> {
                new PageStringFragment(body)
            };
    }
}