using System.Collections.Generic;

namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasNamedList<T>
	{
		IList<T> this[string listId] { get; set; }
	}
}