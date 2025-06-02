#nullable enable

using System;

namespace ServiceStack;

/// <summary>
/// Document the allowable values for an API Property
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ApiAllowableValuesAttribute : AttributeBase
{
    public ApiAllowableValuesAttribute() {}
    public ApiAllowableValuesAttribute(string? name) => Name = name;

    public ApiAllowableValuesAttribute(string name, int min, int max) : this(name)
    {
        Type = "RANGE";
        Min = min;
        Max = max;
    }
    public ApiAllowableValuesAttribute(int min, int max) : this(null, min, max) {}

    public ApiAllowableValuesAttribute(string name, params string[] values)
        : this(name)
    {
        Type = "LIST";
        Values = values;
    }
    public ApiAllowableValuesAttribute(string[] values) : this(null, values) {}

    public ApiAllowableValuesAttribute(string? name, Type enumType)
        : this(name)
    {
        if (enumType.IsEnum)
        {
            Type = "LIST";
            Values = Enum.GetNames(enumType);
        }
    }
    public ApiAllowableValuesAttribute(Type enumType) : this(null, enumType) {}

    public ApiAllowableValuesAttribute(string name, Func<string[]>? listAction)
        : this(name)
    {
        if (listAction != null)
        {
            Type = "LIST";
            Values = listAction();
        }
    }
    public ApiAllowableValuesAttribute(Func<string[]> listAction) : this(null, listAction){}

    /// <summary>
    /// Gets or sets parameter name with which allowable values will be associated.
    /// </summary>
    public string? Name { get; set; }

    public string? Type { get; set; }

    public int? Min { get; set; }

    public int? Max { get; set; }

    public string[]? Values { get; set; }

    //TODO: should be implemented according to:
    //https://github.com/wordnik/swagger-core/wiki/datatypes
}