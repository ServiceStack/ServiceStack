using ServiceStack.IntegrationTests.ServiceModel;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public class LocalSubnetRestrictionService
		: TestServiceBase<LocalSubnetRestriction>
	{
		protected override object Run(LocalSubnetRestriction request)
		{
			return new LocalSubnetRestrictionResponse();
		}
	}
}