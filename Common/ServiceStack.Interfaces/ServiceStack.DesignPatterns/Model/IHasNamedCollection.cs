using System.Collections.Generic;

namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasNamedCollection<T>
	{
		ICollection<T> this[string listId] { get; set; }
	}
}