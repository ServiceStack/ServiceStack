using ServiceStack.IntegrationTests.ServiceModel;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public class HttpPostXmlAndSecureLocalSubnetRestrictionService
		: TestServiceBase<HttpPostXmlAndSecureLocalSubnetRestriction>
	{
		protected override object Run(HttpPostXmlAndSecureLocalSubnetRestriction request)
		{
			return new HttpPostXmlAndSecureLocalSubnetRestrictionResponse();
		}
	}
}