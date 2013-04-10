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
using System.Linq;
using System.Linq.Expressions;
using ServiceStack.Markdown;

namespace ServiceStack.Html
{
	public static class LabelExtensions
	{
		public static MvcHtmlString Label(this HtmlHelper html, string expression)
		{
			return Label(html, expression, null);
		}

		public static MvcHtmlString Label(this HtmlHelper html, string expression, string labelText)
		{
			return LabelHelper(html,
				ModelMetadata.FromStringExpression(expression, html.ViewData),
				expression,
				labelText);
		}

		public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression)
		{
			return LabelFor(html, expression, null);
		}

		public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string labelText)
		{
			return LabelHelper(html,
				ModelMetadata.FromLambdaExpression(expression, html.ViewData),
				ExpressionHelper.GetExpressionText(expression),
				labelText);
		}

		public static MvcHtmlString LabelForModel(this HtmlHelper html)
		{
			return LabelForModel(html, null);
		}

		public static MvcHtmlString LabelForModel(this HtmlHelper html, string labelText)
		{
			return LabelHelper(html, html.ViewData.ModelMetadata, String.Empty, labelText);
		}

		internal static MvcHtmlString LabelHelper(HtmlHelper html, ModelMetadata metadata, string htmlFieldName, string labelText = null)
		{
			var resolvedLabelText = labelText ?? metadata.DisplayName ?? metadata.PropertyName ?? htmlFieldName.Split('.').Last();
			if (String.IsNullOrEmpty(resolvedLabelText))
			{
				return MvcHtmlString.Empty;
			}

			var tag = new TagBuilder("label");
			tag.Attributes.Add("for", TagBuilder.CreateSanitizedId(htmlFieldName));
			tag.SetInnerText(resolvedLabelText);
			return tag.ToHtmlString(TagRenderMode.Normal);
		}
	}
}
