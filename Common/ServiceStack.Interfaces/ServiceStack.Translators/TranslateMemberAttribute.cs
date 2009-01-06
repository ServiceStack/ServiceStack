using System;

namespace ServiceStack.Translators
{
	public class TranslateMemberAttribute : Attribute
	{
		public string PropertyName { get; set; }

		public TranslateMemberAttribute(string toPropertyName)
		{
			this.PropertyName = toPropertyName;
		}
	}
}