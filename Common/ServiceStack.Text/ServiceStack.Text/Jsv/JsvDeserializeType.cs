using System;
using System.Reflection;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv
{
	public static class JsvDeserializeType
	{
		public static Action<object, object> GetSetPropertyMethod(Type type, PropertyInfo propertyInfo)
		{
			return DeserializeType<JsvTypeSerializer>.GetSetPropertyMethod(type, propertyInfo);
		}
	}
}