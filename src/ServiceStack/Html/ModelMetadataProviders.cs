using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace ServiceStack.Html
{
	[Serializable]
	public class ModelStateDictionary : IDictionary<string, ModelState>
	{
		private readonly Dictionary<string, ModelState> innerDictionary = new Dictionary<string, ModelState>(StringComparer.OrdinalIgnoreCase);

		public ModelStateDictionary() {}

		public ModelStateDictionary(ModelStateDictionary dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}

			foreach (var entry in dictionary)
			{
				innerDictionary.Add(entry.Key, entry.Value);
			}
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
				return ((IDictionary<string, ModelState>)innerDictionary).IsReadOnly;
			}
		}

		public bool IsValid
		{
			get
			{
				return Values.All(modelState => modelState.Errors.Count == 0);
			}
		}

		public ICollection<string> Keys
		{
			get
			{
				return innerDictionary.Keys;
			}
		}

		public ModelState this[string key]
		{
			get
			{
				ModelState value;
				innerDictionary.TryGetValue(key, out value);
				return value;
			}
			set
			{
				innerDictionary[key] = value;
			}
		}

		public ICollection<ModelState> Values
		{
			get
			{
				return innerDictionary.Values;
			}
		}

		public void Add(KeyValuePair<string, ModelState> item)
		{
			((IDictionary<string, ModelState>)innerDictionary).Add(item);
		}

		public void Add(string key, ModelState value)
		{
			innerDictionary.Add(key, value);
		}

		public void AddModelError(string key, Exception exception)
		{
			GetModelStateForKey(key).Errors.Add(exception);
		}

		public void AddModelError(string key, string errorMessage)
		{
			GetModelStateForKey(key).Errors.Add(errorMessage);
		}

		public void Clear()
		{
			innerDictionary.Clear();
		}

		public bool Contains(KeyValuePair<string, ModelState> item)
		{
			return ((IDictionary<string, ModelState>)innerDictionary).Contains(item);
		}

		public bool ContainsKey(string key)
		{
			return innerDictionary.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, ModelState>[] array, int arrayIndex)
		{
			((IDictionary<string, ModelState>)innerDictionary).CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<string, ModelState>> GetEnumerator()
		{
			return innerDictionary.GetEnumerator();
		}

		private ModelState GetModelStateForKey(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			ModelState modelState;
			if (!TryGetValue(key, out modelState))
			{
				modelState = new ModelState();
				this[key] = modelState;
			}

			return modelState;
		}

		public bool IsValidField(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			// if the key is not found in the dictionary, we just say that it's valid (since there are no errors)
			return DictionaryHelpers.FindKeysWithPrefix(this, key).All(entry => entry.Value.Errors.Count == 0);
		}

		public void Merge(ModelStateDictionary dictionary)
		{
			if (dictionary == null)
			{
				return;
			}

			foreach (var entry in dictionary)
			{
				this[entry.Key] = entry.Value;
			}
		}

		public bool Remove(KeyValuePair<string, ModelState> item)
		{
			return ((IDictionary<string, ModelState>)innerDictionary).Remove(item);
		}

		public bool Remove(string key)
		{
			return innerDictionary.Remove(key);
		}

		public void SetModelValue(string key, ValueProviderResult value)
		{
			GetModelStateForKey(key).Value = value;
		}

		public bool TryGetValue(string key, out ModelState value)
		{
			return innerDictionary.TryGetValue(key, out value);
		}

		#region IEnumerable Members
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)innerDictionary).GetEnumerator();
		}
		#endregion

	}

	[Serializable]
	public class ModelState
	{
		private readonly ModelErrorCollection errors = new ModelErrorCollection();

		public ValueProviderResult Value
		{
			get;
			set;
		}

		public ModelErrorCollection Errors
		{
			get
			{
				return errors;
			}
		}
	}

	[Serializable]
	public class ModelErrorCollection : Collection<ModelError>
	{

		public void Add(Exception exception)
		{
			Add(new ModelError(exception));
		}

		public void Add(string errorMessage)
		{
			Add(new ModelError(errorMessage));
		}
	}

	[Serializable]
	public class ModelError
	{

		public ModelError(Exception exception)
			: this(exception, null /* errorMessage */)
		{
		}

		public ModelError(Exception exception, string errorMessage)
			: this(errorMessage)
		{
			if (exception == null)
			{
				throw new ArgumentNullException("exception");
			}

			Exception = exception;
		}

		public ModelError(string errorMessage)
		{
			ErrorMessage = errorMessage ?? String.Empty;
		}

		public Exception Exception
		{
			get;
			private set;
		}

		public string ErrorMessage
		{
			get;
			private set;
		}
	}

	[Serializable]
	public class ValueProviderResult
	{

		private static readonly CultureInfo _staticCulture = CultureInfo.InvariantCulture;
		private CultureInfo _instanceCulture;

		// default constructor so that subclassed types can set the properties themselves
		protected ValueProviderResult()
		{
		}

		public ValueProviderResult(object rawValue, string attemptedValue, CultureInfo culture)
		{
			RawValue = rawValue;
			AttemptedValue = attemptedValue;
			Culture = culture;
		}

		public string AttemptedValue
		{
			get;
			protected set;
		}

		public CultureInfo Culture
		{
			get
			{
				if (_instanceCulture == null)
				{
					_instanceCulture = _staticCulture;
				}
				return _instanceCulture;
			}
			protected set
			{
				_instanceCulture = value;
			}
		}

		public object RawValue
		{
			get;
			protected set;
		}

		private static object ConvertSimpleType(CultureInfo culture, object value, Type destinationType)
		{
			if (value == null || destinationType.IsInstanceOfType(value))
			{
				return value;
			}

			// if this is a user-input value but the user didn't type anything, return no value
			string valueAsString = value as string;
			if (valueAsString != null && valueAsString.Trim().Length == 0)
			{
				return null;
			}

			TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
			bool canConvertFrom = converter.CanConvertFrom(value.GetType());
			if (!canConvertFrom)
			{
				converter = TypeDescriptor.GetConverter(value.GetType());
			}
			if (!(canConvertFrom || converter.CanConvertTo(destinationType)))
			{
				string message = String.Format(CultureInfo.CurrentCulture, MvcResources.ValueProviderResult_NoConverterExists,
					value.GetType().FullName, destinationType.FullName);
				throw new InvalidOperationException(message);
			}

			try
			{
				object convertedValue = (canConvertFrom) ?
					 converter.ConvertFrom(null /* context */, culture, value) :
					 converter.ConvertTo(null /* context */, culture, value, destinationType);
				return convertedValue;
			}
			catch (Exception ex)
			{
				string message = String.Format(CultureInfo.CurrentCulture, MvcResources.ValueProviderResult_ConversionThrew,
					value.GetType().FullName, destinationType.FullName);
				throw new InvalidOperationException(message, ex);
			}
		}

		public object ConvertTo(Type type)
		{
			return ConvertTo(type, null /* culture */);
		}

		public virtual object ConvertTo(Type type, CultureInfo culture)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}

			CultureInfo cultureToUse = culture ?? Culture;
			return UnwrapPossibleArrayType(cultureToUse, RawValue, type);
		}

		private static object UnwrapPossibleArrayType(CultureInfo culture, object value, Type destinationType)
		{
			if (value == null || destinationType.IsInstanceOfType(value))
			{
				return value;
			}

			// array conversion results in four cases, as below
			Array valueAsArray = value as Array;
			if (destinationType.IsArray)
			{
				Type destinationElementType = destinationType.GetElementType();
				if (valueAsArray != null)
				{
					// case 1: both destination + source type are arrays, so convert each element
					IList converted = Array.CreateInstance(destinationElementType, valueAsArray.Length);
					for (int i = 0; i < valueAsArray.Length; i++)
					{
						converted[i] = ConvertSimpleType(culture, valueAsArray.GetValue(i), destinationElementType);
					}
					return converted;
				}
				else
				{
					// case 2: destination type is array but source is single element, so wrap element in array + convert
					object element = ConvertSimpleType(culture, value, destinationElementType);
					IList converted = Array.CreateInstance(destinationElementType, 1);
					converted[0] = element;
					return converted;
				}
			}
			else if (valueAsArray != null)
			{
				// case 3: destination type is single element but source is array, so extract first element + convert
				if (valueAsArray.Length > 0)
				{
					value = valueAsArray.GetValue(0);
					return ConvertSimpleType(culture, value, destinationType);
				}
				else
				{
					// case 3(a): source is empty array, so can't perform conversion
					return null;
				}
			}
			// case 4: both destination + source type are single elements, so convert
			return ConvertSimpleType(culture, value, destinationType);
		}
	}


	public class EmptyModelMetadataProvider : ModelMetadataProvider
	{
		protected virtual ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName)
		{
			return new ModelMetadata(this, containerType, modelAccessor, modelType, propertyName);
		}

		public override IEnumerable<ModelMetadata> GetMetadataForProperties(object container, Type containerType)
		{
			return new List<ModelMetadata>();
		}

		public override ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, Type containerType, string propertyName)
		{
			return null;
		}

		public override ModelMetadata GetMetadataForType(Func<object> modelAccessor, Type modelType)
		{
			return null;
		}
	}

	public class PocoMetadataProvider : ModelMetadataProvider
	{
		protected virtual ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName)
		{
			return new ModelMetadata(this, containerType, modelAccessor, modelType, propertyName);
		}

		public override IEnumerable<ModelMetadata> GetMetadataForProperties(object container, Type containerType)
		{
			return new List<ModelMetadata>();
		}

		public override ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, Type containerType, string propertyName)
		{
			var modelType = containerType; //FIX?
			return new ModelMetadata(this, containerType, modelAccessor, modelType, propertyName);
		}

		public override ModelMetadata GetMetadataForType(Func<object> modelAccessor, Type modelType)
		{
			return new ModelMetadata(this, null, modelAccessor, modelType, null);
		}
	}

	//public class PocoMetadataProvider<T> {
	//    static PocoMetadataProvider() {
	//        var propertyInfos = TypeConfig<T>.Properties;  } }

	public class ModelMetadataProviders
	{
		private ModelMetadataProvider currentProvider;
		private static readonly ModelMetadataProviders Instance = new ModelMetadataProviders();

		internal ModelMetadataProviders()
		{
			currentProvider = new PocoMetadataProvider();
		}

		public static ModelMetadataProvider Current
		{
			get
			{
				return Instance.CurrentInternal;
			}
			set
			{
				Instance.CurrentInternal = value;
			}
		}

		internal ModelMetadataProvider CurrentInternal
		{
			get
			{
				return currentProvider;
			}
			set
			{
				currentProvider = value ?? new EmptyModelMetadataProvider();
			}
		}
	}

	internal interface IResolver<T>
	{
		T Current { get; }
	}

	internal static class DictionaryHelpers
	{

		public static IEnumerable<KeyValuePair<string, TValue>> FindKeysWithPrefix<TValue>(IDictionary<string, TValue> dictionary, string prefix)
		{
			TValue exactMatchValue;
			if (dictionary.TryGetValue(prefix, out exactMatchValue))
			{
				yield return new KeyValuePair<string, TValue>(prefix, exactMatchValue);
			}

			foreach (var entry in dictionary)
			{
				string key = entry.Key;

				if (key.Length <= prefix.Length)
				{
					continue;
				}

				if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				char charAfterPrefix = key[prefix.Length];
				switch (charAfterPrefix)
				{
					case '[':
					case '.':
						yield return entry;
						break;
				}
			}
		}

		public static bool DoesAnyKeyHavePrefix<TValue>(IDictionary<string, TValue> dictionary, string prefix)
		{
			return FindKeysWithPrefix(dictionary, prefix).Any();
		}

	}
}