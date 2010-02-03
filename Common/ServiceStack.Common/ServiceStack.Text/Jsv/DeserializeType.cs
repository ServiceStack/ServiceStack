using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceStack.Text.Jsv
{
	public class DeserializeType
	{
		public static Func<string, object> GetParseMethod(Type type)
		{
			if (!type.IsClass) return null;

			var propertyInfos = type.GetProperties();
			if (propertyInfos.Length == 0) return null;


			var setterMap = new Dictionary<string, Action<object, object>>();
			var map = new Dictionary<string, Func<string, object>>();

			foreach (var propertyInfo in propertyInfos)
			{
				map[propertyInfo.Name] = JsvReader.GetParseFn(propertyInfo.PropertyType);
				setterMap[propertyInfo.Name] = GetSetPropertyMethod(type, propertyInfo);
			}

			var ctorFn = ReflectionExtensions.GetConstructorMethodToCache(type);
			return value => StringToType(value, ctorFn, setterMap, map);
		}

		private static object StringToType(string strType, Func<object> ctorFn,
		                                   IDictionary<string, Action<object, object>> setterMap, IDictionary<string, Func<string, object>> parseStringFnMap)
		{
			if (strType[0] != TypeSerializer.MapStartChar)
				throw new SerializationException(string.Format(
				                                 	"Type definitions should start with a '{0}'", TypeSerializer.MapStartChar));

			var instance = ctorFn();
			string propertyName;

			try
			{
				var strTypeLength = strType.Length;
				for (var i=1; i < strTypeLength; i++)
				{
					propertyName = EatPropertyName(strType, ref i);
					i++;
					var propertyValueString = EatPropertyValue(strType, ref i);

					var parseStringFn = parseStringFnMap[propertyName];
					var propertyValue = parseStringFn(propertyValueString);
					var setterFn = setterMap[propertyName];

					if (setterFn != null)
					{
						setterFn(instance, propertyValue);
					}
				}
			}
			catch (Exception ex)
			{
				throw;
			}
			return instance;
		}

		private static string EatPropertyName(string value, ref int i)
		{
			var tokenStartPos = i;
			while (value[++i] != TypeSerializer.MapKeySeperator) { }
			return value.Substring(tokenStartPos, i - tokenStartPos);
		}

		private static string EatPropertyValue(string value, ref int i)
		{
			var tokenStartPos = i;
			var valueLength = value.Length;
			var valueChar = value[i];

			if (i == valueLength
			    || valueChar == TypeSerializer.ItemSeperator
			    || valueChar == TypeSerializer.MapEndChar)
			{
				return null;
			}

			//Is List, i.e. [...]
			var withinQuotes = false;
			if (valueChar == TypeSerializer.ListStartChar)
			{
				var endsToEat = 1;
				while (++i < valueLength && endsToEat > 0)
				{
					valueChar = value[i];

					if (valueChar == TypeSerializer.QuoteChar)
						withinQuotes = !withinQuotes;

					if (withinQuotes)
						continue;

					if (valueChar == TypeSerializer.ListStartChar)
						endsToEat++;

					if (valueChar == TypeSerializer.ListEndChar)
						endsToEat--;
				}
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			//Is Type/Map, i.e. {...}
			if (valueChar == TypeSerializer.MapStartChar)
			{
				var endsToEat = 1;
				while (++i < valueLength && endsToEat > 0)
				{
					valueChar = value[i];

					if (valueChar == TypeSerializer.QuoteChar)
						withinQuotes = !withinQuotes;

					if (withinQuotes)
						continue;

					if (valueChar == TypeSerializer.MapStartChar)
						endsToEat++;

					if (valueChar == TypeSerializer.MapEndChar)
						endsToEat--;
				}
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}


			//Is Within Quotes, i.e. "..."
			if (valueChar == TypeSerializer.QuoteChar)
			{
				while (++i < valueLength)
				{
					valueChar = value[i];

					if (valueChar != TypeSerializer.QuoteChar) continue;
				
					var isLiteralQuote = i + 1 < valueLength && value[i + 1] == TypeSerializer.QuoteChar;

					i++; //skip quote
					if (!isLiteralQuote)
						break;
				}
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			//Is Value
			while (++i < valueLength)
			{
				valueChar = value[i];

				if (valueChar == TypeSerializer.ItemSeperator
				    || valueChar == TypeSerializer.MapEndChar)
				{
					break;
				}
			}

			return value.Substring(tokenStartPos, i - tokenStartPos);
		}

		private static Action<object, object> GetSetPropertyMethod(Type type, PropertyInfo propertyInfo)
		{
			var setMethodInfo = propertyInfo.GetSetMethod();
			if (setMethodInfo == null) return null;
			var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
			var oValueParam = Expression.Parameter(typeof(object), "oValueParam");

			var instanceParam = Expression.Convert(oInstanceParam, type);
			var useType = propertyInfo.PropertyType;

			var valueParam = Expression.Convert(oValueParam, useType);
			var exprCallPropertySetFn = Expression.Call(instanceParam, setMethodInfo, valueParam);

			var propertySetFn = Expression.Lambda<Action<object, object>>
				(
				exprCallPropertySetFn,
				oInstanceParam,
				oValueParam
				).Compile();

			return propertySetFn;
		}
	}
}