using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack;

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
    public static string dump<T>(T instance)
    {
        try
        {
            // convert into common common collection type for generic dump routine 
            var use = UseType(instance);
            return dumpInternal(use);
        }
        catch (Exception e)
        {
            var log = LogManager.GetLogger(typeof(Inspect));
            if (log.IsDebugEnabled)
                log.Debug($"Could not pretty print {typeof(T).Name}", e);
        }
        return instance.Dump();
    }

    private static object UseType<T>(T instance)
    {
        if (typeof(T).IsValueType || typeof(T) == typeof(string))
            return instance;
        if (instance is IEnumerable e)
        {
            var elType = EnumerableUtils.FirstOrDefault(e);
            if (elType?.GetType().GetTypeWithGenericTypeDefinitionOf(typeof(KeyValuePair<,>)) != null)
                return instance.ToObjectDictionary();
            return new List<object>(e.Cast<object>());
        }
        return instance.ToObjectDictionary();
    }

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

    private static string dumpInternal(object instance)
    {
        if (instance is Dictionary<string, object> obj)
        {
            var sb = StringBuilderCache.Allocate();
            var keyLen = obj.Keys.Map(x => x.Length).Max() + 2;
            foreach (var entry in obj)
            {
                var k = entry.Key;
                var value = entry.Value;
                var key = k + ":";
                if (value is Dictionary<string, object> nestedObj)
                {
                    if (nestedObj.Count > 0)
                    {
                        sb.AppendLine($"{key}");
                        sb.AppendLine(nestedObj.Dump());
                    }
                }
                else if (value is List<object> nestedList)
                {
                    if (nestedList.Count > 0)
                    {
                        sb.AppendOneNewLine();
                        if (nestedList.Count == 1)
                        {
                            sb.AppendLine($"[{k}]");
                            sb.AppendLine(dumpInternal(nestedList[0]));
                        }
                        else
                        {
                            var showNakedList = nestedList
                                .All(x => x is not Dictionary<string, object> and not List<object>);
                            var showTable = nestedList
                                .All(x => x is Dictionary<string, object> nestedItem && nestedItem.Values
                                    .All(y => y is not Dictionary<string, object> and not List<object>));

                            sb.AppendLine(key);
                            if (showNakedList)
                            {
                                foreach (var item in nestedList)
                                {
                                    sb.AppendLine($"  {item}");
                                }
                            }
                            else if (showTable)
                            {
                                sb.AppendLine(nestedList.DumpTable());
                            }
                            else
                            {
                                sb.AppendLine(nestedList.Dump());
                            }
                        }
                        sb.AppendOneNewLine();
                    }
                }
                else
                {
                    sb.AppendLine($"{key.PadRight(keyLen, ' ')} {value}");
                }
            }
            return StringBuilderCache.ReturnAndFree(sb).TrimStart();
        }
        if (instance is List<object> list)
            return list.DumpTable();
        return instance.Dump();
    }

    private static StringBuilder AppendOneNewLine(this StringBuilder sb)
    {
        if (sb.Length <= 2)
        {
            sb.AppendLine();
        }
        else
        {
            var c1 = sb[sb.Length - 1];
            var c2 = sb[sb.Length - 2];

            var hasNewLine = c1 == '\n' && c2 == '\n'
                             || (sb.Length > 4 && c1 == '\n' && c2 == '\r' && sb[sb.Length - 3] == '\n' && sb[sb.Length - 4] == '\r');

            if (!hasNewLine)
                sb.AppendLine();
        }
        return sb;
    }
}
