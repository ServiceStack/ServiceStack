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
	public class TranslateModelExtensionAttribute : Attribute
	{
		public string FromExtensionPrefix { get; set; }
		public string ToExtensionPrefix { get; set; }
		public Type FromType { get; set; }
		public Type ToType { get; set; }

		public TranslateModelExtensionAttribute(Type fromType, Type toType)
		{
			this.FromType = fromType;
			this.ToType = toType;
		}

		public TranslateModelExtensionAttribute(Type fromType, string fromExtensionPrefix, Type toType, string toExtensionPrefix)
		{
			this.FromType = fromType;
			this.FromExtensionPrefix = fromExtensionPrefix;
			this.ToType = toType;
			this.ToExtensionPrefix = toExtensionPrefix;
		}
	}
}
