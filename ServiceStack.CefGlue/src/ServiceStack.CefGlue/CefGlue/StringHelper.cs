namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal static class StringHelper
    {
		public static bool IsNullOrEmpty(string value)
		{
			return value == null || value.Length == 0;
		}

		public static bool IsNullOrWhiteSpace(string value)
		{
			if (value == null) return true;

			for (int i = 0; i < value.Length; i++)
			{
				if (!char.IsWhiteSpace(value[i])) return false;
			}

			return true;
		}
    }
}
