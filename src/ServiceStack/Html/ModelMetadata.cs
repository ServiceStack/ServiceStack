﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Markdown;

namespace ServiceStack.Html
{
	public class ModelMetadata
	{
		public const int DefaultOrder = 10000;

		// Explicit backing store for the things we want initialized by default, so don't have to call
		// the protected virtual setters of an auto-generated property
		private Dictionary<string, object> _additionalValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		private readonly Type _containerType;
		private bool _convertEmptyStringToNull = true;
		private bool _isRequired;
		private object _model;
		private Func<object> _modelAccessor;
		private readonly Type _modelType;
		private int _order = DefaultOrder;
		private IEnumerable<ModelMetadata> _properties;
		private readonly string _propertyName;
		private Type _realModelType;
		private bool _requestValidationEnabled = true;
		private bool _showForDisplay = true;
		private bool _showForEdit = true;
		private string _simpleDisplayText;

		public ModelMetadata(ModelMetadataProvider provider, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}
			if (modelType == null)
			{
				throw new ArgumentNullException("modelType");
			}

			Provider = provider;

			_containerType = containerType;
			_isRequired = !TypeHelpers.TypeAllowsNullValue(modelType);
			_modelAccessor = modelAccessor;
			_modelType = modelType;
			_propertyName = propertyName;
		}

		public virtual Dictionary<string, object> AdditionalValues
		{
			get
			{
				return _additionalValues;
			}
		}

		public Type ContainerType
		{
			get
			{
				return _containerType;
			}
		}

		public virtual bool ConvertEmptyStringToNull
		{
			get
			{
				return _convertEmptyStringToNull;
			}
			set
			{
				_convertEmptyStringToNull = value;
			}
		}

		public virtual string DataTypeName
		{
			get;
			set;
		}

		public virtual string Description
		{
			get;
			set;
		}

		public virtual string DisplayFormatString
		{
			get;
			set;
		}

		public virtual string DisplayName
		{
			get;
			set;
		}

		public virtual string EditFormatString
		{
			get;
			set;
		}

		public virtual bool HideSurroundingHtml
		{
			get;
			set;
		}

		public virtual bool IsComplexType
		{
			get
			{
				return !(TypeDescriptor.GetConverter(ModelType).CanConvertFrom(typeof(string)));
			}
		}

		public bool IsNullableValueType
		{
			get
			{
				return TypeHelpers.IsNullableValueType(ModelType);
			}
		}

		public virtual bool IsReadOnly
		{
			get;
			set;
		}

		public virtual bool IsRequired
		{
			get
			{
				return _isRequired;
			}
			set
			{
				_isRequired = value;
			}
		}

		public object Model
		{
			get
			{
				if (_modelAccessor != null)
				{
					_model = _modelAccessor();
					_modelAccessor = null;
				}
				return _model;
			}
			set
			{
				_model = value;
				_modelAccessor = null;
				_properties = null;
				_realModelType = null;
			}
		}

		public Type ModelType
		{
			get
			{
				return _modelType;
			}
		}

		public virtual string NullDisplayText
		{
			get;
			set;
		}

		public virtual int Order
		{
			get
			{
				return _order;
			}
			set
			{
				_order = value;
			}
		}

		public virtual IEnumerable<ModelMetadata> Properties
		{
			get
			{
				if (_properties == null)
				{
					_properties = Provider.GetMetadataForProperties(Model, RealModelType).OrderBy(m => m.Order);
				}
				return _properties;
			}
		}

		public string PropertyName
		{
			get
			{
				return _propertyName;
			}
		}

		protected ModelMetadataProvider Provider
		{
			get;
			set;
		}

		internal Type RealModelType
		{
			get
			{
				if (_realModelType == null)
				{
					_realModelType = ModelType;

					// Don't call GetType() if the model is Nullable<T>, because it will
					// turn Nullable<T> into T for non-null values
					if (Model != null && !TypeHelpers.IsNullableValueType(ModelType))
					{
						_realModelType = Model.GetType();
					}
				}

				return _realModelType;
			}
		}

		public virtual bool RequestValidationEnabled
		{
			get
			{
				return _requestValidationEnabled;
			}
			set
			{
				_requestValidationEnabled = value;
			}
		}

		public virtual string ShortDisplayName
		{
			get;
			set;
		}

		public virtual bool ShowForDisplay
		{
			get
			{
				return _showForDisplay;
			}
			set
			{
				_showForDisplay = value;
			}
		}

		public virtual bool ShowForEdit
		{
			get
			{
				return _showForEdit;
			}
			set
			{
				_showForEdit = value;
			}
		}

		public virtual string SimpleDisplayText
		{
			get
			{
				if (_simpleDisplayText == null)
				{
					_simpleDisplayText = GetSimpleDisplayText();
				}
				return _simpleDisplayText;
			}
			set
			{
				_simpleDisplayText = value;
			}
		}

		public virtual string TemplateHint
		{
			get;
			set;
		}

		public virtual string Watermark
		{
			get;
			set;
		}

		public static ModelMetadata FromLambdaExpression<TParameter, TValue>(
			Expression<Func<TParameter, TValue>> expression, ViewDataDictionary<TParameter> viewData)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			if (viewData == null)
			{
				throw new ArgumentNullException("viewData");
			}

			string propertyName = null;
			Type containerType = null;
			bool legalExpression = false;

			// Need to verify the expression is valid; it needs to at least end in something
			// that we can convert to a meaningful string for model binding purposes

			switch (expression.Body.NodeType)
			{
				// ArrayIndex always means a single-dimensional indexer; multi-dimensional indexer is a method call to Get()
				case ExpressionType.ArrayIndex:
					legalExpression = true;
					break;

				// Only legal method call is a single argument indexer/DefaultMember call
				case ExpressionType.Call:
					legalExpression = ExpressionHelper.IsSingleArgumentIndexer(expression.Body);
					break;

				// Property/field access is always legal
				case ExpressionType.MemberAccess:
					var memberExpression = (MemberExpression)expression.Body;
					propertyName = memberExpression.Member is PropertyInfo ? memberExpression.Member.Name : null;
					containerType = memberExpression.Expression.Type;
					legalExpression = true;
					break;

				// Parameter expression means "model => model", so we delegate to FromModel
				case ExpressionType.Parameter:
					return FromModel(viewData);
			}

			if (!legalExpression)
			{
				throw new InvalidOperationException(MvcResources.TemplateHelpers_TemplateLimitations);
			}

			TParameter container = viewData.Model;
			Func<object> modelAccessor = () => {
				try
				{
					return CachedExpressionCompiler.Process(expression)(container);
				}
				catch (NullReferenceException)
				{
					return null;
				}
			};

			return GetMetadataFromProvider(modelAccessor, typeof(TValue), propertyName, containerType);
		}

		private static ModelMetadata FromModel(ViewDataDictionary viewData)
		{
			return viewData.ModelMetadata ?? GetMetadataFromProvider(null, typeof(string), null, null);
		}

		public static ModelMetadata FromStringExpression(string expression, ViewDataDictionary viewData)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			if (viewData == null)
			{
				throw new ArgumentNullException("viewData");
			}
			if (expression.Length == 0)
			{    // Empty string really means "model metadata for the current model"
				return FromModel(viewData);
			}

			var vdi = viewData.GetViewDataInfo(expression);
			Type containerType = null;
			Type modelType = null;
			Func<object> modelAccessor = null;
			string propertyName = null;

			if (vdi != null)
			{
				if (vdi.Container != null)
				{
					containerType = vdi.Container.GetType();
				}

				modelAccessor = () => vdi.Value;

				if (vdi.PropertyDescriptor != null)
				{
					propertyName = vdi.PropertyDescriptor.Name;
					modelType = vdi.PropertyDescriptor.PropertyType;
				}
				else if (vdi.Value != null)
				{  // We only need to delay accessing properties (for LINQ to SQL)
					modelType = vdi.Value.GetType();
				}
			}
			//  Try getting a property from ModelMetadata if we couldn't find an answer in ViewData
			else if (viewData.ModelMetadata != null)
			{
				ModelMetadata propertyMetadata = viewData.ModelMetadata.Properties.Where(p => p.PropertyName == expression).FirstOrDefault();
				if (propertyMetadata != null)
				{
					return propertyMetadata;
				}
			}


			return GetMetadataFromProvider(modelAccessor, modelType ?? typeof(string), propertyName, containerType);
		}

		public string GetDisplayName()
		{
			return DisplayName ?? PropertyName ?? ModelType.GetOperationName();
		}

		private static ModelMetadata GetMetadataFromProvider(Func<object> modelAccessor, Type modelType, string propertyName, Type containerType)
		{
			if (containerType != null && !String.IsNullOrEmpty(propertyName))
			{
				return ModelMetadataProviders.Current.GetMetadataForProperty(modelAccessor, containerType, propertyName);
			}
			return ModelMetadataProviders.Current.GetMetadataForType(modelAccessor, modelType);
		}

		protected virtual string GetSimpleDisplayText()
		{
			if (Model == null)
			{
				return NullDisplayText;
			}

			string toStringResult = Convert.ToString(Model, CultureInfo.CurrentCulture);
			if (!toStringResult.Equals(Model.GetType().FullName, StringComparison.Ordinal))
			{
				return toStringResult;
			}

			ModelMetadata firstProperty = Properties.FirstOrDefault();
			if (firstProperty == null)
			{
				return String.Empty;
			}

			if (firstProperty.Model == null)
			{
				return firstProperty.NullDisplayText;
			}

			return Convert.ToString(firstProperty.Model, CultureInfo.CurrentCulture);
		}

	}
}
