using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DefaultAttribute : AttributeBase
    {
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }

        public Type DefaultType { get; set; }
        public string DefaultValue { get; set; }

        public bool UseOnUpdate { get; set; }

        public DefaultAttribute(int intValue)
        {
            this.UseOnUpdate = false;
            this.IntValue = intValue;
            this.DefaultType = typeof(int);
            this.DefaultValue = this.IntValue.ToString();
        }

        public DefaultAttribute(double doubleValue)
        {
            this.UseOnUpdate = false;
            this.DoubleValue = doubleValue;
            this.DefaultType = typeof(double);
            this.DefaultValue = doubleValue.ToString();
        }

        public DefaultAttribute(string defaultValue)
        {
            this.UseOnUpdate = false;
            this.DefaultType = typeof(string);
            this.DefaultValue = defaultValue;
        }

        public DefaultAttribute(Type defaultType, string defaultValue)
        {
            this.UseOnUpdate = false;
            this.DefaultValue = defaultValue;
            this.DefaultType = defaultType;
        }
    }
}