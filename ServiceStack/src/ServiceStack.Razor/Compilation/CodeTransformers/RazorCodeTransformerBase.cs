using System.CodeDom;
using System.Collections.Generic;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    public class RazorCodeTransformerBase : IRazorCodeTransformer
    {
        void IRazorCodeTransformer.Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            Initialize((RazorPageHost)razorHost, directives);
        }

        public virtual void Initialize(RazorPageHost razorHost, IDictionary<string, string> directives)
        {
            // do nothing
        }

        public virtual void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            // do nothing.
        }

        public virtual string ProcessOutput(string codeContent)
        {
            return codeContent;
        }
    }
}
