using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@.Validation;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@.Version100.Types;
using @ServiceNamespace@.Tests.Integration.Support;

namespace @ServiceNamespace@.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class NotifyLogoutClientSessionsTests : BaseIntegrationTest
	{

		[Test]
		public void Calling_NotifyLogout_invalidates_session()
		{
			var existing@ModelName@ = this.@ModelName@s[0];
			var sessionId = GetSessionId(existing@ModelName@);

			//Make an authenticated call with a valid session id
			var requestDto = new Get@ModelName@s {
				SessionId = sessionId.ToString(),
				Ids = new ArrayOfIntId(new[] { (int)existing@ModelName@.Id }),
			};
			var responseDto = (Get@ModelName@sResponse)base.ExecuteService(requestDto);
			Assert.That(responseDto.@ModelName@s[0].Id, Is.EqualTo(existing@ModelName@.Id));

			//Invalidate the session
			var logoutDto = new NotifyLogoutClientSessions {
				@ModelName@Id = sessionId.@ModelName@Id,
				ClientSessionIds = new[] { sessionId.ClientSessionId }.ToList(),
			};
			base.ExecuteService(logoutDto);

			responseDto = (Get@ModelName@sResponse)base.ExecuteService(requestDto);
			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.EqualTo(ErrorCodes.InvalidOrExpiredSession.ToString()));
		}
	}
}