using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    public class TemplateDefaultFilters : TemplateFilter
    {
        public static Dictionary<string, string> AttributeAliases { get; } = new Dictionary<string, string>
        {
            {"htmlFor", "for"},
            {"className", "class"},
        };

        public IRawString raw(object value) => value.ToString().ToRawString();

        public IRawString json(object value)
        {
            var json = value.ToJson() ?? "null";
            return json.ToRawString();
        }

        public string appSetting(string name) => 
            Context.AppSettings.GetString(name);

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

        public IRawString htmlencode(object obj) => StringUtils.HtmlDecode(obj?.ToString() ?? "").ToRawString();

        public string currency(decimal decimalValue) => currency(decimalValue, null); //required to support 1/2 vars

        public string currency(decimal decimalValue, string culture)
        {
            var cultureInfo = culture != null
                ? new CultureInfo(culture)
                : (CultureInfo) Context.Args[TemplateConstants.DefaultCulture];

            var fmt = string.Format(cultureInfo, "{0:C}", decimalValue);
            return fmt;
        }
    }
}