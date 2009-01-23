using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Translators
{
	/// <summary>
	/// This instructs the generator tool to generate translator extension methods for the types supplied.
	/// A {TypeName}.generated.cs static class will be generated that contains the extension methods required
	/// to generate to and from that type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class TranslateModelExtentionAttribute : Attribute
	{
		public Type FromType { get; set; }
		public Type ToType { get; set; }

		public TranslateModelExtentionAttribute(Type fromType, Type toType)
		{
			this.FromType = fromType;
			this.ToType = toType;
		}
	}
}
