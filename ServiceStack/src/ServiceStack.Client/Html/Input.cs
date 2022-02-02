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
        var useType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
        if (useType.IsNumericType())
            return new InputInfo(pi.Name, Types.Number);
        if (useType == typeof(bool))
            return new InputInfo(pi.Name, Types.Checkbox);
        if (useType == typeof(DateTime) || useType == typeof(DateTimeOffset) || useType.Name == "DateOnly")
            return new InputInfo(pi.Name, Types.Date);
        if (useType == typeof(TimeSpan) || useType.Name == "TimeOnly")
            return new InputInfo(pi.Name, Types.Time);

        if (useType.IsEnum)
        {
            return GetEnumEntries(useType, out var entries)
                ? new InputInfo(pi.Name, Types.Select) {
                    AllowableEntries = entries
                }
                : new InputInfo(pi.Name, Types.Select) {
                    AllowableValues = entries.Select(x => x.Value).ToArray()
                };
        }

        return new InputInfo(pi.Name);
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
}