/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * This software is subject to the Microsoft Public License (Ms-PL). 
 * A copy of the license can be found in the license.htm file included 
 * in this distribution.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Linq.Expressions;
using ServiceStack.Markdown;

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
