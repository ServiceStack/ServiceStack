using System;

namespace ServiceStack.Common.Extensions
{
	public static class CustomExtensions
	{
		public static T To<T>(this IConvertible value)
		{
			try
			{
				return (T) Convert.ChangeType(value, typeof (T));
			}
			catch
			{
				return default(T);
			}
		}
	}
}


