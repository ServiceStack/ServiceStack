using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ServiceStack.MiniProfiler.Helpers
{
	/// <summary>
	/// Gets part of a stack trace containing only methods we care about.
	/// </summary>
	public class StackTraceSnippet
	{
		private const string AspNetEntryPointMethodName = "System.Web.HttpApplication.IExecutionStep.Execute";

		/// <summary>
		/// Gets the current formatted and filted stack trace.
		/// </summary>
		/// <returns>Space separated list of methods</returns>
		public static string Get()
		{
			var frames = new StackTrace().GetFrames();
			if (frames == null)
			{
				return "";
			}

			var methods = new List<string>();

			foreach (StackFrame t in frames)
			{
				var method = t.GetMethod();

				// no need to continue up the chain
				if (method.Name == AspNetEntryPointMethodName)
					break;

				var assembly = method.Module.Assembly.GetName().Name;
				if (!Profiler.Settings.AssembliesToExclude.Contains(assembly) &&
					!ShouldExcludeType(method) &&
					!Profiler.Settings.MethodsToExclude.Contains(method.Name))
				{
					methods.Add(method.Name);
				}
			}

			var result = string.Join(" ", methods.ToArray());

            if (result.Length > Profiler.Settings.StackMaxLength)
            {
                var index = result.IndexOf(" ", Profiler.Settings.StackMaxLength);		
	            if (index >= Profiler.Settings.StackMaxLength)		
	            {
	                result = result.Substring(0, index);		
	            }
	        }

			return result;
		}

		private static bool ShouldExcludeType(MethodBase method)
		{
			var t = method.DeclaringType;

			while (t != null)
			{
				if (Profiler.Settings.TypesToExclude.Contains(t.Name))
					return true;

				t = t.DeclaringType;
			}
			return false;
		}
	}
}
