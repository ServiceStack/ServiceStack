using System;

namespace ServiceStack.ServiceHost
{
	public class ServiceAttribute
		: Attribute
	{
		public EndpointAttributes AccessRestrictions { get; set; }
		public int? Version { get; set; }

		public ServiceAttribute(EndpointAttributes restrictAccessTo)
		{
			this.AccessRestrictions = restrictAccessTo;
		}

		public ServiceAttribute(EndpointAttributes endpointAttributes, int version)
			: this(endpointAttributes)
		{
			this.Version = version;
		}
	}

}