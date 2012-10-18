using System;

namespace ServiceStack.Translators
{
	/// <summary>
	/// This instructs the generator tool to generate translator extension methods for the types supplied.
	/// A {TypeName}.generated.cs static class will be generated that contains the extension methods required
	/// to generate to and from that type.
	/// 
	/// The source type is what the type the attribute is decorated on which can only be resolved at runtime.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class TranslateExtensionAttribute : TranslateAttribute
	{

		public TranslateExtensionAttribute(Type sourceType, Type targetType)
			: base(sourceType, targetType) {}

		public TranslateExtensionAttribute(Type sourceType, string sourceExtensionPrefix, Type targetType, string targetExtensionPrefix)
			:base(sourceType, sourceExtensionPrefix, targetType, targetExtensionPrefix) {}
	}

}
