using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.IntegrationTests.ServiceModel;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public class HttpPostXmlOrSecureLocalSubnetRestrictionService
		: TestServiceBase<HttpPostXmlOrSecureLocalSubnetRestriction>
	{
		protected override object Run(HttpPostXmlOrSecureLocalSubnetRestriction request)
		{
			return new HttpPostXmlOrSecureLocalSubnetRestrictionResponse();
		}
	}

}
