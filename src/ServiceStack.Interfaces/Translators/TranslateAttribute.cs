using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Translators
{
	/// <summary>
	/// This instructs the generator tool to generate translator methods for the types supplied.
	/// A {TypeName}.generated.cs partial class will be generated that contains the methods required
	/// to generate to and from that type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TranslateAttribute : Attribute
	{
		public string SourceMethodPrefix { get; set; }
		public string TargetMethodPrefix { get; set; }
		public Type SourceType { get; set; }
		public Type TargetType { get; set; }

		public TranslateAttribute(Type targetType) 
			: this(null, targetType) {}

		public TranslateAttribute(string sourceExtensionPrefix, Type targetType, string targetExtensionPrefix)
			: this(null, sourceExtensionPrefix, targetType, targetExtensionPrefix) { }

		protected TranslateAttribute(Type sourceType, Type targetType)
		{
			this.SourceType = sourceType;
			this.TargetType = targetType;
		}

		protected TranslateAttribute(Type sourceType, string sourceExtensionPrefix, Type targetType, string targetExtensionPrefix)
		{
			this.SourceType = sourceType;
			this.SourceMethodPrefix = sourceExtensionPrefix;
			this.TargetType = targetType;
			this.TargetMethodPrefix = targetExtensionPrefix;
		}
	}
}
