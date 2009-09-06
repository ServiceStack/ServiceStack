using System;
using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
	public class IndexAttribute : Attribute
	{
		public IndexAttribute()
		{
			this.FieldNames = new List<string>();
		}

		public IndexAttribute(bool unique)
		{
			Unique = unique;
		}

		public IndexAttribute(params string[] fieldNames)
		{
			this.FieldNames = new List<string>(fieldNames);
		}

		public IndexAttribute(bool unique, params string[] fieldNames)
		{
			this.Unique = unique;
			this.FieldNames = new List<string>(fieldNames);
		}

		public List<string> FieldNames { get; set; }

		public bool Unique { get; set; }
	}
}