using System;

namespace ServiceStack.DataAccess
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
	public class AliasAttribute : Attribute
	{
		protected string name;

		public AliasAttribute(string name)
		{
			this.name = name;
		}
	}
}