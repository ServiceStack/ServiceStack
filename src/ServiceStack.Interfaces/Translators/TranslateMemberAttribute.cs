using System;

namespace ServiceStack.Translators
{
	/// <summary>
	/// This changes the default behaviour for the 
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class TranslateMemberAttribute : Attribute
	{
		public string PropertyName { get; set; }

		public TranslateMemberAttribute(string toPropertyName)
		{
			this.PropertyName = toPropertyName;
		}
	}
}