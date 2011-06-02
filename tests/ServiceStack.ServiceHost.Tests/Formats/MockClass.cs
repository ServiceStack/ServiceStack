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
	 : ServiceStack.Markdown.MarkdownViewBase
	{
		public System.Boolean EvalExpr_0(System.Collections.Generic.List<ServiceStack.ServiceHost.Tests.Formats.Product> products)
		{
			return (products.Count == 0);
		}
	}
}
