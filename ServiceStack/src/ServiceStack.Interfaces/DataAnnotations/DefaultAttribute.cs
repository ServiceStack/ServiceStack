using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Create RDBMS Column definition with specified default value
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DefaultAttribute : AttributeBase
{
    public int IntValue { get; set; }
    public double DoubleValue { get; set; }

    public Type DefaultType { get; set; }
    public string DefaultValue { get; set; }

    public bool OnUpdate { get; set; }

    public DefaultAttribute(int intValue)
    {
        this.IntValue = intValue;
        this.DefaultType = typeof(int);
        this.DefaultValue = this.IntValue.ToString();
    }

    public DefaultAttribute(double doubleValue)
    {
        this.DoubleValue = doubleValue;
        this.DefaultType = typeof(double);
        this.DefaultValue = doubleValue.ToString();
    }

    public DefaultAttribute(string defaultValue)
    {
        this.DefaultType = typeof(string);
        this.DefaultValue = defaultValue;
    }

    public DefaultAttribute(Type defaultType, string defaultValue)
    {
        this.DefaultValue = defaultValue;
        this.DefaultType = defaultType;
    }
}
