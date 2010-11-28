using System;
using System.Runtime.Serialization;

namespace ServiceStack.ServiceHost.Tests.Support
{
	[DataContract]
	public class RequiresContext { }

	[DataContract]
	public class RequiresContextResponse { }

	public class RequiresContextService 
		: IService<RequiresContext>, IRequiresRequestContext
	{
		public IRequestContext RequestContext { get;  set; }

		public object Execute(RequiresContext requires)
		{
			if (RequestContext == null)
				throw new ArgumentNullException("RequestContext");
	
			return new RequiresContextResponse();
		}
	}
}