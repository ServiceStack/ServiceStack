using System;
using System.Collections.Generic;

namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasNamedList<T> : IHasNamed<IList<T>>
	{
	}
}