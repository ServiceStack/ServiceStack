using ServiceStack.IntegrationTests.ServiceModel;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public class LocalhostRestrictionService
		: TestServiceBase<LocalhostRestriction>
	{
		protected override object Run(LocalhostRestriction request)
		{
			return new LocalhostRestrictionResponse();
		}
	}
}