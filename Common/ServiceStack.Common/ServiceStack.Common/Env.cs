
using System;

namespace ServiceStack.Common
{
	public static class Env
	{
		static Env()
		{
			var platform = (int)Environment.OSVersion.Platform;
			IsUnix = (platform == 4) || (platform == 6) || (platform == 128);

			IsMono = Type.GetType ("Mono.Runtime") != null;
			
			IsMonoTouch = Type.GetType("MonoTouch.Foundation.NSObject") != null;

			SupportsExpressions = SupportsEmit = !IsMonoTouch;

			UserAgent = Environment.OSVersion.Platform
				+ (IsMono ? "/Mono" : "/.NET") 
				+ (IsMonoTouch ? " MonoTouch" : "");			
		}

		public static bool IsUnix { get; set; }

		public static bool IsMono { get; set; }

		public static bool IsMonoTouch { get; set; }

		public static bool SupportsExpressions { get; set; }

		public static bool SupportsEmit { get; set; }

		public static string UserAgent { get; set; }
	}
}