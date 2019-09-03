using System;
using System.Collections.Generic;

namespace ServiceStack.Script
{
    public abstract class ScriptLanguage
    {
        public abstract string Name { get; }

        public abstract List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers);
        
        public List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body) => Parse(context, body, default);
    }

    public class TemplateScriptLanguage : ScriptLanguage
    {
        public static ScriptLanguage Instance = new TemplateScriptLanguage();
        
        public override string Name => "template";
        
        public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
        {
            var pageFragments = context.ParseTemplate(body);
            return pageFragments;
        }
    }

    public class CodeScriptLanguage : ScriptLanguage
    {
        public static ScriptLanguage Instance = new CodeScriptLanguage();
        
        public override string Name => "code";

        public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
        {
            var statements = context.ParseCodeStatements(body);
            
            return new List<PageFragment> { new PageJsBlockStatementFragment(new JsBlockStatement(statements)) };
        }
    }
    
}