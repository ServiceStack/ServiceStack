using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@.Version100.Types;
using @ServiceNamespace@.Tests.Integration.Support;

namespace @ServiceNamespace@.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class Get@ModelName@sPortTests : IntegrationTestBase
	{
		[Test]
		public void Get_existing_customers()
		{
			var requestDto = new Get@ModelName@s { @ModelName@Ids = new ArrayOfIntId(new[] { (int)base.@ModelName@Id }) };
			var responseDto = (Get@ModelName@sResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.@ModelName@s.Count, Is.EqualTo(1));
			Assert.That(responseDto.@ModelName@s[0].Id, Is.EqualTo(base.@ModelName@Id));
		}
	}
}