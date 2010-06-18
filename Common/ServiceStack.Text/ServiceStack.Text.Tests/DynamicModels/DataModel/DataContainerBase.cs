using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Text.Tests.DynamicModels.DataModel
{
	[Serializable]
	public abstract class DataContainerBase
	{
		public Guid Identifier { get; set; }

	}
}