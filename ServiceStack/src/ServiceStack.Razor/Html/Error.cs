using System;
using System.Globalization;

namespace ServiceStack.Html
{
	internal static class Error
	{
		public static InvalidOperationException ViewDataDictionary_WrongTModelType(Type valueType, Type modelType)
		{
			string message = String.Format(CultureInfo.CurrentCulture, MvcResources.ViewDataDictionary_WrongTModelType,
				valueType, modelType);
			return new InvalidOperationException(message);
		}

		public static InvalidOperationException ViewDataDictionary_ModelCannotBeNull(Type modelType)
		{
			string message = String.Format(CultureInfo.CurrentCulture, MvcResources.ViewDataDictionary_ModelCannotBeNull,
				modelType);
			return new InvalidOperationException(message);
		}

        public static ArgumentException ParameterCannotBeNullOrEmpty(string parameterName)
        {
            return new ArgumentException(MvcResources.Common_NullOrEmpty, parameterName);
        }
	}
}
