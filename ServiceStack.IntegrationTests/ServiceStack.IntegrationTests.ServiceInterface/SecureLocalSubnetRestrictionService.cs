using ServiceStack.IntegrationTests.ServiceModel;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public class SecureLocalSubnetRestrictionService
		: TestServiceBase<SecureLocalSubnetRestriction>
	{
		protected override object Run(SecureLocalSubnetRestriction request)
		{
			return new SecureLocalSubnetRestrictionResponse();
		}
	}
}