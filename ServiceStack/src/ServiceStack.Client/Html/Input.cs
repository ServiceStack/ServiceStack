#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.Text;

namespace ServiceStack.Html;

public static class Input
{
    public static class Types
    {
        public const string Text = "text";
        public const string Checkbox = "checkbox";
        public const string Color = "color";
        public const string Date = "date";
        public const string DatetimeLocal = "datetime-local";
        public const string Email = "email";
        public const string File = "file";
        public const string Hidden = "hidden";
        public const string Image = "image";
        public const string Month = "month";
        public const string Number = "number";
        public const string Password = "password";
        public const string Radio = "radio";
        public const string Range = "range";
        public const string Reset = "reset";
        public const string Search = "search";
        public const string Submit = "submit";
        public const string Tel = "tel";
        public const string Time = "time";
        public const string Url = "url";
        public const string Week = "week";
        public const string Select = "select";
        public const string Textarea = "textarea";
    }

    public class ConfigureCss
    {
        public ConfigureCss(InputInfo input)
        {
            Input = input;
            Input.Css ??= new FieldCss();
        }

        string ColSpan(int n) => "col-span-" + n switch
        {
            1 => "12",
            2 => "6",
            3 => "4",
            4 => "3",
            6 => "2",
            12 => "1",
            _ => throw new ArgumentException("Supported fields per row: 1, 2, 3, 4, 6, 12")
        };

        public ConfigureCss FieldsPerRow(int sm, int? md=null, int? lg=null, int? xl=null, int? xl2=null)
        {
            var cls = new List<string> { "col-span-12", "sm:" + ColSpan(sm) };
            if (md != null) cls.Add("md:" + ColSpan(md.Value));
            if (lg != null) cls.Add("lg:" + ColSpan(lg.Value));
            if (xl != null) cls.Add("xl:" + ColSpan(xl.Value));
            if (xl2 != null) cls.Add("2xl:" + ColSpan(xl2.Value));
            Input.Css.Field = string.Join(" ", cls);
            return this;
        }

        public InputInfo Input { get; }
    }
    
    public static InputInfo FieldsPerRow(this InputInfo input, 
        int sm, int? md = null, int? lg = null, int? xl = null, int? xl2 = null) =>
        new ConfigureCss(input).FieldsPerRow(sm, md, lg, xl, xl2).Input;

    public static InputInfo AddCss(this InputInfo input, Action<ConfigureCss> configure)
    {
        configure(new ConfigureCss(input));
        return input;
    }

    public static InputInfo For<TModel>(Expression<Func<TModel, object?>> expr, Action<InputInfo> configure)
    {
        var ret = For(expr);
        configure(ret);
        return ret;
    }

    public static InputInfo For<TModel>(Expression<Func<TModel, object?>> expr)
    {
        var pi = InspectUtils.PropertyFromExpression(expr) 
            ?? throw new Exception($"Could not resolve property expression from {expr}");
        var css = pi.FirstAttribute<FieldCssAttribute>().ToCss();
        var useType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
        if (useType.IsNumericType())
            return new InputInfo(pi.Name, Types.Number) { Css = css };
        if (useType == typeof(bool))
            return new InputInfo(pi.Name, Types.Checkbox) { Css = css };
        if (useType == typeof(DateTime) || useType == typeof(DateTimeOffset) || useType.Name == "DateOnly")
            return new InputInfo(pi.Name, Types.Date) { Css = css };
        if (useType == typeof(TimeSpan) || useType.Name == "TimeOnly")
            return new InputInfo(pi.Name, Types.Time) { Css = css };

        if (useType.IsEnum)
        {
            return GetEnumEntries(useType, out var entries)
                ? new InputInfo(pi.Name, Types.Select) {
                    Css = css,
                    AllowableEntries = entries
                }
                : new InputInfo(pi.Name, Types.Select) {
                    Css = css,
                    AllowableValues = entries.Select(x => x.Value).ToArray()
                };
        }
        
        return new InputInfo(pi.Name) { Css = css };
    }

    static FieldInfo GetEnumMember(Type type, string name) => 
        (FieldInfo) type.GetMember(name, BindingFlags.Public | BindingFlags.Static)[0];

    public static bool GetEnumEntries(Type enumType, out KeyValuePair<string, string>[] entries)
    {
        var names = Enum.GetNames(enumType);
        var to = new List<KeyValuePair<string, string>>();

        var useEntries = JsConfig.TreatEnumAsInteger || enumType.IsEnumFlags();
        
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];

            var enumMember = GetEnumMember(enumType, name);
            var enumDesc = GetDescription(enumMember);
            if (enumDesc != null)
            {
                name = enumDesc;
                useEntries = true;
            }

            var rawValue = enumMember.GetRawConstantValue();
            var value = Convert.ToInt64(rawValue).ToString();

            var enumAttr = enumMember.FirstAttribute<EnumMemberAttribute>()?.Value;
            if (enumAttr != null)
            {
                name = enumAttr;
                useEntries = true;
            }

            to.Add(new KeyValuePair<string, string>(value, name));
        }

        entries = to.ToArray();
        return useEntries;
    }

    public static string[]? GetEnumValues(Type enumType)
    {
        if (!enumType.IsEnum) return null;
        GetEnumEntries(enumType, out var entries);
        return entries.Select(x => x.Value).ToArray();
    }

    public static string? GetDescription(MemberInfo mi)
    {
        var apiAttr = mi.FirstAttribute<ApiMemberAttribute>();
        if (apiAttr != null)
            return apiAttr.Description;

        var componentDescAttr = mi.FirstAttribute<System.ComponentModel.DescriptionAttribute>();
        if (componentDescAttr != null)
            return componentDescAttr.Description;

        var ssDescAttr = mi.FirstAttribute<ServiceStack.DataAnnotations.DescriptionAttribute>();
        return ssDescAttr?.Description;
    }

    /// <summary>
    /// Convert from Grid Matrix of Inputs into a flat List of Inputs with configured grid classes
    /// </summary>
    /// <param name="gridLayout"></param>
    /// <returns></returns>
    public static List<InputInfo> FromGridLayout(IEnumerable<List<InputInfo>> gridLayout)
    {
        var to = new List<InputInfo>();
        foreach (var inputs in gridLayout)
        {
            if (inputs.Count == 0) continue;
            if (inputs.Count == 1)
            {
                to.Add(inputs[0]);
                continue;
            }
            foreach (var input in inputs)
            {
                to.Add(input.FieldsPerRow(inputs.Count));
            }
        }
        return to;
    }
}