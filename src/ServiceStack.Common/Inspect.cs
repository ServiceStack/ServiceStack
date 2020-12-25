using System;
using System.IO;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack
{
    /// <summary>
    /// Helper utility for inspecting variables
    /// </summary>
    public static class Inspect
    {
        public static class Config
        {
            public const string VarsName = "vars.json";
            
            public static Action<object> VarsFilter { get; set; } = DefaultVarsFilter;
            
            public static Func<object,string> DumpTableFilter { get; set; }

            public static void DefaultVarsFilter(object anonArgs)
            {
                try
                {
                    var inspectVarsPath = Environment.GetEnvironmentVariable("INSPECT_VARS");
                    if (string.IsNullOrEmpty(inspectVarsPath)) // Disable
                        return;
                    
                    var varsPath = Path.DirectorySeparatorChar == '\\'
                        ? inspectVarsPath.Replace('/','\\')
                        : inspectVarsPath.Replace('\\','/');

                    if (varsPath.IndexOf(Path.DirectorySeparatorChar) >= 0)
                        Path.GetDirectoryName(varsPath).AssertDir();
                    
                    File.WriteAllText(varsPath, anonArgs.ToSafeJson());
                }
                catch (Exception ex)
                {
                    Tracer.Instance.WriteError("Inspect.vars() Error: " + ex);
                }
            }
        }

        /// <summary>
        /// Dump serialized values to 'vars.json'
        /// </summary>
        /// <param name="anonArgs">Anonymous object with named value</param>
        // ReSharper disable once InconsistentNaming
        public static void vars(object anonArgs) => Config.VarsFilter?.Invoke(anonArgs);

        /// <summary>
        /// Recursively prints the contents of any POCO object in a human-friendly, readable format
        /// </summary>
        public static string dump<T>(T instance) => instance.Dump();

        /// <summary>
        /// Print Dump to Console.WriteLine
        /// </summary>
        public static void printDump<T>(T instance) => PclExport.Instance.WriteLine(dump(instance));
        
        /// <summary>
        /// Dump object in Ascii Markdown table
        /// </summary>
        public static string dumpTable(object instance) => DefaultScripts.TextDump(instance, null); 
        
        /// <summary>
        /// Print Dump object in Ascii Markdown table
        /// </summary>
        public static void printDumpTable(object instance) => PclExport.Instance.WriteLine(dumpTable(instance)); 
    }
}