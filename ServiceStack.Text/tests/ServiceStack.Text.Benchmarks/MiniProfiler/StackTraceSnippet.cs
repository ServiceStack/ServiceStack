using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Gets part of a stack trace containing only methods we care about.
    /// </summary>
    public static class StackTraceSnippet
    {
        // TODO: Uhhhhhhh, this isn't gonna work. Let's come back to this. Oh and async. Dammit.
        private const string AspNetEntryPointMethodName = "System.Web.HttpApplication.IExecutionStep.Execute";

        /// <summary>
        /// Gets the current formatted and filtered stack trace.
        /// </summary>
        /// <returns>Space separated list of methods</returns>
        public static string Get()
        {
#if !NETCOREAPP1_1
            var frames = new StackTrace().GetFrames();
#else // TODO: Make this work in .NET Standard, true fix isn't until 2.0 via https://github.com/dotnet/corefx/pull/12527
            StackFrame[] frames = null;
#endif
            if (frames == null /*|| MiniProfiler.Settings.StackMaxLength <= 0*/)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            int stackLength = -1; // Starts on -1 instead of zero to compensate for adding 1 first time

            foreach (StackFrame t in frames)
            {
                var method = t.GetMethod();

                // no need to continue up the chain
                if (method.Name == AspNetEntryPointMethodName)
                    break;

                if (stackLength >= 120 /*MiniProfiler.Settings.StackMaxLength*/)
                    break;

                var assembly = method.Module.Assembly.GetName().Name;
                //if (!ShouldExcludeType(method)
                //    && !MiniProfiler.Settings.AssembliesToExclude.Contains(assembly)
                //    && !MiniProfiler.Settings.MethodsToExclude.Contains(method.Name))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(method.Name);
                    stackLength += method.Name.Length + 1; // 1 added for spaces.
                }
            }

            return sb.ToString();
        }

        /*private static bool ShouldExcludeType(MethodBase method)
        {
            var t = method.DeclaringType;

            while (t != null)
            {
                if (MiniProfiler.Settings.TypesToExclude.Contains(t.Name))
                    return true;

                t = t.DeclaringType;
            }

            return false;
        }
       */
    }
}