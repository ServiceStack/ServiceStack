﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;

namespace ServiceStack.Html
{
    public static class SelectExtensions
    {
        // DropDownList

        public static MvcHtmlString DropDownList(this HtmlHelper htmlHelper, string name)
        {
            return DropDownList(htmlHelper, name, null /* selectList */, null /* optionLabel */, null /* htmlAttributes */);
        }

        public static MvcHtmlString DropDownList(this HtmlHelper htmlHelper, string name, string optionLabel)
        {
            return DropDownList(htmlHelper, name, null /* selectList */, optionLabel, null /* htmlAttributes */);
        }

        public static MvcHtmlString DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList)
        {
            return DropDownList(htmlHelper, name, selectList, null /* optionLabel */, null /* htmlAttributes */);
        }

        public static MvcHtmlString DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            return DropDownList(htmlHelper, name, selectList, null /* optionLabel */, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, IDictionary<string, object> htmlAttributes)
        {
            return DropDownList(htmlHelper, name, selectList, null /* optionLabel */, htmlAttributes);
        }

        public static MvcHtmlString DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, string optionLabel)
        {
            return DropDownList(htmlHelper, name, selectList, optionLabel, null /* htmlAttributes */);
        }

        public static MvcHtmlString DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes)
        {
            return DropDownList(htmlHelper, name, selectList, optionLabel, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, string optionLabel, IDictionary<string, object> htmlAttributes)
        {
            return DropDownListHelper(htmlHelper, metadata: null, expression: name, selectList: selectList, optionLabel: optionLabel, htmlAttributes: htmlAttributes);
        }

        public static MvcHtmlString DropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList)
        {
            return DropDownListFor(htmlHelper, expression, selectList, null /* optionLabel */, null /* htmlAttributes */);
        }

        public static MvcHtmlString DropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            return DropDownListFor(htmlHelper, expression, selectList, null /* optionLabel */, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString DropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, IDictionary<string, object> htmlAttributes)
        {
            return DropDownListFor(htmlHelper, expression, selectList, null /* optionLabel */, htmlAttributes);
        }

        public static MvcHtmlString DropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, string optionLabel)
        {
            return DropDownListFor(htmlHelper, expression, selectList, optionLabel, null /* htmlAttributes */);
        }

        public static MvcHtmlString DropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes)
        {
            return DropDownListFor(htmlHelper, expression, selectList, optionLabel, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString DropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, string optionLabel, IDictionary<string, object> htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);

            return DropDownListHelper(htmlHelper, metadata, ExpressionHelper.GetExpressionText(expression), selectList, optionLabel, htmlAttributes);
        }

        private static MvcHtmlString DropDownListHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string expression, IEnumerable<SelectListItem> selectList, string optionLabel, IDictionary<string, object> htmlAttributes)
        {
            return SelectInternal(htmlHelper, metadata, optionLabel, expression, selectList, allowMultiple: false, htmlAttributes: htmlAttributes);
        }

        // ListBox

        public static MvcHtmlString ListBox(this HtmlHelper htmlHelper, string name)
        {
            return ListBox(htmlHelper, name, null /* selectList */, null /* htmlAttributes */);
        }

        public static MvcHtmlString ListBox(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList)
        {
            return ListBox(htmlHelper, name, selectList, (IDictionary<string, object>)null);
        }

        public static MvcHtmlString ListBox(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            return ListBox(htmlHelper, name, selectList, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ListBox(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, IDictionary<string, object> htmlAttributes)
        {
            return ListBoxHelper(htmlHelper, metadata: null, name: name, selectList: selectList, htmlAttributes: htmlAttributes);
        }

        public static MvcHtmlString ListBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList)
        {
            return ListBoxFor(htmlHelper, expression, selectList, null /* htmlAttributes */);
        }

        public static MvcHtmlString ListBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            return ListBoxFor(htmlHelper, expression, selectList, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ListBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, IDictionary<string, object> htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);

            return ListBoxHelper(htmlHelper,
                                 metadata,
                                 ExpressionHelper.GetExpressionText(expression),
                                 selectList,
                                 htmlAttributes);
        }

        private static MvcHtmlString ListBoxHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string name, IEnumerable<SelectListItem> selectList, IDictionary<string, object> htmlAttributes)
        {
            return SelectInternal(htmlHelper, metadata, optionLabel: null, name: name, selectList: selectList, allowMultiple: true, htmlAttributes: htmlAttributes);
        }

        // Helper methods

        private static IEnumerable<SelectListItem> GetSelectData(this HtmlHelper htmlHelper, string name)
        {
            object o = null;
            if (htmlHelper.ViewData != null)
            {
                o = htmlHelper.ViewData.Eval(name);
            }
            if (o == null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        MvcResources.HtmlHelper_MissingSelectData,
                        name,
                        "IEnumerable<SelectListItem>"));
            }
            IEnumerable<SelectListItem> selectList = o as IEnumerable<SelectListItem>;
            if (selectList == null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        MvcResources.HtmlHelper_WrongSelectDataType,
                        name,
                        o.GetType().FullName,
                        "IEnumerable<SelectListItem>"));
            }
            return selectList;
        }

        internal static string ListItemToOption(SelectListItem item)
        {
            TagBuilder builder = new TagBuilder("option")
            {
                InnerHtml = HttpUtility.HtmlEncode(item.Text)
            };
            if (item.Value != null)
            {
                builder.Attributes["value"] = item.Value;
            }
            if (item.Selected)
            {
                builder.Attributes["selected"] = "selected";
            }
            return builder.ToString(TagRenderMode.Normal);
        }

        private static IEnumerable<SelectListItem> GetSelectListWithDefaultValue(IEnumerable<SelectListItem> selectList, object defaultValue, bool allowMultiple)
        {
            IEnumerable defaultValues;

            if (allowMultiple)
            {
                defaultValues = defaultValue as IEnumerable;
                if (defaultValues == null || defaultValues is string)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            CultureInfo.CurrentCulture,
                            MvcResources.HtmlHelper_SelectExpressionNotEnumerable,
                            "expression"));
                }
            }
            else
            {
                defaultValues = new[] { defaultValue };
            }

            IEnumerable<string> values = from object value in defaultValues
                                         select Convert.ToString(value, CultureInfo.CurrentCulture);
            HashSet<string> selectedValues = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
            List<SelectListItem> newSelectList = new List<SelectListItem>();

            foreach (SelectListItem item in selectList)
            {
                item.Selected = (item.Value != null) ? selectedValues.Contains(item.Value) : selectedValues.Contains(item.Text);
                newSelectList.Add(item);
            }
            return newSelectList;
        }

        private static MvcHtmlString SelectInternal(this HtmlHelper htmlHelper, ModelMetadata metadata, string optionLabel, string name, IEnumerable<SelectListItem> selectList, bool allowMultiple, IDictionary<string, object> htmlAttributes)
        {
            //string fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            string fullName = name;
            if (String.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(MvcResources.Common_NullOrEmpty, "name");
            }

            bool usedViewData = false;

            // If we got a null selectList, try to use ViewData to get the list of items.
            if (selectList == null)
            {
                selectList = htmlHelper.GetSelectData(name);
                usedViewData = true;
            }

            object defaultValue = (allowMultiple) ? htmlHelper.GetModelStateValue(fullName, typeof(string[])) : htmlHelper.GetModelStateValue(fullName, typeof(string));

            // If we haven't already used ViewData to get the entire list of items then we need to
            // use the ViewData-supplied value before using the parameter-supplied value.
            if (!usedViewData && defaultValue == null && !String.IsNullOrEmpty(name))
            {
                defaultValue = htmlHelper.ViewData.Eval(name);
            }

            if (defaultValue != null)
            {
                selectList = GetSelectListWithDefaultValue(selectList, defaultValue, allowMultiple);
            }

            // Convert each ListItem to an <option> tag
            StringBuilder listItemBuilder = new StringBuilder();

            // Make optionLabel the first item that gets rendered.
            if (optionLabel != null)
            {
                listItemBuilder.AppendLine(ListItemToOption(new SelectListItem() { Text = optionLabel, Value = String.Empty, Selected = false }));
            }

            foreach (SelectListItem item in selectList)
            {
                listItemBuilder.AppendLine(ListItemToOption(item));
            }

            TagBuilder tagBuilder = new TagBuilder("select")
            {
                InnerHtml = listItemBuilder.ToString()
            };
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("name", fullName, true /* replaceExisting */);
            tagBuilder.GenerateId(fullName);
            if (allowMultiple)
            {
                tagBuilder.MergeAttribute("multiple", "multiple");
            }

            // If there are any errors for a named field, we add the css attribute.
            ModelState modelState;
            if (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out modelState))
            {
                if (modelState.Errors.Count > 0)
                {
                    tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
                }
            }

            tagBuilder.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, metadata));

            return tagBuilder.ToMvcHtmlString(TagRenderMode.Normal);
        }
    }
}
