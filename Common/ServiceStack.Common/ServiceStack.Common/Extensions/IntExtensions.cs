using System;
using System.Collections.Generic;

namespace ServiceStack.Common.Extensions
{
	public static class IntExtensions
	{
		public static IEnumerable<int> Times(this int times)
		{
			for (var i=0; i < times; i++)
			{
				yield return i;
			}
		}

		public static void Times(this int times, Action<int> actionFn)
		{
			for (var i=0; i<times; i++)
			{
				actionFn(i);
			}
		}
	}
}