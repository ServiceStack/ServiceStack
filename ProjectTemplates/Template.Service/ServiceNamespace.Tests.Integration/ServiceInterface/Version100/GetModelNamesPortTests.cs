/*
// $Id: Get@ModelName@sPortTests.cs 675 2008-12-22 18:39:43Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 675 $
// Modified Date : $LastChangedDate: 2008-12-22 18:39:43 +0000 (Mon, 22 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@.Validation;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@.Version100.Types;
using @ServiceNamespace@.Tests.Integration.Support;

namespace @ServiceNamespace@.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class Get@ModelName@sPortTests : BaseIntegrationTest
	{
		[Test]
		public void Get_users_without_token_fails()
		{
			var existing@ModelName@ = this.@ModelName@s[0];
			var requestDto = new Get@ModelName@s { Ids = new ArrayOfIntId(new[] { (int)existing@ModelName@.Id }) };
			var responseDto = (Get@ModelName@sResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.EqualTo(ErrorCodes.InvalidOrExpiredSession.ToString()));
		}

		[Test]
		public void Get_users_with_token_getting_their_own_info()
		{
			var existing@ModelName@ = this.@ModelName@s[0];
			var sessionId = GetSessionId(existing@ModelName@);

			var requestDto = new Get@ModelName@s {
				SessionId = sessionId.ToString(),
				Ids = new ArrayOfIntId(new[] { (int)existing@ModelName@.Id }),
			};
			var responseDto = (Get@ModelName@sResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.@ModelName@s.Count, Is.EqualTo(1));
			Assert.That(responseDto.@ModelName@s[0].Id, Is.EqualTo(existing@ModelName@.Id));
		}

		/// <summary>
		/// Authenticated requests such as Get@ModelName@s will only return their own results.
		/// Requesting another users ids will return an empty result set.
		/// </summary>
		[Test]
		public void Get_users_with_token_getting_their_someone_elses_info()
		{
			var existing@ModelName@ = this.@ModelName@s[0];
			var another@ModelName@ = this.@ModelName@s[1];
			var sessionId = GetSessionId(existing@ModelName@);

			var requestDto = new Get@ModelName@s {
				SessionId = sessionId.ToString(),
				Ids = new ArrayOfIntId(new[] { (int)another@ModelName@.Id }),
			};
			var responseDto = (Get@ModelName@sResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.@ModelName@s.Count, Is.EqualTo(0));
		}

		/// <summary>
		/// Authenticated requests such as Get@ModelName@s will only return their own results.
		/// Asking for multiple users ids will only return theirs.
		/// </summary>
		[Test]
		public void Get_users_with_token_getting_a_lot_of_users_info_only_returns_theirs()
		{
			var existing@ModelName@ = this.@ModelName@s[0];
			var all@ModelName@Ids = this.@ModelName@s.ConvertAll(x => (int)x.Id);
			var sessionId = GetSessionId(existing@ModelName@);

			var requestDto = new Get@ModelName@s {
				SessionId = sessionId.ToString(),
				Ids = new ArrayOfIntId(all@ModelName@Ids),
			};
			var responseDto = (Get@ModelName@sResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.@ModelName@s.Count, Is.EqualTo(1));
		}
	}
}