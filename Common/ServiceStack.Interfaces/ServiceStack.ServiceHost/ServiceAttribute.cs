using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	public class ServiceAttribute
		: Attribute
	{
		public List<EndpointAttributes> AccessRestrictions { get; set; }
		public int? Version { get; set; }

		public ServiceAttribute(params EndpointAttributes[] restrictAccessToScenarios)
		{
			this.AccessRestrictions = new List<EndpointAttributes>(restrictAccessToScenarios);
		}

		public ServiceAttribute(int version, params EndpointAttributes[] restrictAccessScenarios)
			: this(restrictAccessScenarios)
		{
			this.Version = version;
		}
	}

}