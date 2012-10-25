using System;

namespace ServiceStack.ServiceInterface.ServiceModel
{
	public interface ICacheByDateModified
	{
		DateTime? LastModified { get; }
	}
}