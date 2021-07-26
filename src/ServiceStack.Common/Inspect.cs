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
        /// Dump object in Ascii Markdown table
        /// </summary>
        public static string dumpTable(object instance, TextDumpOptions options) => DefaultScripts.TextDump(instance, options); 
        
        /// <summary>
        /// Dump object in Ascii Markdown table using specified column headers
        /// </summary>
        public static string dumpTable(object instance, string[] headers) => 
            DefaultScripts.TextDump(instance, new TextDumpOptions { Headers = headers, IncludeRowNumbers = false }); 
        
        /// <summary>
        /// Print Dump object in Ascii Markdown table
        /// </summary>
        public static void printDumpTable(object instance) => PclExport.Instance.WriteLine(dumpTable(instance));
        
        /// <summary>
        /// Print Dump object in Ascii Markdown table using specified column headers
        /// </summary>
        public static void printDumpTable(object instance, string[] headers) => 
            PclExport.Instance.WriteLine(dumpTable(instance, new TextDumpOptions { Headers = headers, IncludeRowNumbers = false }));
        
        /// <summary>
        /// Recursively prints the contents of any POCO object to HTML
        /// </summary>
        public static string htmlDump(object target) => HtmlScripts.HtmlDump(target, null); 

        /// <summary>
        /// Recursively prints the contents of any POCO object to HTML
        /// </summary>
        public static string htmlDump(object target, HtmlDumpOptions options) => HtmlScripts.HtmlDump(target, options); 

        /// <summary>
        /// Recursively prints the contents of any POCO object to HTML with specified columns
        /// </summary>
        public static string htmlDump(object target, string[] headers) => 
            HtmlScripts.HtmlDump(target, new HtmlDumpOptions { Headers = headers }); 
         
        /// <summary>
        /// Print htmlDump object
        /// </summary>
        public static void printHtmlDump(object instance) => PclExport.Instance.WriteLine(htmlDump(instance));
         
        /// <summary>
        /// Print htmlDump object with specified columns
        /// </summary>
        public static void printHtmlDump(object instance, string[] headers) => 
            PclExport.Instance.WriteLine(htmlDump(instance, headers));
    }
}