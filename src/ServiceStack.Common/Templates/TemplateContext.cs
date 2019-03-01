using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Script;


namespace ServiceStack.Script
{
    public partial class SharpPages : ISharpPages, Templates.ITemplatePages {}
    
    public partial class ScriptContext
    {
        [Obsolete("Use DefaultScripts")]
        public DefaultScripts DefaultFilters => DefaultMethods;

        [Obsolete("Use ProtectedScripts")]
        public ProtectedScripts ProtectedFilters => ProtectedMethods;
    }
}

namespace ServiceStack.Templates
{
    [Obsolete("Use DefaultScripts")]
    public class TemplateDefaultFilters : Script.DefaultScripts {}
    
    [Obsolete("Use HtmlScripts")]
    public class TemplateHtmlFilters : Script.HtmlScripts {}
    
    [Obsolete("Use ProtectedScripts")]
    public class TemplateProtectedFilters : Script.ProtectedScripts {}

    [Obsolete("Use ScriptBlock")]
    public abstract class TemplateBlock : ServiceStack.Script.ScriptBlock
    {
        public override Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token) =>
            WriteAsync((TemplateScopeContext)scope, block, token);
        public abstract Task WriteAsync(TemplateScopeContext scope, PageBlockFragment block, CancellationToken ct);
    }

    [Obsolete("Use ScriptMethods")]
    public class TemplateFilter : ServiceStack.Script.ScriptMethods {}

    [Obsolete("Use TemplateContext")]
    public class TemplateContext : Script.ScriptContext
    {
        [Obsolete("Use ScriptMethods")]
        public List<Script.ScriptMethods> TemplateFilters => ScriptMethods;

        [Obsolete("Use ScriptBlocks")]
        public List<Script.ScriptBlock> TemplateBlocks => ScriptBlocks;

        [Obsolete("Use DefaultScripts")]
        public Script.DefaultScripts DefaultFilters => DefaultMethods;
        [Obsolete("Use ProtectedScripts")]
        public Script.ProtectedScripts ProtectedFilters => ProtectedMethods;
        [Obsolete("Use HtmlScripts")]
        public Script.HtmlScripts HtmlFilters => HtmlMethods;

        public new TemplateContext Init()
        {
            Container.AddSingleton(() => this);
            Container.AddSingleton(() => (ITemplatePages)Pages);

            base.Init();
            return this;
        }
    }
    
    [Obsolete("Use ScriptScopeContext")]
    public struct TemplateScopeContext 
    {
        public Script.PageResult PageResult { get; }
        public Script.SharpPage Page => PageResult.Page;
        public Script.SharpCodePage CodePage => PageResult.CodePage;
        public Script.ScriptContext Context => PageResult.Context;
        public Dictionary<string, object> ScopedParams { get; internal set; }
        public Stream OutputStream { get; }

        public TemplateScopeContext(Script.PageResult pageResult, Stream outputStream, Dictionary<string, object> scopedParams)
        {
            PageResult = pageResult;
            ScopedParams = scopedParams ?? new Dictionary<string, object>();
            OutputStream = outputStream;
        }
        
        public static implicit operator Script.ScriptScopeContext(TemplateScopeContext from)
        {
            return new Script.ScriptScopeContext(from.PageResult, from.OutputStream, from.ScopedParams);
        }
    }
    
    [Obsolete("Use ISharpPages")]
    public interface ITemplatePages : ISharpPages {}
    [Obsolete("Use IScriptPlugin")]
    public interface ITemplatePlugin : IScriptPlugin { }

    [Obsolete("Use SharpPage")]
    public class TemplatePage : Script.SharpPage 
    {
        public TemplatePage(TemplateContext context, IVirtualFile file, Script.PageFormat format = null) : base(context, file, format) { }
    }
    
    [Obsolete("Use SharpCodePage")]
    public class TemplateCodePage : Script.SharpCodePage {}

    public static class TemplateContextExtensions
    {
        public static string EvaluateTemplate(this TemplateContext context, string script, Dictionary<string, object> args = null)
        {
            var result = context.EvaluateScript(script, args, out var ex);
            if (ex?.InnerException is NotSupportedException)
                throw ex.InnerException;
            return result;
        }

        public static async Task<string> EvaluateTemplateAsync(this TemplateContext context, string script, Dictionary<string, object> args = null)
        {
            try
            {
                var result = await context.EvaluateScriptAsync(script, args);
                return result;
            }
            catch (ScriptException ex)
            {
                if (ex.InnerException is NotSupportedException)
                    throw ex.InnerException;
                throw;
            }
        }
    }
    
    [Obsolete("Use ScriptConstants")]
    public static class TemplateConstants
    {
        public const string MaxQuota = ScriptConstants.MaxQuota;
        public const string DefaultCulture = ScriptConstants.DefaultCulture;
        public const string DefaultDateFormat = ScriptConstants.DefaultDateFormat;
        public const string DefaultDateTimeFormat = ScriptConstants.DefaultDateTimeFormat;
        public const string DefaultTimeFormat = ScriptConstants.DefaultTimeFormat;
        public const string DefaultFileCacheExpiry = ScriptConstants.DefaultFileCacheExpiry;
        public const string DefaultUrlCacheExpiry = ScriptConstants.DefaultUrlCacheExpiry;
        public const string DefaultIndent = ScriptConstants.DefaultIndent;
        public const string DefaultNewLine = ScriptConstants.DefaultNewLine;
        public const string DefaultJsConfig = ScriptConstants.DefaultJsConfig;
        public const string DefaultStringComparison = ScriptConstants.DefaultStringComparison;
        public const string DefaultTableClassName = ScriptConstants.DefaultTableClassName;
        public const string DefaultErrorClassName = ScriptConstants.DefaultErrorClassName;

        public const string Debug = ScriptConstants.Debug;
        public const string AssignError = ScriptConstants.AssignError;
        public const string CatchError = ScriptConstants.CatchError; //assigns error and continues
        public const string HtmlEncode = ScriptConstants.HtmlEncode;
        public const string Model = ScriptConstants.Model;
        public const string Page = ScriptConstants.Page;
        public const string Partial = ScriptConstants.Partial;
        public const string TempFilePath = ScriptConstants.TempFilePath;
        public const string Index = ScriptConstants.Index;
        public const string Comparer = ScriptConstants.Comparer;
        public const string Map = ScriptConstants.Map;
        public const string Request = ScriptConstants.Request;
        public const string PathInfo = ScriptConstants.PathInfo;
        public const string PathArgs = ScriptConstants.PathArgs;
        public const string AssetsBase = ScriptConstants.AssetsBase;
        public const string Format = ScriptConstants.Format;

        public static IRawString EmptyRawString { get; } = ScriptConstants.EmptyRawString;
        public static IRawString TrueRawString { get; } = ScriptConstants.TrueRawString;
        public static IRawString FalseRawString { get; } = ScriptConstants.FalseRawString;
    }
    
    [Obsolete("Use ScriptConfig")]
    public static class TemplateConfig
    {
        public static HashSet<string> RemoveNewLineAfterFiltersNamed => ScriptConfig.RemoveNewLineAfterFiltersNamed;
        public static HashSet<string> OnlyEvaluateFiltersWhenSkippingPageFilterExecution =>
            ScriptConfig.OnlyEvaluateFiltersWhenSkippingPageFilterExecution;
        public static HashSet<Type> FatalExceptions => ScriptConfig.FatalExceptions;
        public static HashSet<Type> CaptureAndEvaluateExceptionsToNull => ScriptConfig.CaptureAndEvaluateExceptionsToNull;
        public static HashSet<string> DontEvaluateBlocksNamed => ScriptConfig.DontEvaluateBlocksNamed;
        public static int MaxQuota { get; set; } = 10000;
        public static CultureInfo DefaultCulture => ScriptConfig.DefaultCulture;
        public static string DefaultDateFormat => ScriptConfig.DefaultDateFormat;
        public static string DefaultDateTimeFormat => ScriptConfig.DefaultDateTimeFormat;
        public static string DefaultTimeFormat => ScriptConfig.DefaultTimeFormat;
        public static TimeSpan DefaultFileCacheExpiry => ScriptConfig.DefaultFileCacheExpiry;
        public static TimeSpan DefaultUrlCacheExpiry => ScriptConfig.DefaultUrlCacheExpiry;
        public static string DefaultIndent => ScriptConfig.DefaultIndent;
        public static string DefaultNewLine => ScriptConfig.DefaultNewLine;
        public static string DefaultJsConfig => ScriptConfig.DefaultJsConfig;
        public static StringComparison DefaultStringComparison => ScriptConfig.DefaultStringComparison;
        public static string DefaultTableClassName => ScriptConfig.DefaultTableClassName;
        public static string DefaultErrorClassName => ScriptConfig.DefaultErrorClassName;
        public static CultureInfo CreateCulture() => ScriptConfig.CreateCulture();
    }    
}
