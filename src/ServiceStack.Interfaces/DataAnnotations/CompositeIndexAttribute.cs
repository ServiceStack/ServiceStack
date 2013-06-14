using System;
using System.Collections.Generic;

namespace ServiceStack.DataAnnotations
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class CompositeIndexAttribute : Attribute
	{
		public CompositeIndexAttribute()
		{
			this.FieldNames = new List<string>();
		}

		public CompositeIndexAttribute(params string[] fieldNames)
		{
			this.FieldNames = new List<string>(fieldNames);
		}

		public CompositeIndexAttribute(bool unique, params string[] fieldNames)
		{
			this.Unique = unique;
			this.FieldNames = new List<string>(fieldNames);
		}

		public List<string> FieldNames { get; set; }

		public bool Unique { get; set; }

	    public string Name { get; set; }
	}
}