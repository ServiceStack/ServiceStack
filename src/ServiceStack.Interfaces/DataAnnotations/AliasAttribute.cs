using System;

namespace ServiceStack.DataAnnotations
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
	public class AliasAttribute : Attribute
	{
		public string Name { get; set; }

		public AliasAttribute(string name)
		{
			this.Name = name;
		}
	}
}