using System;
using System.Collections.ObjectModel;

namespace ServiceStack.Html
{
#if !NETSTANDARD2_0
    [Serializable]
#endif
	public class ModelErrorCollection : Collection<ModelError>
	{

		public void Add(Exception exception)
		{
			Add(new ModelError(exception));
		}

		public void Add(string errorMessage)
		{
			Add(new ModelError(errorMessage));
		}
	}
}
