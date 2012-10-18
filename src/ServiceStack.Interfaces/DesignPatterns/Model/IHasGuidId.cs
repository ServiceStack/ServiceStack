using System;

namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasGuidId : IHasId<Guid>
	{
	}
}