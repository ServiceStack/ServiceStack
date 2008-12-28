/*
// $Id$
//
// Revision      : $Revision: 699 $
// Modified Date : $LastChangedDate: 2008-12-23 15:27:40 +0000 (Tue, 23 Dec 2008) $
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Tests.Integration.Support;
using @ServiceNamespace@.Tests.Support;

namespace @ServiceNamespace@.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class Store@ModelName@PasswordTests : BaseIntegrationTest
	{
		private const string NewPassword = "newpassword";

		[Test]
		public void ValidStorePasswordTest()
		{
			Store@ModelName@Password storeDto = new Store@ModelName@Password
			{
				@ModelName@GlobalId = base.@ModelName@s[0].GlobalIdGuid,
				Base64OldEncryptedPassword = base.ServerPublicKey.EncryptData(TestData.@ModelName@Password),
				Base64NewEncryptedPassword = base.ServerPublicKey.EncryptData(NewPassword)
			};

			base.ExecuteService(storeDto);

			GetLoginAuth requestDto = new GetLoginAuth
			{
				@ModelName@Name = base.@ModelName@s[0].@ModelName@Name,
				Base64EncryptedPassword = base.ServerPublicKey.EncryptData(NewPassword),
				Base64ClientModulus = "modulus"
			};

			GetLoginAuthResponse responseDto = (GetLoginAuthResponse) base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.@ModelName@Id, Is.EqualTo(base.@ModelName@s[0].Id));
			Assert.That(responseDto.PublicSessionId, Is.Not.Null);
			Assert.That(responseDto.SecureSessionId, Is.Not.Null);
		}
	}
}