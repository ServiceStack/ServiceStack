using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    public class TemplateDefaultFilters : TemplateFilter
    {
        public IRawString raw(object value) => value.ToString().ToRawString();

        public IRawString json(object value) => (value.ToJson() ?? "null").ToRawString();

        public string appSetting(string name) =>  Context.AppSettings.GetString(name);

        public int toInt(int value) => value;
        public long toLong(long value) => value;
        public double toDouble(double value) => value;
        public decimal toDecimal(decimal value) => value;
        public string toString(object value) => value?.ToString();

        public static double applyToNumbers(double lhs, double rhs, Func<double, double, double> fn) => fn(lhs, rhs);

        public double add(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x + y);
        public double sub(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x - y);
        public double subtract(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x - y);
        public double mul(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x * y);
        public double multiply(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x * y);
        public double div(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x / y);
        public double divide(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x / y);

        public string currency(decimal decimalValue) => currency(decimalValue, null); //required to support 1/2 vars

        public string currency(decimal decimalValue, string culture)
        {
            var cultureInfo = culture != null
                ? new CultureInfo(culture)
                : (CultureInfo) Context.Args[TemplateConstants.DefaultCulture];

            var fmt = string.Format(cultureInfo, "{0:C}", decimalValue);
            return fmt;
        }

        public string format(object obj, string format) => string.Format(format, obj);

        public object dateFormat(DateTime dateValue) =>  dateValue.ToString((string)Context.Args[TemplateConstants.DefaultDateFormat]);
        public object dateFormat(DateTime dateValue, string format) => dateValue.ToString(format ?? throw new ArgumentNullException(nameof(format)));
        public object dateTimeFormat(DateTime dateValue) =>  dateValue.ToString((string)Context.Args[TemplateConstants.DefaultDateTimeFormat]);

        public string humanize(string varName) => varName.SplitCamelCase().Replace('_',' ').ToTitleCase();
        public string titleCase(string varName) => varName.ToTitleCase();
        public string pascalCase(string varName) => varName.ToPascalCase();
        public string camelCase(string varName) => varName.ToCamelCase();

        public string lower(string varName) => varName?.ToLower();
        public string upper(string varName) => varName?.ToUpper();

        public string substring(string varName, int startIndex) => varName.SafeSubstring(startIndex);
        public string substring(string varName, int startIndex, int length) => varName.SafeSubstring(startIndex, length);

        public string trimStart(string text) => text?.TrimStart();
        public string trimEnd(string text) => text?.TrimEnd();
        public string trim(string text) => text?.Trim();

        public string padLeft(string text, int totalWidth) => text?.PadLeft(totalWidth);
        public string padLeft(string text, int totalWidth, char padChar) => text?.PadLeft(totalWidth, padChar);
        public string padRight(string text, int totalWidth) => text?.PadRight(totalWidth);
        public string padRight(string text, int totalWidth, char padChar) => text?.PadRight(totalWidth, padChar);

        public string repeating(string text, int times)
        {
            var sb = StringBuilderCache.Allocate();
            for (var i = 0; i < times; i++)
            {
                sb.Append(text);
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }
        
        public DateTime now() => DateTime.Now;
        public DateTime utcNow() => DateTime.UtcNow;

        public object @if(object returnTarget, object ifCondition) => when(returnTarget, ifCondition);
        public object when(object returnTarget, object ifCondition)
        {
            if (ifCondition is bool b && b)
                return returnTarget;

            return null;
        }

        public object ifNot(object returnTarget, object ifCondition) => unless(returnTarget, ifCondition);
        public object unless(object returnTarget, object unlessCondition)
        {
            if (unlessCondition is bool b && b)
                return null;

            return returnTarget;
        }
    }

    public class HtmlFilters : TemplateFilter
    {
        public static Dictionary<string, string> AttributeAliases { get; } = new Dictionary<string, string>
        {
            {"htmlFor", "for"},
            {"className", "class"},
        };

        public IRawString htmlencode(object obj) => StringUtils.HtmlDecode(obj?.ToString() ?? "").ToRawString();

        public IRawString tag(object obj, string tagName, Dictionary<string, object> props)
        {
            var sb = StringBuilderCache.Allocate().Append($"<{tagName}");

            if (!(obj is string) && obj is IEnumerable seq)
            {
                foreach (var item in seq)
                {
                    sb.Append(tag(item, tagName, props));
                }
            }
            else
            {
                foreach (var entry in props)
                {
                    var name = AttributeAliases.TryGetValue(entry.Key, out string alias)
                        ? alias
                        : entry.Key;

                    sb.Append($" {name}=\"{htmlencode(entry.Value)}\"");
                }
                sb.Append(obj);
                sb.Append($"</{tagName}>");
            }

            var html = StringBuilderCache.ReturnAndFree(sb);
            return html.ToRawString();
        }

        // 'a' | li({ id:'id-{name}', className:'cls'}) => <li id="the-id" class="cls">a</li>
        // todo: items | li({ id: `id-${name}`, class: "cls" })
        public IRawString div(object obj, Dictionary<string, object> props) => tag(obj, "div", props);
        public IRawString span(object obj, Dictionary<string, object> props) => tag(obj, "span", props);
        public IRawString p(object obj, Dictionary<string, object> props) => tag(obj, "p", props);
        public IRawString a(object obj, Dictionary<string, object> props) => tag(obj, "a", props);
        public IRawString li(object obj, Dictionary<string, object> props) => tag(obj, "li", props);
        public IRawString ol(object obj, Dictionary<string, object> props) => tag(obj, "ol", props);
        public IRawString img(object obj, Dictionary<string, object> props) => tag(obj, "img", props);
    }
}