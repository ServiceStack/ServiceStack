using System;

namespace ServiceStack.DataAnnotations
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class DefaultAttribute : Attribute
	{
		public int IntValue { get; set; }
		public double DoubleValue { get; set; }

		public Type DefaultType { get; set; }
		public string DefaultValue { get; set; }

		public DefaultAttribute(int intValue)
		{
			this.IntValue = intValue;
		}

		public DefaultAttribute(double doubleValue)
		{
			this.DoubleValue = doubleValue;
		}

		public DefaultAttribute(Type defaultType, string defaultValue)
		{
			this.DefaultValue = defaultValue;
			this.DefaultType = defaultType;
		}
	}
}