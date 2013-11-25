using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack
{
	public static class Extensions
	{
		public static string GetComplexTypeName(this Type t)
		{
			return t.FullName.Replace(t.Namespace + ".", "").Replace("+", ".");
		}
	}
}
