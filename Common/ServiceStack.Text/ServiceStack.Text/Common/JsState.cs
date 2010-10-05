using System;

namespace ServiceStack.Text.Common
{
	internal static class JsState
	{
		//Exposing field for perf
		[ThreadStatic]
		public static int WritingKeyCount = 0;

		[ThreadStatic]
		public static bool IsWritingValue = false;
	}
}