using System;
using System.Collections.Generic;

namespace ServiceStack.Text.Jsv
{
	public static class ParseUtils
	{
		//public static Func<string, object> GetParseMethod(Type type)
		//{
		//    type = Nullable.GetUnderlyingType(type) ?? type;

		//    if (type.IsEnum)
		//    {
		//        return value => ParseEnum(type, value);
		//    }

		//    if (type == typeof(string))
		//        return ParseString;

		//    if (type == typeof(object))
		//        return ParseObject;

		//    if (type.IsEnum)
		//        return value => ParseEnum(type, value);

		//    if (type.IsArray)
		//    {
		//        return DeserializeArray.GetParseFn(type);
		//    }

		//    var builtInMethod = DeserializeBuiltin.GetParseMethod(type);
		//    if (builtInMethod != null)
		//        return builtInMethod;

		//    if (type.IsGenericType())
		//    {
		//        var listInterfaces = type.FindInterfaces(
		//            (t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);
		//        if (listInterfaces.Length > 0)
		//            return DeserializeListWithElements.GetParseMethod(type);

		//        var mapInterfaces = type.FindInterfaces(
		//            (t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null);
		//        if (mapInterfaces.Length > 0)
		//            return DeserializeDictionary.GetParseMethod(type);

		//        var collectionInterfaces = type.FindInterfaces(
		//            (t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>), null);

		//        if (collectionInterfaces.Length > 0)
		//            return DeserializeCollection.GetParseMethod(type);
		//    }

		//    var staticParseMethod = StaticParseMethod.GetParseMethod(type);
		//    if (staticParseMethod != null)
		//        return staticParseMethod;

		//    var typeConstructor = DeserializeType.GetParseMethod(type);
		//    if (typeConstructor != null)
		//        return typeConstructor;

		//    var stringConstructor = DeserializeTypeUtils.GetParseMethod(type);
		//    if (stringConstructor != null) return stringConstructor;

		//    return null;
		//}
		
		public static object NullValueType(Type type)
		{
			return ReflectionExtensions.GetDefaultValue(type);
		}

		public static string ParseString(string value)
		{
			return value.FromCsvField();
		}

		public static object ParseObject(string value)
		{
			return value;
		}

		public static object ParseEnum(Type type, string value)
		{
			return Enum.Parse(type, value);
		}

		public static string EatTypeValue(string value, ref int i)
		{
			var tokenStartPos = i;
			var typeEndsToEat = 1;
			while (++i < value.Length && typeEndsToEat > 0)
			{
				if (value[i] == TypeSerializer.MapStartChar)
					typeEndsToEat++;
				if (value[i] == TypeSerializer.MapEndChar)
					typeEndsToEat--;
			}
			return value.Substring(tokenStartPos, i - tokenStartPos);
		}


		public static string EatKey(string value, ref int i)
		{
			return EatUntilCharFound(value, ref i, TypeSerializer.MapKeySeperator);
		}

		public static string EatValue(string value, ref int i)
		{
			return EatUntilCharFound(value, ref i, TypeSerializer.ItemSeperator);
		}

		public static string EatUntilCharFound(string value, ref int i, char findChar)
		{
			var tokenStartPos = i;
			var valueLength = value.Length;
			if (value[tokenStartPos] != TypeSerializer.QuoteChar)
			{
				i = value.IndexOf(findChar, tokenStartPos);
				if (i == -1) i = valueLength;
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			while (++i < valueLength)
			{
				if (value[i] == TypeSerializer.QuoteChar
				    && (i + 1 >= valueLength || value[i + 1] == findChar))
				{
					i++;
					return value.Substring(tokenStartPos, i - tokenStartPos);
				}
			}

			throw new IndexOutOfRangeException("Could not find ending quote");
		}

	}
}