using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using ServiceStack.Markdown;
using ServiceStack.Text;

namespace ServiceStack.Html
{
	public class ViewDataDictionary : IDictionary<string, object>
	{
		private readonly Dictionary<string, object> innerDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		private object model;
		private ModelMetadata modelMetadata;
		private ModelStateDictionary modelState;
        private TemplateInfo _templateMetadata;

		public ViewDataDictionary() : this((object)null) { }

		public ViewDataDictionary(object model)
		{
			Model = model;
		}

		public ViewDataDictionary(ViewDataDictionary dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}

			foreach (var entry in dictionary)
			{
				innerDictionary.Add(entry.Key, entry.Value);
			}
			foreach (var entry in dictionary.ModelState)
			{
				ModelState.Add(entry.Key, entry.Value);
			}

			Model = dictionary.Model;

			// PERF: Don't unnecessarily instantiate the model metadata
			modelMetadata = dictionary.modelMetadata;
		}

		public int Count
		{
			get
			{
				return innerDictionary.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return ((IDictionary<string, object>)innerDictionary).IsReadOnly;
			}
		}

		public ICollection<string> Keys
		{
			get
			{
				return innerDictionary.Keys;
			}
		}

		public object Model
		{
			get
			{
				return model;
			}
			set
			{
				modelMetadata = null;
				SetModel(value);
			}
		}

		public virtual ModelMetadata ModelMetadata
		{
			get
			{
				if (modelMetadata == null && model != null)
				{
					modelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => model, model.GetType());
				}
				return modelMetadata;
			}
			set
			{
				modelMetadata = value;
			}
		}

		public ModelStateDictionary ModelState
		{
			get
			{
				return modelState ?? (modelState = new ModelStateDictionary());
			}
		}

	    private bool hasPopulatedModelState;
		public virtual void PopulateModelState()
		{
			if (model == null) return;
            if (hasPopulatedModelState) return;

		    lock (this)
		    {
                if (hasPopulatedModelState) return;
                
                //Skip non-poco's, i.e. List
                modelState = new ModelStateDictionary();
                var modelType = model.GetType();
                var listType = modelType.IsGenericType
                    ? modelType.GetTypeWithGenericInterfaceOf(typeof(IList<>))
                    : null;
                if (listType != null || model.GetType().IsArray) return;

                var strModel = TypeSerializer.SerializeToString(model);
                var map = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(strModel);
                foreach (var kvp in map)
                {
                    var valueState = new ModelState {
                        Value = new ValueProviderResult(kvp.Value, kvp.Value, CultureInfo.CurrentCulture)
                    };
                    try
                    {
                        modelState.Add(kvp.Key, valueState);
                    }
                    catch (Exception ex)
                    {
                        ex.Message.PrintDump();
                        throw;
                    }
                }
		        hasPopulatedModelState = true;
		    }
		}

		public object this[string key]
		{
			get
			{
				object value;
				innerDictionary.TryGetValue(key, out value);
				return value;
			}
			set
			{
				innerDictionary[key] = value;
			}
		}

        public TemplateInfo TemplateInfo
        {
            get
            {
                if (_templateMetadata == null) {
                    _templateMetadata = new TemplateInfo();
                }
                return _templateMetadata;
            }
            set
            {
                _templateMetadata = value;
            }
        }

		public ICollection<object> Values
		{
			get
			{
				return innerDictionary.Values;
			}
		}

		public void Add(KeyValuePair<string, object> item)
		{
			((IDictionary<string, object>)innerDictionary).Add(item);
		}

		public void Add(string key, object value)
		{
			innerDictionary.Add(key, value);
		}

		public void Clear()
		{
			innerDictionary.Clear();
		}

		public bool Contains(KeyValuePair<string, object> item)
		{
			return ((IDictionary<string, object>)innerDictionary).Contains(item);
		}

		public bool ContainsKey(string key)
		{
			return innerDictionary.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			((IDictionary<string, object>)innerDictionary).CopyTo(array, arrayIndex);
		}

		public object Eval(string expression)
		{
			ViewDataInfo info = GetViewDataInfo(expression);
			return (info != null) ? info.Value : null;
		}

		public string Eval(string expression, string format)
		{
			object value = Eval(expression);

			if (value == null)
			{
				return String.Empty;
			}

			if (String.IsNullOrEmpty(format))
			{
				return Convert.ToString(value, CultureInfo.CurrentCulture);
			}
			else
			{
				return String.Format(CultureInfo.CurrentCulture, format, value);
			}
		}

        internal static string FormatValueInternal(object value, string format)
        {
            if (value == null) {
                return String.Empty;
            }

            if (String.IsNullOrEmpty(format)) {
                return Convert.ToString(value, CultureInfo.CurrentCulture);
            } else {
                return String.Format(CultureInfo.CurrentCulture, format, value);
            }
        }

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return innerDictionary.GetEnumerator();
		}

		public ViewDataInfo GetViewDataInfo(string expression)
		{
			if (String.IsNullOrEmpty(expression))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "expression");
			}

			return ViewDataEvaluator.Eval(this, expression);
		}

		public bool Remove(KeyValuePair<string, object> item)
		{
			return ((IDictionary<string, object>)innerDictionary).Remove(item);
		}

		public bool Remove(string key)
		{
			return innerDictionary.Remove(key);
		}

		// This method will execute before the derived type's instance constructor executes. Derived types must
		// be aware of this and should plan accordingly. For example, the logic in SetModel() should be simple
		// enough so as not to depend on the "this" pointer referencing a fully constructed object.
		protected virtual void SetModel(object value)
		{
			model = value;
		}

		public bool TryGetValue(string key, out object value)
		{
			return innerDictionary.TryGetValue(key, out value);
		}

		internal static class ViewDataEvaluator
		{
			public static ViewDataInfo Eval(ViewDataDictionary vdd, string expression)
			{
				//Given an expression "foo.bar.baz" we look up the following (pseudocode):
				//  this["foo.bar.baz.quux"]
				//  this["foo.bar.baz"]["quux"]
				//  this["foo.bar"]["baz.quux]
				//  this["foo.bar"]["baz"]["quux"]
				//  this["foo"]["bar.baz.quux"]
				//  this["foo"]["bar.baz"]["quux"]
				//  this["foo"]["bar"]["baz.quux"]
				//  this["foo"]["bar"]["baz"]["quux"]

				ViewDataInfo evaluated = EvalComplexExpression(vdd, expression);
				return evaluated;
			}

			private static ViewDataInfo EvalComplexExpression(object indexableObject, string expression)
			{
				foreach (ExpressionPair expressionPair in GetRightToLeftExpressions(expression))
				{
					string subExpression = expressionPair.Left;
					string postExpression = expressionPair.Right;

					ViewDataInfo subTargetInfo = GetPropertyValue(indexableObject, subExpression);
					if (subTargetInfo != null)
					{
						if (String.IsNullOrEmpty(postExpression))
						{
							return subTargetInfo;
						}

						if (subTargetInfo.Value != null)
						{
							ViewDataInfo potential = EvalComplexExpression(subTargetInfo.Value, postExpression);
							if (potential != null)
							{
								return potential;
							}
						}
					}
				}
				return null;
			}

			private static IEnumerable<ExpressionPair> GetRightToLeftExpressions(string expression)
			{
				// Produces an enumeration of all the combinations of complex property names
				// given a complex expression. See the list above for an example of the result
				// of the enumeration.

				yield return new ExpressionPair(expression, String.Empty);

				int lastDot = expression.LastIndexOf('.');

				string subExpression = expression;
				string postExpression = string.Empty;

				while (lastDot > -1)
				{
					subExpression = expression.Substring(0, lastDot);
					postExpression = expression.Substring(lastDot + 1);
					yield return new ExpressionPair(subExpression, postExpression);

					lastDot = subExpression.LastIndexOf('.');
				}
			}

			private static ViewDataInfo GetIndexedPropertyValue(object indexableObject, string key)
			{
				var dict = indexableObject as IDictionary<string, object>;
				object value = null;
				bool success = false;

				if (dict != null)
				{
					success = dict.TryGetValue(key, out value);
				}
				else
				{
					var tgvDel = TypeHelpers.CreateTryGetValueDelegate(indexableObject.GetType());
					if (tgvDel != null)
					{
						success = tgvDel(indexableObject, key, out value);
					}
				}

				if (success)
				{
					return new ViewDataInfo() {
						Container = indexableObject,
						Value = value
					};
				}

				return null;
			}

			private static ViewDataInfo GetPropertyValue(object container, string propertyName)
			{
				// This method handles one "segment" of a complex property expression

				// First, we try to evaluate the property based on its indexer
				var value = GetIndexedPropertyValue(container, propertyName);
				if (value != null)
				{
					return value;
				}

				// If the indexer didn't return anything useful, continue...

				// If the container is a ViewDataDictionary then treat its Model property
				// as the container instead of the ViewDataDictionary itself.
				var vdd = container as ViewDataDictionary;
				if (vdd != null)
				{
					container = vdd.Model;
				}

				// If the container is null, we're out of options
				if (container == null)
				{
					return null;
				}

				// Second, we try to use PropertyDescriptors and treat the expression as a property name
				var descriptor = TypeDescriptor.GetProperties(container).Find(propertyName, true);
				if (descriptor == null)
				{
					return null;
				}

				return new ViewDataInfo(() => descriptor.GetValue(container)) {
					Container = container,
					PropertyDescriptor = descriptor
				};
			}

			private struct ExpressionPair
			{
				public readonly string Left;
				public readonly string Right;

				public ExpressionPair(string left, string right)
				{
					Left = left;
					Right = right;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)innerDictionary).GetEnumerator();
		}

        public MvcHtmlString AsRawJson()
        {
            return MvcHtmlString.Create(Model.ToJson());
        }

        public MvcHtmlString AsRaw()
        {
            return MvcHtmlString.Create((Model ?? "").ToString());
        }

	}
}
