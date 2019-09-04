using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Script
{
    public abstract class ScriptLanguage
    {
        public abstract string Name { get; }

        public abstract List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers);
        
        public List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body) => Parse(context, body, default);

        public virtual Task<bool> WritePageFragmentAsync(ScriptScopeContext scope, PageFragment fragment, CancellationToken token) => TypeConstants.FalseTask;

        public virtual Task<bool> WriteStatementAsync(ScriptScopeContext scope, JsStatement statement, CancellationToken token) => TypeConstants.FalseTask;
    }
}