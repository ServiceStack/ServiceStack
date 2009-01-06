using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Translators
{
	public class TranslateModelAttribute : Attribute
	{
		public ICollection<Type> ForTypes { get; set; }
		
		public TranslateModelAttribute(params Type[] forTypes)
		{
			this.ForTypes = forTypes;
		}
	}
}
