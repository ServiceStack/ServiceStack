using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class VirtualPathAttribute : Attribute
    {
        public VirtualPathAttribute( string virtualPath )
        {
            VirtualPath = virtualPath;
        }

        public string VirtualPath { get; private set; }
    }

    public class AddPageVirtualPathAttribute : RazorCodeTransformerBase
    {
        private const string VirtualPathDirectiveKey = "VirtualPath";
        private string _projectRelativePath;
        private string _overriddenVirtualPath;

        public override void Initialize(RazorPageHost razorHost, IDictionary<string, string> directives)
        {
            _projectRelativePath = razorHost.File.VirtualPath;
            directives.TryGetValue(VirtualPathDirectiveKey, out _overriddenVirtualPath);
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            Debug.Assert(_projectRelativePath != null, "Initialize has to be called before we get here.");
            var virtualPath = _overriddenVirtualPath ?? VirtualPathUtility.ToAppRelative("~/" + _projectRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            generatedClass.CustomAttributes.Add(
                new CodeAttributeDeclaration(typeof(VirtualPathAttribute).FullName,
                new CodeAttributeArgument(new CodePrimitiveExpression(virtualPath))));
        }
    }
}
