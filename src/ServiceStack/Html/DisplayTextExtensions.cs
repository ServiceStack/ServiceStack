// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;

namespace ServiceStack.Html
{
	public static class DisplayTextExtensions
	{
		public static MvcHtmlString DisplayText(this HtmlHelper html, string name)
		{
			return DisplayTextHelper(ModelMetadata.FromStringExpression(name, html.ViewData));
		}

		public static MvcHtmlString DisplayTextFor<TModel, TResult>(this HtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression)
		{
			return DisplayTextHelper(ModelMetadata.FromLambdaExpression(expression, html.ViewData));
		}

		private static MvcHtmlString DisplayTextHelper(ModelMetadata metadata)
		{
			return MvcHtmlString.Create(metadata.SimpleDisplayText);
		}
	}
}
