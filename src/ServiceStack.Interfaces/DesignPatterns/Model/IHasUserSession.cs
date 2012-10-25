using System;

namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasUserSession
	{
		Guid UserId { get; }

		Guid SessionId { get; }
	}
}