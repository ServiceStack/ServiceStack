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
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using ServiceStack.Markdown;

namespace ServiceStack.Html
{
	public static class InputExtensions
	{
		// CheckBox

		public static MvcHtmlString CheckBox(this HtmlHelper htmlHelper, string name)
		{
			return CheckBox(htmlHelper, name, (object)null /* htmlAttributes */);
		}

		public static MvcHtmlString CheckBox(this HtmlHelper htmlHelper, string name, bool isChecked)
		{
			return CheckBox(htmlHelper, name, isChecked, (object)null /* htmlAttributes */);
		}

		public static MvcHtmlString CheckBox(this HtmlHelper htmlHelper, string name, bool isChecked, object htmlAttributes)
		{
			return CheckBox(htmlHelper, name, isChecked, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString CheckBox(this HtmlHelper htmlHelper, string name, object htmlAttributes)
		{
			return CheckBox(htmlHelper, name, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString CheckBox(this HtmlHelper htmlHelper, string name, IDictionary<string, object> htmlAttributes)
		{
			return CheckBoxHelper(htmlHelper, null, name, null /* isChecked */, htmlAttributes);
		}

		public static MvcHtmlString CheckBox(this HtmlHelper htmlHelper, string name, bool isChecked, IDictionary<string, object> htmlAttributes)
		{
			return CheckBoxHelper(htmlHelper, null, name, isChecked, htmlAttributes);
		}

		public static MvcHtmlString CheckBoxFor<TModel>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, bool>> expression)
		{
			return CheckBoxFor(htmlHelper, expression, null /* htmlAttributes */);
		}

		public static MvcHtmlString CheckBoxFor<TModel>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, bool>> expression, object htmlAttributes)
		{
			return CheckBoxFor(htmlHelper, expression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString CheckBoxFor<TModel>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, bool>> expression, IDictionary<string, object> htmlAttributes)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}

			var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
			bool? isChecked = null;
			if (metadata.Model != null)
			{
				bool modelChecked;
				if (Boolean.TryParse(metadata.Model.ToString(), out modelChecked))
				{
					isChecked = modelChecked;
				}
			}

			return CheckBoxHelper(htmlHelper, metadata, ExpressionHelper.GetExpressionText(expression), isChecked, htmlAttributes);
		}

		private static MvcHtmlString CheckBoxHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string name, bool? isChecked, IDictionary<string, object> htmlAttributes)
		{
			RouteValueDictionary attributes = ToRouteValueDictionary(htmlAttributes);

			bool explicitValue = isChecked.HasValue;
			if (explicitValue)
			{
				attributes.Remove("checked");    // Explicit value must override dictionary
			}

			return InputHelper(htmlHelper, InputType.CheckBox, metadata, name, "true", !explicitValue /* useViewData */, isChecked ?? false, true /* setId */, false /* isExplicitValue */, attributes);
		}

		// Hidden

		public static MvcHtmlString Hidden(this HtmlHelper htmlHelper, string name)
		{
			return Hidden(htmlHelper, name, null /* value */, null /* htmlAttributes */);
		}

		public static MvcHtmlString Hidden(this HtmlHelper htmlHelper, string name, object value)
		{
			return Hidden(htmlHelper, name, value, null /* hmtlAttributes */);
		}

		public static MvcHtmlString Hidden(this HtmlHelper htmlHelper, string name, object value, object htmlAttributes)
		{
			return Hidden(htmlHelper, name, value, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString Hidden(this HtmlHelper htmlHelper, string name, object value, IDictionary<string, object> htmlAttributes)
		{
			return HiddenHelper(htmlHelper,
								null,
								value,
								value == null /* useViewData */,
								name,
								htmlAttributes);
		}

		public static MvcHtmlString HiddenFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
		{
			return HiddenFor(htmlHelper, expression, (IDictionary<string, object>)null);
		}

		public static MvcHtmlString HiddenFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
		{
			return HiddenFor(htmlHelper, expression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString HiddenFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
		{
			ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
			return HiddenHelper(htmlHelper,
								metadata,
								metadata.Model,
								false,
								ExpressionHelper.GetExpressionText(expression),
								htmlAttributes);
		}

		private static MvcHtmlString HiddenHelper(HtmlHelper htmlHelper, ModelMetadata metadata, object value, bool useViewData, string expression, IDictionary<string, object> htmlAttributes)
		{
			byte[] byteArrayValue = value as byte[];
			if (byteArrayValue != null)
			{
				value = Convert.ToBase64String(byteArrayValue);
			}

			return InputHelper(htmlHelper, InputType.Hidden, metadata, expression, value, useViewData, false /* isChecked */, true /* setId */, true /* isExplicitValue */, htmlAttributes);
		}

		// Password

		public static MvcHtmlString Password(this HtmlHelper htmlHelper, string name)
		{
			return Password(htmlHelper, name, null /* value */);
		}

		public static MvcHtmlString Password(this HtmlHelper htmlHelper, string name, object value)
		{
			return Password(htmlHelper, name, value, null /* htmlAttributes */);
		}

		public static MvcHtmlString Password(this HtmlHelper htmlHelper, string name, object value, object htmlAttributes)
		{
			return Password(htmlHelper, name, value, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString Password(this HtmlHelper htmlHelper, string name, object value, IDictionary<string, object> htmlAttributes)
		{
			return PasswordHelper(htmlHelper, null /* metadata */, name, value, htmlAttributes);
		}

		public static MvcHtmlString PasswordFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
		{
			return PasswordFor(htmlHelper, expression, null /* htmlAttributes */);
		}

		public static MvcHtmlString PasswordFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
		{
			return PasswordFor(htmlHelper, expression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString PasswordFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}

			return PasswordHelper(htmlHelper,
								  ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData),
								  ExpressionHelper.GetExpressionText(expression),
								  null /* value */,
								  htmlAttributes);
		}

		private static MvcHtmlString PasswordHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string name, object value, IDictionary<string, object> htmlAttributes)
		{
			return InputHelper(htmlHelper, InputType.Password, metadata, name, value, false /* useViewData */, false /* isChecked */, true /* setId */, true /* isExplicitValue */, htmlAttributes);
		}

		// RadioButton

		public static MvcHtmlString RadioButton(this HtmlHelper htmlHelper, string name, object value)
		{
			return RadioButton(htmlHelper, name, value, (object)null /* htmlAttributes */);
		}

		public static MvcHtmlString RadioButton(this HtmlHelper htmlHelper, string name, object value, object htmlAttributes)
		{
			return RadioButton(htmlHelper, name, value, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString RadioButton(this HtmlHelper htmlHelper, string name, object value, IDictionary<string, object> htmlAttributes)
		{
			// Determine whether or not to render the checked attribute based on the contents of ViewData.
			string valueString = Convert.ToString(value, CultureInfo.CurrentCulture);
			bool isChecked = (!String.IsNullOrEmpty(name)) && (String.Equals(htmlHelper.EvalString(name), valueString, StringComparison.OrdinalIgnoreCase));
			// checked attributes is implicit, so we need to ensure that the dictionary takes precedence.
			RouteValueDictionary attributes = ToRouteValueDictionary(htmlAttributes);
			if (attributes.ContainsKey("checked"))
			{
				return InputHelper(htmlHelper, InputType.Radio, null, name, value, false, false, true, true /* isExplicitValue */, attributes);
			}

			return RadioButton(htmlHelper, name, value, isChecked, htmlAttributes);
		}

		public static MvcHtmlString RadioButton(this HtmlHelper htmlHelper, string name, object value, bool isChecked)
		{
			return RadioButton(htmlHelper, name, value, isChecked, (object)null /* htmlAttributes */);
		}

		public static MvcHtmlString RadioButton(this HtmlHelper htmlHelper, string name, object value, bool isChecked, object htmlAttributes)
		{
			return RadioButton(htmlHelper, name, value, isChecked, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString RadioButton(this HtmlHelper htmlHelper, string name, object value, bool isChecked, IDictionary<string, object> htmlAttributes)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			// checked attribute is an explicit parameter so it takes precedence.
			RouteValueDictionary attributes = ToRouteValueDictionary(htmlAttributes);
			attributes.Remove("checked");
			return InputHelper(htmlHelper, InputType.Radio, null, name, value, false, isChecked, true, true /* isExplicitValue */, attributes);
		}

		public static MvcHtmlString RadioButtonFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object value)
		{
			return RadioButtonFor(htmlHelper, expression, value, null /* htmlAttributes */);
		}

		public static MvcHtmlString RadioButtonFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object value, object htmlAttributes)
		{
			return RadioButtonFor(htmlHelper, expression, value, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString RadioButtonFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object value, IDictionary<string, object> htmlAttributes)
		{
			ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
			return RadioButtonHelper(htmlHelper,
									 metadata,
									 metadata.Model,
									 ExpressionHelper.GetExpressionText(expression),
									 value,
									 null /* isChecked */,
									 htmlAttributes);
		}

		private static MvcHtmlString RadioButtonHelper(HtmlHelper htmlHelper, ModelMetadata metadata, object model, string name, object value, bool? isChecked, IDictionary<string, object> htmlAttributes)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			var attributes = ToRouteValueDictionary(htmlAttributes);

			var explicitValue = isChecked.HasValue;
			if (explicitValue)
			{
				attributes.Remove("checked");    // Explicit value must override dictionary
			}
			else
			{
				var valueString = Convert.ToString(value, CultureInfo.CurrentCulture);
				isChecked = model != null &&
							!String.IsNullOrEmpty(name) &&
							String.Equals(model.ToString(), valueString, StringComparison.OrdinalIgnoreCase);
			}

			return InputHelper(htmlHelper, InputType.Radio, metadata, name, value, false /* useViewData */, isChecked ?? false, true /* setId */, true /* isExplicitValue */, attributes);
		}

		// TextBox

		public static MvcHtmlString TextBox(this HtmlHelper htmlHelper, string name)
		{
			return TextBox(htmlHelper, name, null /* value */);
		}

		public static MvcHtmlString TextBox(this HtmlHelper htmlHelper, string name, object value)
		{
			return TextBox(htmlHelper, name, value, (object)null /* htmlAttributes */);
		}

		public static MvcHtmlString TextBox(this HtmlHelper htmlHelper, string name, object value, object htmlAttributes)
		{
			return TextBox(htmlHelper, name, value, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString TextBox(this HtmlHelper htmlHelper, string name, object value, IDictionary<string, object> htmlAttributes)
		{
			return InputHelper(htmlHelper, InputType.Text, null, name, value, (value == null) /* useViewData */, false /* isChecked */, true /* setId */, true /* isExplicitValue */, htmlAttributes);
		}

		public static MvcHtmlString TextBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
		{
			return htmlHelper.TextBoxFor(expression, (IDictionary<string, object>)null);
		}

		public static MvcHtmlString TextBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
		{
			return htmlHelper.TextBoxFor(expression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}

		public static MvcHtmlString TextBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
		{
			var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
			return TextBoxHelper(htmlHelper,
								 metadata,
								 metadata != null ? metadata.Model : null,
								 ExpressionHelper.GetExpressionText(expression),
								 htmlAttributes);
		}

		private static MvcHtmlString TextBoxHelper(this HtmlHelper htmlHelper, ModelMetadata metadata, object model, string expression, IDictionary<string, object> htmlAttributes)
		{
			return InputHelper(htmlHelper, InputType.Text, metadata, expression, model, false /* useViewData */, false /* isChecked */, true /* setId */, true /* isExplicitValue */, htmlAttributes);
		}

		// Helper methods

		private static MvcHtmlString InputHelper(HtmlHelper htmlHelper, InputType inputType, ModelMetadata metadata, string name, object value, bool useViewData, bool isChecked, bool setId, bool isExplicitValue, IDictionary<string, object> htmlAttributes)
		{
			var fullName = name;
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "name");
			}

			var tagBuilder = new TagBuilder("input");
			tagBuilder.MergeAttributes(htmlAttributes);
			tagBuilder.MergeAttribute("type", HtmlHelper.GetInputTypeString(inputType));
			tagBuilder.MergeAttribute("name", fullName, true);

			string valueParameter = Convert.ToString(value, CultureInfo.CurrentCulture);
			bool usedModelState = false;

			switch (inputType)
			{
				case InputType.CheckBox:
					var modelStateWasChecked = htmlHelper.GetModelStateValue(fullName, typeof(bool)) as bool?;
					if (modelStateWasChecked.HasValue)
					{
						isChecked = modelStateWasChecked.Value;
						usedModelState = true;
					}
					goto case InputType.Radio;
				case InputType.Radio:
					if (!usedModelState)
					{
						var modelStateValue = htmlHelper.GetModelStateValue(fullName, typeof(string)) as string;
						if (modelStateValue != null)
						{
							isChecked = String.Equals(modelStateValue, valueParameter, StringComparison.Ordinal);
							usedModelState = true;
						}
					}
					if (!usedModelState && useViewData)
					{
						isChecked = htmlHelper.EvalBoolean(fullName);
					}
					if (isChecked)
					{
						tagBuilder.MergeAttribute("checked", "checked");
					}
					tagBuilder.MergeAttribute("value", valueParameter, isExplicitValue);
					break;
				case InputType.Password:
					if (value != null)
					{
						tagBuilder.MergeAttribute("value", valueParameter, isExplicitValue);
					}
					break;
				default:
					var attemptedValue = (string)htmlHelper.GetModelStateValue(fullName, typeof(string));
					tagBuilder.MergeAttribute("value", attemptedValue ?? ((useViewData) ? htmlHelper.EvalString(fullName) : valueParameter), isExplicitValue);
					break;
			}

			if (setId) {
				tagBuilder.GenerateId(fullName);
			}

			return tagBuilder.ToMvcHtmlString(TagRenderMode.SelfClosing);
		}

		private static RouteValueDictionary ToRouteValueDictionary(IDictionary<string, object> dictionary)
		{
			return dictionary == null ? new RouteValueDictionary() : new RouteValueDictionary(dictionary);
		}
	}
}
