using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Script 
{
    public class TemplateScriptLanguage : ScriptLanguage
    {
        public static ScriptLanguage Instance = new TemplateScriptLanguage();
        
        public override string Name => "template";
        
        public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
        {
            var pageFragments = context.ParseTemplate(body);
            return pageFragments;
        }
        
        public override async Task<bool> WritePageFragmentAsync(ScriptScopeContext scope, PageFragment fragment, CancellationToken token)
        {
            if (fragment is PageStringFragment str)
            {
                await scope.OutputStream.WriteAsync(str.ValueUtf8, token);
            }
            else if (fragment is PageVariableFragment var)
            {
                if (var.Binding?.Equals(ScriptConstants.Page) == true)
                {
                    await scope.PageResult.WritePageAsync(scope.PageResult.Page, scope.PageResult.CodePage, scope, token);

                    if (scope.PageResult.HaltExecution)
                        scope.PageResult.HaltExecution = false; //break out of page but continue evaluating layout
                }
                else
                {
                    await scope.PageResult.WriteVarAsync(scope, var, token);
                }
            }
            else if (fragment is PageBlockFragment blockFragment)
            {
                var block = scope.PageResult.GetBlock(blockFragment.Name);
                await block.WriteAsync(scope, blockFragment, token);
            }
            else return false;

            return true; 
        }
    }
}