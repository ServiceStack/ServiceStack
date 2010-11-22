using ServiceStack.IntegrationTests.ServiceModel;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public class InternalRestrictionService
		: TestServiceBase<InternalRestriction>
	{
		protected override object Run(InternalRestriction request)
		{
			return new IntranetRestrictionResponse();
		}
	}
}