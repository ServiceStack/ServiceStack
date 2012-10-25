using System.Collections.Generic;

namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasNamedCollection<T> : IHasNamed<ICollection<T>>
	{
	}
}