using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Text
{
	public class ParseStringTypeMethod
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
				map[propertyInfo.Name] = ParseStringMethods.GetParseMethod(propertyInfo.PropertyType);
				setterMap[propertyInfo.Name] = GetSetPropertyMethod(type, propertyInfo);
			}

			var ctorFn = ReflectionUtils.GetConstructorMethodToCache(type);
			return value => StringToType(value, ctorFn, setterMap, map);
		}

		private static object StringToType(string strType, Func<object> ctorFn,
			IDictionary<string, Action<object, object>> setterMap, IDictionary<string, Func<string, object>> parseStringFnMap)
		{
			if (strType[0] != StringSerializer.MapStartChar)
				throw new SerializationException(string.Format(
					"Type definitions should start with a '{0}'", StringSerializer.MapStartChar));

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
			while (value[++i] != StringSerializer.MapKeySeperator) { }
			return value.Substring(tokenStartPos, i - tokenStartPos);
		}

		private static string EatPropertyValue(string value, ref int i)
		{
			var tokenStartPos = i;
			var valueLength = value.Length;
			var valueChar = value[i];

			if (i == valueLength
				|| valueChar == StringSerializer.ItemSeperator
				|| valueChar == StringSerializer.MapEndChar)
			{
				return null;
			}

			//Is List, i.e. [...]
			var withinQuotes = false;
			if (valueChar == StringSerializer.ListStartChar)
			{
				var endsToEat = 1;
				while (++i < valueLength && endsToEat > 0)
				{
					valueChar = value[i];

					if (valueChar == StringSerializer.QuoteChar)
						withinQuotes = !withinQuotes;

					if (withinQuotes)
						continue;

					if (valueChar == StringSerializer.ListStartChar)
						endsToEat++;

					if (valueChar == StringSerializer.ListEndChar)
						endsToEat--;
				}
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			//Is Type/Map, i.e. {...}
			if (valueChar == StringSerializer.MapStartChar)
			{
				var endsToEat = 1;
				while (++i < valueLength && endsToEat > 0)
				{
					valueChar = value[i];

					if (valueChar == StringSerializer.QuoteChar)
						withinQuotes = !withinQuotes;

					if (withinQuotes)
						continue;

					if (valueChar == StringSerializer.MapStartChar)
						endsToEat++;

					if (valueChar == StringSerializer.MapEndChar)
						endsToEat--;
				}
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}


			//Is Within Quotes, i.e. "..."
			if (valueChar == StringSerializer.QuoteChar)
			{
				while (++i < valueLength)
				{
					valueChar = value[i];

					if (valueChar != StringSerializer.QuoteChar) continue;
				
					var isLiteralQuote = i + 1 < valueLength && value[i + 1] == StringSerializer.QuoteChar;

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

				if (valueChar == StringSerializer.ItemSeperator
					|| valueChar == StringSerializer.MapEndChar)
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