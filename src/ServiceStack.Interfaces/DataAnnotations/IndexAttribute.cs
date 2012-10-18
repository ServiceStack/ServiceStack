using System;

namespace ServiceStack.DataAnnotations
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
	public class IndexAttribute : Attribute
	{
		public IndexAttribute()
		{
		}

		public IndexAttribute(bool unique)
		{
			Unique = unique;
		}

		public bool Unique { get; set; }
	}
}