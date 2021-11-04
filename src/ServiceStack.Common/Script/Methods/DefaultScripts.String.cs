using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    // ReSharper disable InconsistentNaming
    
    public partial class DefaultScripts
    {
        public IRawString raw(object value)
        {
            if (value == null || Equals(value, JsNull.Value))
                return ScriptConstants.EmptyRawString;
            if (value is string s)
                return s == string.Empty ? ScriptConstants.EmptyRawString : s.ToRawString();
            if (value is IRawString r)
                return r;
            if (value is bool b)
                return b ? ScriptConstants.TrueRawString : ScriptConstants.FalseRawString;

            var rawStr = value.ToString().ToRawString();
            return rawStr;
        }

        public string appSetting(string name) => Context.AppSettings.GetString(name);

        public string indent() => Context.Args[ScriptConstants.DefaultIndent] as string;
        public string indents(int count) => repeat(Context.Args[ScriptConstants.DefaultIndent] as string, count);
        public string space() => " ";
        public string spaces(int count) => padLeft("", count, ' ');
        public string newLine() => Context.Args[ScriptConstants.DefaultNewLine] as string;
        public string newLines(int count) => repeat(newLine(), count);
        public string newLine(string target) => target + newLine();

        public string currency(decimal decimalValue) => currency(decimalValue, null); //required to support 1/2 vars
        public string currency(decimal decimalValue, string culture)
        {
            var cultureInfo = culture != null
                ? new CultureInfo(culture)
                : (CultureInfo)Context.Args[ScriptConstants.DefaultCulture];

            var fmt = string.Format(cultureInfo, "{0:C}", decimalValue);
            return fmt;
        }

        public IRawString formatRaw(object obj, string fmt) => raw(string.Format(fmt.Replace("{{","{").Replace("}}","}"), obj));
        
        public string format(object obj, string format) => obj is IFormattable formattable
            ? formattable.ToString(format, null)
            : string.Format(format, obj);

        public string fmt(string format, object arg)
        {
            if (arg is object[] args)
                return string.Format(format, args);

            if (arg is List<object> argsList)
                return string.Format(format, argsList.ToArray());

            return string.Format(format, arg);
        }
        public string fmt(string format, object arg0, object arg1) => string.Format(format, arg0, arg1);
        public string fmt(string format, object arg0, object arg1, object arg2) => string.Format(format, arg0, arg1, arg2);

        public string append(string target, string suffix) => target + suffix;
        public string appendLine(string target) => target + newLine();

        public string appendFmt(string target, string format, object arg) => target + fmt(format, arg);
        public string appendFmt(string target, string format, object arg0, object arg1) => target + fmt(format, arg0, arg1);
        public string appendFmt(string target, string format, object arg0, object arg1, object arg2) => target + fmt(format, arg0, arg1, arg2);

        public string dateFormat(DateTime dateValue) => dateValue.ToString((string)Context.Args[ScriptConstants.DefaultDateFormat]);
        public string dateFormat(DateTime dateValue, string format) => dateValue.ToString(format ?? throw new ArgumentNullException(nameof(format)));
        public string dateTimeFormat(DateTime dateValue) => dateValue.ToString((string)Context.Args[ScriptConstants.DefaultDateTimeFormat]);
        public string timeFormat(TimeSpan timeValue) => timeValue.ToString((string)Context.Args[ScriptConstants.DefaultTimeFormat]);
        public string timeFormat(TimeSpan timeValue, string format) => timeValue.ToString(format);

        public string splitCase(string text) => text.SplitCamelCase().Replace('_', ' ').Replace("  ", " ");
        public string humanize(string text) => splitCase(text).ToTitleCase();
        public string titleCase(string text) => text.ToTitleCase();
        public string pascalCase(string text) => text.ToPascalCase();
        public string camelCase(string text) => text.ToCamelCase();
        public string snakeCase(string text) => text.ToLowercaseUnderscore();
        public string kebabCase(string text) => text.ToLowercaseUnderscore().Replace("_","-");

        public string textStyle(string text, string headerStyle)
        {
            if (text == null) return null;
            switch (headerStyle)
            {
                case nameof(splitCase):
                    return splitCase(text);
                case nameof(humanize):
                    return humanize(text);
                case nameof(titleCase):
                    return titleCase(text);
                case nameof(pascalCase):
                    return pascalCase(text);
                case nameof(camelCase):
                    return camelCase(text);
                case nameof(snakeCase):
                    return snakeCase(text);
                case nameof(kebabCase):
                    return kebabCase(text);
            }
            return text;
        }

        public string lower(string text) => text?.ToLower();
        public string upper(string text) => text?.ToUpper();

        public string substring(string text, int startIndex) => text.SafeSubstring(startIndex);
        public string substring(string text, int startIndex, int length) => text.SafeSubstring(startIndex, length);

        [Obsolete("typo")] public string substringWithElipsis(string text, int length) => text.SubstringWithEllipsis(0, length);
        [Obsolete("typo")] public string substringWithElipsis(string text, int startIndex, int length) => text.SubstringWithEllipsis(startIndex, length);

        public string substringWithEllipsis(string text, int length) => text.SubstringWithEllipsis(0, length);
        public string substringWithEllipsis(string text, int startIndex, int length) => text.SubstringWithEllipsis(startIndex, length);

        public string leftPart(string text, string needle) => text.LeftPart(needle);
        public string rightPart(string text, string needle) => text.RightPart(needle);
        public string lastLeftPart(string text, string needle) => text.LastLeftPart(needle);
        public string lastRightPart(string text, string needle) => text.LastRightPart(needle);


        public int compareTo(string text, string other) => string.Compare(text, other, (StringComparison)Context.Args[ScriptConstants.DefaultStringComparison]);

        public bool startsWith(string text, string needle) => text?.StartsWith(needle) == true;
        public bool endsWith(string text, string needle) => text?.EndsWith(needle) == true;

        public string replace(string text, string oldValue, string newValue) => text.Replace(oldValue, newValue);

        public string trimStart(string text) => text?.TrimStart();
        public string trimStart(string text, char c) => text?.TrimStart(c);
        public string trimEnd(string text) => text?.TrimEnd();
        public string trimEnd(string text, char c) => text?.TrimEnd(c);
        public string trim(string text) => text?.Trim();
        public string trim(string text, char c) => text?.Trim(c);

        public string padLeft(string text, int totalWidth) => text?.PadLeft(AssertWithinMaxQuota(totalWidth));
        public string padLeft(string text, int totalWidth, char padChar) => text?.PadLeft(AssertWithinMaxQuota(totalWidth), padChar);
        public string padRight(string text, int totalWidth) => text?.PadRight(AssertWithinMaxQuota(totalWidth));
        public string padRight(string text, int totalWidth, char padChar) => text?.PadRight(AssertWithinMaxQuota(totalWidth), padChar);

        public string[] splitOnFirst(string text, string needle) => text.SplitOnFirst(needle);
        public string[] splitOnLast(string text, string needle) => text.SplitOnLast(needle);
        public string[] split(string stringList) => stringList.Split(',');
        public string[] split(string stringList, object delimiter)
        {
            if (delimiter is IEnumerable<object> objDelims)
                delimiter = objDelims.Select(x => x.ToString());

            if (delimiter is char c)
                return stringList.Split(c);
            if (delimiter is string s)
                return s.Length == 1
                    ? stringList.Split(s[0])
                    : stringList.Split(new[] { s }, StringSplitOptions.RemoveEmptyEntries);
            if (delimiter is IEnumerable<string> strDelims)
                return strDelims.All(x => x.Length == 1)
                    ? stringList.Split(strDelims.Select(x => x[0]).ToArray(), StringSplitOptions.RemoveEmptyEntries)
                    : stringList.Split(strDelims.ToArray(), StringSplitOptions.RemoveEmptyEntries);

            throw new NotSupportedException($"{delimiter} is not a valid delimiter");
        }

        public string[] glob(IEnumerable<string> strings, string pattern)
        {
            var list = strings?.ToList() ?? new List<string>();
            return list.Where(x => x.Glob(pattern)).ToArray();
        }

        public string globln(IEnumerable<string> strings, string pattern) => joinln(glob(strings, pattern));

        public Dictionary<string, string> parseKeyValueText(string target) => target?.ParseKeyValueText();
        public Dictionary<string, string> parseKeyValueText(string target, string delimiter) => target?.ParseKeyValueText(delimiter);

        public IEnumerable<KeyValuePair<string,string>> parseAsKeyValues(string target) => target?.ParseAsKeyValues();
        public IEnumerable<KeyValuePair<string,string>> parseAsKeyValues(string target, string delimiter) => target?.ParseAsKeyValues(delimiter);

        public ICollection keys(object target) => 
            target is IDictionary d 
                ? d.Keys
                : target is IList l
                    ? times(l.Count)
                    : throw new NotSupportedException($"{target.GetType().Name} is not supported");
        public ICollection values(object target) => 
            target is IDictionary d 
                ? d.Values
                : target is IList l
                    ? l
                    : throw new NotSupportedException($"{target.GetType().Name} is not supported");

        public string addPath(string target, string pathToAppend) => target.AppendPath(pathToAppend);
        public string addPaths(string target, IEnumerable pathsToAppend) =>
            target.AppendPath(pathsToAppend.Map(x => x.ToString()).ToArray());

        public string addQueryString(string url, object urlParams) =>
            urlParams.AssertOptions(nameof(addQueryString)).Aggregate(url, (current, entry) => current.AddQueryParam(entry.Key, entry.Value));

        public string addHashParams(string url, object urlParams) =>
            urlParams.AssertOptions(nameof(addHashParams)).Aggregate(url, (current, entry) => current.AddHashParam(entry.Key, entry.Value));

        public string setQueryString(string url, object urlParams) =>
            urlParams.AssertOptions(nameof(setQueryString)).Aggregate(url, (current, entry) => current.SetQueryParam(entry.Key, entry.Value?.ToString()));

        public string setHashParams(string url, object urlParams) =>
            urlParams.AssertOptions(nameof(setHashParams)).Aggregate(url, (current, entry) => current.SetHashParam(entry.Key, entry.Value?.ToString()));

        public string repeating(int times, string text) => repeat(text, AssertWithinMaxQuota(times));
        public string repeat(string text, int times)
        {
            AssertWithinMaxQuota(times);
            var sb = StringBuilderCache.Allocate();
            for (var i = 0; i < times; i++)
            {
                sb.Append(text);
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public string escapeSingleQuotes(string text) => text?.Replace("'", "\\'");
        public string escapeDoubleQuotes(string text) => text?.Replace("\"", "\\\"");
        public string escapeBackticks(string text) => text?.Replace("`", "\\`");
        public string escapePrimeQuotes(string text) => text?.Replace("′", "\\′");
        public string escapeNewLines(string text) => text?.Replace("\r", "\\r").Replace("\n", "\\n");

        public IRawString jsString(string text) => string.IsNullOrEmpty(text) 
            ? RawString.Empty 
            : escapeNewLines(escapeSingleQuotes(text)).ToRawString();
        public IRawString jsQuotedString(string text) => 
            ("'" + escapeNewLines(escapeSingleQuotes(text ?? "")) + "'").ToRawString();

        private async Task serialize(ScriptScopeContext scope, object items, string jsconfig, Func<object, string> fn)
        {
            var defaultJsConfig = Context.Args[ScriptConstants.DefaultJsConfig] as string;
            jsconfig = jsconfig != null && !string.IsNullOrEmpty(defaultJsConfig)
                ? defaultJsConfig + "," + jsconfig
                : defaultJsConfig;

            if (jsconfig != null)
            {
                using (JsConfig.CreateScope(jsconfig))
                {
                    await scope.OutputStream.WriteAsync(fn(items)).ConfigAwait();
                    return;
                }
            }
            await scope.OutputStream.WriteAsync(items.ToJson()).ConfigAwait();
        }

        private IRawString serialize(object target, string jsconfig, Func<object, string> fn)
        {
            var defaultJsConfig = Context.Args[ScriptConstants.DefaultJsConfig] as string;
            jsconfig = jsconfig != null && !string.IsNullOrEmpty(defaultJsConfig)
                ? defaultJsConfig + "," + jsconfig
                : defaultJsConfig;

            if (jsconfig == null)
                return fn(target.AssertNoCircularDeps()).ToRawString();

            using (JsConfig.CreateScope(jsconfig))
            {
                return fn(target).ToRawString();
            }
        }

        static string jsonOrNull(object x)
        {
            if (x != null)
            {
                var json = x.ToJson();
                if (!string.IsNullOrEmpty(json))
                    return json;
            }
            return "null";
        }
        
        //Filters
        public IRawString json(object value) => serialize(value, null, jsonOrNull);
        public IRawString json(object value, string jsconfig) => serialize(value, jsconfig, jsonOrNull);
        public IRawString jsv(object value) => serialize(value, null, x => x.ToJsv() ?? "");
        public IRawString jsv(object value, string jsconfig) => serialize(value, jsconfig, x => x.ToJsv() ?? "");
        public IRawString csv(object value) => (value.AssertNoCircularDeps().ToCsv() ?? "").ToRawString();
        public IRawString dump(object value) => serialize(value, null, x => x.Dump() ?? "");
        public IRawString indentJson(object value) => indentJson(value, null);
        public IRawString indentJson(object value, string jsconfig) => 
            (value is string js ? js : json(value).ToRawString()).IndentJson().ToRawString();

        //Blocks
        public Task json(ScriptScopeContext scope, object items) => json(scope, items, null);
        public Task json(ScriptScopeContext scope, object items, string jsConfig) => serialize(scope, items, jsConfig, x => x.ToJson());

        public Task jsv(ScriptScopeContext scope, object items) => jsv(scope, items, null);
        public Task jsv(ScriptScopeContext scope, object items, string jsConfig) => serialize(scope, items, jsConfig, x => x.ToJsv());

        public Task dump(ScriptScopeContext scope, object items) => jsv(scope, items, null);
        public Task dump(ScriptScopeContext scope, object items, string jsConfig) => serialize(scope, items, jsConfig, x => x.Dump());

        public Task csv(ScriptScopeContext scope, object items) => scope.OutputStream.WriteAsync(items.ToCsv());
        public Task xml(ScriptScopeContext scope, object items) => scope.OutputStream.WriteAsync(items.ToXml());

        public JsonObject jsonToObject(string json) => JsonObject.Parse(json);
        public JsonArrayObjects jsonToArrayObjects(string json) => JsonArrayObjects.Parse(json);
        public Dictionary<string, object> jsonToObjectDictionary(string json) => json.FromJson<Dictionary<string, object>>();
        public Dictionary<string, string> jsonToStringDictionary(string json) => json.FromJson<Dictionary<string, string>>();

        public Dictionary<string, object> jsvToObjectDictionary(string json) => json.FromJsv<Dictionary<string, object>>();
        public Dictionary<string, string> jsvToStringDictionary(string json) => json.FromJsv<Dictionary<string, string>>();

        public object eval(ScriptScopeContext scope, string js) => JS.eval(js, scope);
        public object parseJson(string json) => JSON.parse(json);

        public string base64(byte[] bytes) => Convert.ToBase64String(bytes);
        public byte[] fromBase64(string base64) => Convert.FromBase64String(base64);

        public List<List<string>> parseCsv(string csv)
        {
            var trimmedBody = StringBuilderCache.Allocate();
            foreach (var line in csv.ReadLines())
            {
                trimmedBody.AppendLine(line.Trim());
            }
            var strList = trimmedBody.ToString().FromCsv<List<List<string>>>();
            return strList;
        }

        public List<KeyValuePair<string, string>> parseKeyValues(string keyValuesText) =>
            parseKeyValues(keyValuesText, " ");
        public List<KeyValuePair<string, string>> parseKeyValues(string keyValuesText, string delimiter) => 
            keyValuesText.Trim().ParseAsKeyValues(delimiter);

        private static readonly Regex InvalidCharsRegex = new Regex(@"[^a-z0-9\s-]", RegexOptions.Compiled);
        private static readonly Regex SpacesRegex = new Regex(@"\s", RegexOptions.Compiled);
        private static readonly Regex CollapseHyphensRegex = new Regex("-+", RegexOptions.Compiled);
        
        public string generateSlug(string phrase)
        {
            var str = phrase.ToLower()
                .Replace("#", "sharp")  // c#, f# => csharp, fsharp
                .Replace("++", "pp");   // c++ => cpp

            str = InvalidCharsRegex.Replace(str, "-");
            //// convert multiple spaces into one space   
            //str = CollapseSpacesRegex.Replace(str, " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 100 ? str.Length : 100).Trim();
            str = SpacesRegex.Replace(str, "-");
            str = CollapseHyphensRegex.Replace(str, "-");

            if (string.IsNullOrEmpty(str))
                return null;

            if (str[0] == '-')
                str = str.Substring(1);
            if (str[str.Length - 1] == '-')
                str = str.Substring(0, str.Length - 1);

            return str;            
        }
        
        public bool isBinary(string fileOrExt) => MimeTypes.IsBinary(MimeTypes.GetMimeType(fileOrExt));
        public string contentType(string fileOrExt) => MimeTypes.GetMimeType(fileOrExt);

        public static string[] splitLines(string contents) => contents.Replace("\r", "").Split('\n');
        public IEnumerable<string> readLines(string contents) => contents.ReadLines();
    }
}