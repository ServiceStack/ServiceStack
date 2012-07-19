using System.CodeDom;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;
using ServiceStack.Razor.ServiceStack;
using ServiceStack.Text;

namespace ServiceStack.Razor.Compilation.CSharp
{
	public partial class CSharpRazorCodeGenerator
	{
		public CSharpRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host) 
			: base(className, rootNamespaceName, sourceFileName, host) {}

		private void SetBaseType(string modelTypeName)
		{
			var baseClass = Host.DefaultBaseClass.SplitOnFirst('<')[0];
			var baseType = new CodeTypeReference(baseClass + "<" + modelTypeName + ">");
			GeneratedClass.BaseTypes.Clear();
			GeneratedClass.BaseTypes.Add(baseType);
		}

		protected override bool TryVisitSpecialSpan(Span span)
		{
			return TryVisit<ModelSpan>(span, VisitModelSpan);
		}

		protected override void VisitSpan(InheritsSpan span)
		{
			var baseType = new CodeTypeReference(span.BaseClass);
			GeneratedClass.BaseTypes.Clear();
			GeneratedClass.BaseTypes.Add(baseType);

			if (DesignTimeMode)
			{
				WriteHelperVariable(span.Content, "__modelHelper");
			}
		}

		private void VisitModelSpan(ModelSpan span)
		{
			SetBaseType(span.ModelTypeName);

			if (DesignTimeMode)
			{
				WriteHelperVariable(span.Content, "__modelHelper");
			}
		}
	}
}