﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public abstract class ScriptBlock : IConfigureScriptContext
    {
        /// <summary>
        /// How to Parse the Block's Body Contents 
        /// </summary>
        public enum BodyStyle
        {
            /// <summary>
            /// Context Sensitive. Parse as Code in Code Blocks, otherwise as Template Expressions
            /// </summary>
            Default,
            
            /// <summary>
            /// Parse Body as Template Expressions
            /// </summary>
            Template,
            
            /// <summary>
            /// Parse Body as Code Statement Blocks
            /// </summary>
            CodeBlock,

            /// <summary>
            /// Ignore Parsing Body and treat Body as Raw Text
            /// </summary>
            Verbatim,
        }

        public virtual BodyStyle ParseBody => BodyStyle.Template;
        
        public void Configure(ScriptContext context)
        {
            if (ParseBody == BodyStyle.Verbatim)
                context.ParseAsVerbatimBlock.Add(Name);
            else if (ParseBody == BodyStyle.CodeBlock)
                context.ParseAsCodeBlock.Add(Name);
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
            await scope.PageResult.WriteFragmentsAsync(scope, body, callTrace, cancel);
        }

        protected virtual async Task WriteAsync(ScriptScopeContext scope, JsStatement[] body, string callTrace, CancellationToken cancel)
        {
            await scope.PageResult.WriteStatementsAsync(scope, body, scope.OutputStream, callTrace, cancel);
        }

        protected virtual async Task WriteBodyAsync(ScriptScopeContext scope, PageBlockFragment fragment, CancellationToken token)
        {
            if (fragment.Body != null)
            {
                await WriteAsync(scope, fragment.Body, GetCallTrace(fragment), token);
            }
            else if (fragment.BodyStatement?.Statements != null)
            {
                await WriteAsync(scope, fragment.BodyStatement.Statements, GetCallTrace(fragment), token);
            }
        }

        protected virtual async Task WriteElseAsync(ScriptScopeContext scope, PageElseBlock fragment, CancellationToken token)
        {
            if (fragment.Body != null)
            {
                await WriteAsync(scope, fragment.Body, GetElseCallTrace(fragment), token);
            }
            else if (fragment.BodyStatement?.Statements != null)
            {
                await WriteAsync(scope, fragment.BodyStatement.Statements, GetElseCallTrace(fragment), token);
            }
        }

        protected async Task WriteElseAsync(ScriptScopeContext scope, PageElseBlock[] elseBlocks, CancellationToken cancel)
        {
            foreach (var elseBlock in elseBlocks)
            {
                if (elseBlock.Argument.IsNullOrEmpty())
                {
                    await WriteElseAsync(scope, elseBlock, cancel);
                    return;
                }

                var argument = elseBlock.Argument;
                if (argument.StartsWith("if "))
                    argument = argument.Advance(3);

                var result = await argument.GetJsExpressionAndEvaluateToBoolAsync(scope,
                    ifNone: () => throw new NotSupportedException("'else if' block does not have a valid expression"));
                if (result)
                {
                    await WriteElseAsync(scope, elseBlock, cancel);
                    return;
                }
            }
        }

        protected bool CanExportScopeArgs(object element) => 
            element != null && !(element is string) && (element.GetType().IsClass || element.GetType().Name == "KeyValuePair`2");

        protected int AssertWithinMaxQuota(int value)
        {
            var maxQuota = Context.MaxQuota;
            if (value > maxQuota)
                throw new NotSupportedException($"{value} exceeds Max Quota of {maxQuota}. \nMaxQuota can be changed in `Context.Args[nameof(TemplateConfig.MaxQuota)]` or globally in `TemplateConfig.MaxQuota`.");

            return value;
        }

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

}