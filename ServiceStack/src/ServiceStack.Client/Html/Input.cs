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
        public const string Tag = "tag";
    }

    public static Dictionary<Type, string> TypesMap { get; set; } = new()
    {
        [typeof(bool)] = Types.Checkbox,
        [typeof(DateTime)] = Types.Date,
        [typeof(DateTimeOffset)] = Types.Date,
        [typeof(TimeSpan)] = Types.Time,
        [typeof(byte)] = Types.Number,
        [typeof(short)] = Types.Number,
        [typeof(int)] = Types.Number,
        [typeof(long)] = Types.Number,
        [typeof(ushort)] = Types.Number,
        [typeof(uint)] = Types.Number,
        [typeof(ulong)] = Types.Number,
        [typeof(float)] = Types.Number,
        [typeof(double)] = Types.Number,
        [typeof(decimal)] = Types.Number,
        [typeof(string)] = Types.Text,
        [typeof(Guid)] = Types.Text,
        [typeof(Uri)] = Types.Text,
#if NET6_0_OR_GREATER
        [typeof(DateOnly)] = Types.Date,
        [typeof(TimeOnly)] = Types.Time,
#endif
    };

    static Dictionary<string, string>? typeNameMap;
    public static Dictionary<string, string> TypeNameMap => typeNameMap ??= CreateInputTypes(TypesMap);
    static Dictionary<string, string> CreateInputTypes(Dictionary<Type, string> inputTypesMap)
    {
        var to = new Dictionary<string, string>();
        foreach (var entry in inputTypesMap)
        {
            to[entry.Key.Name] = entry.Value;
        }
        return to;
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
        return Create(pi);
    }

    public static InputInfo Create(PropertyInfo pi)
    {
        InputInfo create(string id, string? type = null)
        {
            var inputAttr = pi.FirstAttribute<InputAttribute>();
            var input = inputAttr?.ToInput(c => { c.Id ??= id; c.Type ??= type; }) ?? new InputInfo(id, type);
            input.Css = pi.FirstAttribute<FieldCssAttribute>().ToCss();
            return input;
        }

        var useType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
        if (useType.IsNumericType())
            return create(pi.Name, Types.Number);
        if (useType == typeof(bool))
            return create(pi.Name, Types.Checkbox);
        if (useType == typeof(DateTime) || useType == typeof(DateTimeOffset) || useType.Name == "DateOnly")
            return create(pi.Name, Types.Date);
        if (useType == typeof(TimeSpan) || useType.Name == "TimeOnly")
            return create(pi.Name, Types.Time);

        if (useType.IsEnum)
        {
            return GetEnumEntries(useType, out var entries)
                ? X.Apply(create(pi.Name, Types.Select), x => x.AllowableEntries = entries)
                : X.Apply(create(pi.Name, Types.Select), x => x.AllowableValues = entries.Select(x => x.Value).ToArray());
        }

        return create(pi.Name);
    }

    static FieldInfo GetEnumMember(Type type, string name) => 
        (FieldInfo) type.GetMember(name, BindingFlags.Public | BindingFlags.Static)[0];

    public static KeyValuePair<string, string>[] GetEnumEntries(Type enumType)
    {
        GetEnumEntries(enumType, out var entries);
        return entries;
    }

    public static bool GetEnumEntries(Type enumType, out KeyValuePair<string, string>[] entries)
    {
        var names = Enum.GetNames(enumType);
        var to = new List<KeyValuePair<string, string>>();

        var intEnum = JsConfig.TreatEnumAsInteger || enumType.IsEnumFlags();
        var useEntries = intEnum;
        
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];

            var enumMember = GetEnumMember(enumType, name);
            var rawValue = enumMember.GetRawConstantValue();
            var value = Convert.ToInt64(rawValue).ToString();
            var enumDesc = GetDescription(enumMember);
            if (enumDesc != null)
            {
                if (!intEnum)
                    value = name;

                name = enumDesc;
                useEntries = true;
            }

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
        if (enumType is not { IsEnum: true }) return null;
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