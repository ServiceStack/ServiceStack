using System;

namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasUserId
	{
		Guid UserId { get; }
	}
}