using System;

namespace ServiceStack.DataAnnotations
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
	public class ReferencesAttribute : Attribute
	{
		public Type Type { get; set; }

		public ReferencesAttribute(Type type)
		{
			this.Type = type;
		}
	}
}