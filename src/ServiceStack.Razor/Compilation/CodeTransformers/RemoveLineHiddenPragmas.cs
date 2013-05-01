using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    public class RemoveLineHiddenPragmas : RazorCodeTransformerBase
    {
        private const string LinePragmaText = "#line hidden";

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            var linePragmaStatements = new List<CodeSnippetTypeMember>();
            foreach (var member in generatedClass.Members.OfType<CodeSnippetTypeMember>())
            {
                if (member.Text.TrimEnd().Equals(LinePragmaText, StringComparison.OrdinalIgnoreCase))
                {
                    // If the snippet is entirely a line pragma, mark it for removal.
                    linePragmaStatements.Add(member);
                }
                if (member.Text.StartsWith(LinePragmaText, StringComparison.OrdinalIgnoreCase))
                {
                    member.Text = member.Text.Substring(LinePragmaText.Length);
                }
            }

            foreach (var item in linePragmaStatements)
            {
                generatedClass.Members.Remove(item);
            }
        }
    }
}
