using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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

		public static object TryTranslateToGenericICollection(Type fromPropertyType, Type toPropertyType, object fromValue)
		{
			var args = typeof(ICollection<>).GetGenericArgumentsIfBothHaveSameGenericDefinitionTypeAndArguments(
				fromPropertyType, toPropertyType);

			if (args == null) return null;

			return TranslateToGenericICollectionCache(
				fromValue, toPropertyType, args[0]);
		}

	}


	public class TranslateListWithElements<T>
	{
		private static object CreateInstance(Type toInstanceOfType)
		{
			if (toInstanceOfType.IsGenericType)
			{
				if (toInstanceOfType.HasAnyTypeDefinitionsOf(
					typeof(ICollection<>), typeof(IList<>)))
				{
					return ReflectionExtensions.CreateInstance(typeof (List<T>));
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
				(ICollection<T>) fromList, toInstanceOfType);
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
}
