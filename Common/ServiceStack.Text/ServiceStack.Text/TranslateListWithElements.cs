using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Text.Support;

namespace ServiceStack.Text
{
	public static class TranslateListWithElements
	{
		private static readonly Dictionary<Type, Func<object, Type, object>> TranslateICollectionCache 
			= new Dictionary<Type, Func<object, Type, object>>();

		public static object TranslateToGenericICollectionCache(object from, Type toInstanceOfType, Type elementType)
		{
			Func<object, Type, object> translateToFn;
			lock (TranslateICollectionCache)
			{
				if (!TranslateICollectionCache.TryGetValue(toInstanceOfType, out translateToFn))
				{
					var genericType = typeof(TranslateListWithElements<>).MakeGenericType(elementType);

					var mi = genericType.GetMethod("LateBoundTranslateToGenericICollection",
						BindingFlags.Static | BindingFlags.Public);

					translateToFn = (Func<object, Type, object>)Delegate.CreateDelegate(typeof(Func<object, Type, object>), mi);

					TranslateICollectionCache[elementType] = translateToFn;
				}
			}

			return translateToFn(from, toInstanceOfType);
		}

		private static readonly Dictionary<ConvertibleTypeKey, Func<object, Type, object>> TranslateConvertibleICollectionCache 
			= new Dictionary<ConvertibleTypeKey, Func<object, Type, object>>();

		public static object TranslateToConvertibleGenericICollectionCache(
			object from, Type toInstanceOfType, Type fromElementType)
		{
			var typeKey = new ConvertibleTypeKey(toInstanceOfType, fromElementType);
			Func<object, Type, object> translateToFn;
			lock (TranslateICollectionCache)
			{
				if (!TranslateConvertibleICollectionCache.TryGetValue(typeKey, out translateToFn))
				{
					var toElementType = toInstanceOfType.GetGenericType().GetGenericArguments()[0];
					var genericType = typeof(TranslateListWithConvertibleElements<,>)
						.MakeGenericType(fromElementType, toElementType);

					var mi = genericType.GetMethod("LateBoundTranslateToGenericICollection",
						BindingFlags.Static | BindingFlags.Public);

					translateToFn = (Func<object, Type, object>)
						Delegate.CreateDelegate(typeof(Func<object, Type, object>), mi);

					TranslateConvertibleICollectionCache[typeKey] = translateToFn;
				}
			}

			return translateToFn(from, toInstanceOfType);
		}

		public static object TryTranslateToGenericICollection(Type fromPropertyType, Type toPropertyType, object fromValue)
		{
			var args = typeof(ICollection<>).GetGenericArgumentsIfBothHaveSameGenericDefinitionTypeAndArguments(
				fromPropertyType, toPropertyType);

			if (args != null)
			{
				return TranslateToGenericICollectionCache(
					fromValue, toPropertyType, args[0]);
			}

			var varArgs = typeof(ICollection<>).GetGenericArgumentsIfBothHaveConvertibleGenericDefinitionTypeAndArguments(
			fromPropertyType, toPropertyType);

			if (varArgs != null)
			{
				return TranslateToConvertibleGenericICollectionCache(
					fromValue, toPropertyType, varArgs.Args1[0]);
			}

			return null;
		}

	}

	public class ConvertibleTypeKey
	{
		public Type ToInstanceType { get; set; }
		public Type FromElemenetType { get; set; }

		public ConvertibleTypeKey()
		{
		}

		public ConvertibleTypeKey(Type toInstanceType, Type fromElemenetType)
		{
			ToInstanceType = toInstanceType;
			FromElemenetType = fromElemenetType;
		}

		public bool Equals(ConvertibleTypeKey other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.ToInstanceType, ToInstanceType) && Equals(other.FromElemenetType, FromElemenetType);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(ConvertibleTypeKey)) return false;
			return Equals((ConvertibleTypeKey)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((ToInstanceType != null ? ToInstanceType.GetHashCode() : 0) * 397)
					^ (FromElemenetType != null ? FromElemenetType.GetHashCode() : 0);
			}
		}
	}

	public class TranslateListWithElements<T>
	{
		public static object CreateInstance(Type toInstanceOfType)
		{
			if (toInstanceOfType.IsGenericType)
			{
				if (toInstanceOfType.HasAnyTypeDefinitionsOf(
					typeof(ICollection<>), typeof(IList<>)))
				{
					return ReflectionExtensions.CreateInstance(typeof(List<T>));
				}
			}

			return ReflectionExtensions.CreateInstance(toInstanceOfType);
		}

		public static IList TranslateToIList(IList fromList, Type toInstanceOfType)
		{
			var to = (IList)ReflectionExtensions.CreateInstance(toInstanceOfType);
			foreach (var item in fromList)
			{
				to.Add(item);
			}
			return to;
		}

		public static object LateBoundTranslateToGenericICollection(
			object fromList, Type toInstanceOfType)
		{
			return TranslateToGenericICollection(
				(ICollection<T>)fromList, toInstanceOfType);
		}

		public static ICollection<T> TranslateToGenericICollection(
			ICollection<T> fromList, Type toInstanceOfType)
		{
			var to = (ICollection<T>)CreateInstance(toInstanceOfType);
			foreach (var item in fromList)
			{
				to.Add(item);
			}
			return to;
		}
	}

	public class TranslateListWithConvertibleElements<TFrom, TTo>
	{
		private static readonly Func<TFrom, TTo> ConvertFn;

		static TranslateListWithConvertibleElements()
		{
			ConvertFn = GetConvertFn();
		}

		public static object LateBoundTranslateToGenericICollection(
			object fromList, Type toInstanceOfType)
		{
			return TranslateToGenericICollection(
				(ICollection<TFrom>)fromList, toInstanceOfType);
		}

		public static ICollection<TTo> TranslateToGenericICollection(
			ICollection<TFrom> fromList, Type toInstanceOfType)
		{
			var to = (ICollection<TTo>)TranslateListWithElements<TTo>.CreateInstance(toInstanceOfType);

			foreach (var item in fromList)
			{
				var toItem = ConvertFn(item);
				to.Add(toItem);
			}
			return to;
		}

		private static Func<TFrom, TTo> GetConvertFn()
		{
			if (typeof(TTo) == typeof(string))
			{
				return x => (TTo)(object)TypeSerializer.SerializeToString(x);
			}
			if (typeof(TFrom) == typeof(string))
			{
				return x => TypeSerializer.DeserializeFromString<TTo>((string)(object)x);
			}
			return x => TypeSerializer.DeserializeFromString<TTo>(
							TypeSerializer.SerializeToString(x));
		}
	}
}
