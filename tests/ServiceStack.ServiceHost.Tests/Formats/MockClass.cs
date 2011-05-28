using System;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using ServiceStack.Markdown;
using ServiceStack.Markdown.Html;

namespace CSharpEval
{
	public class _Expr
	 : ServiceStack.Markdown.MarkdownViewBase<ServiceStack.ServiceHost.Tests.Formats.TemplateTests.Person>
	{
		public MvcHtmlString EvalExpr_1(ServiceStack.ServiceHost.Tests.Formats.TemplateTests.Person Model)
		{
			return (Html.TextBoxFor(m => m.FirstName));
		}
	}
}
