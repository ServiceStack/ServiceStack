using System;

namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasId
	{
		Guid Id { get; }
	}
}