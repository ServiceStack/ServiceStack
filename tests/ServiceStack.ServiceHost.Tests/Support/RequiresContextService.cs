using System;
using System.Runtime.Serialization;
using ServiceStack.Server;

namespace ServiceStack.ServiceHost.Tests.Support
{
	[DataContract]
	public class RequiresContext { }

	[DataContract]
	public class RequiresContextResponse { }

	public class RequiresContextService 
		: IService, IRequiresRequestContext
	{
		public IRequestContext RequestContext { get;  set; }

		public object Any(RequiresContext requires)
		{
			if (RequestContext == null)
				throw new ArgumentNullException("RequestContext");
	
			return new RequiresContextResponse();
		}
	}
}