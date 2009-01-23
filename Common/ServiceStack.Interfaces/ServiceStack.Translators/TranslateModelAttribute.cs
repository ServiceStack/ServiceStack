using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Translators
{
	/// <summary>
	/// This instructs the generator tool to generate translator methods for the types supplied.
	/// A {TypeName}.generated.cs partial class will be generated that contains the methods required
	/// to generate to and from that type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TranslateModelAttribute : Attribute
	{
		public ICollection<Type> ForTypes { get; set; }
		
		public TranslateModelAttribute(params Type[] forTypes)
		{
			this.ForTypes = forTypes;
		}
	}
}
