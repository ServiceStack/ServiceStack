/*
// $Id$
//
// Revision      : $Revision: 699 $
// Modified Date : $LastChangedDate: 2008-12-23 15:27:40 +0000 (Tue, 23 Dec 2008) $
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@.Validation;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Tests.Integration.Support;
using @ServiceNamespace@.Tests.Support;

namespace @ServiceNamespace@.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class GetLoginAuthPortTests : BaseIntegrationTest
	{
		[Test]
		public void ValidLoginTest()
		{
			GetLoginAuthResponse responseDto = this.ExecutePort(
				base.@ModelName@s[0].@ModelName@Name,
				base.ServerPublicKey.EncryptData(TestData.@ModelName@Password),
				"modulus");

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.@ModelName@Id, Is.EqualTo(base.@ModelName@s[0].Id));
			Assert.That(responseDto.PublicSessionId, Is.Not.Null);
			Assert.That(responseDto.SecureSessionId, Is.Not.Null);
		}

		[Test]
		public void Invalid@ModelName@NameTest()
		{
			GetLoginAuthResponse responseDto = this.ExecutePort(
				"invalidusername",
				base.ServerPublicKey.EncryptData(TestData.@ModelName@Password),
				"modulus");

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.EqualTo(ErrorCodes.Invalid@ModelName@OrPassword.ToString()));
			Assert.That(responseDto.@ModelName@Id, Is.EqualTo(default(int)));
			Assert.That(responseDto.PublicSessionId, Is.EqualTo(default(Guid)));
			Assert.That(responseDto.SecureSessionId, Is.EqualTo(default(Guid)));
		}

		[Test]
		public void Invalid@ModelName@PasswordTest()
		{
			GetLoginAuthResponse responseDto = this.ExecutePort(
				base.@ModelName@s[0].@ModelName@Name,
				base.ServerPublicKey.EncryptData("invalidpassword"),
				"modulus");

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.EqualTo(ErrorCodes.Invalid@ModelName@OrPassword.ToString()));
			Assert.That(responseDto.@ModelName@Id, Is.EqualTo(default(int)));
			Assert.That(responseDto.PublicSessionId, Is.EqualTo(default(Guid)));
			Assert.That(responseDto.SecureSessionId, Is.EqualTo(default(Guid)));
		}

		[Test]
		public void Unencrypted@ModelName@PasswordTest()
		{
			GetLoginAuthResponse responseDto = this.ExecutePort(
				base.@ModelName@s[0].@ModelName@Name,
				"unencryptedandinvalidpassword",
				"modulus");

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.EqualTo(ErrorCodes.Invalid@ModelName@OrPassword.ToString()));
			Assert.That(responseDto.@ModelName@Id, Is.EqualTo(default(int)));
			Assert.That(responseDto.PublicSessionId, Is.EqualTo(default(Guid)));
			Assert.That(responseDto.SecureSessionId, Is.EqualTo(default(Guid)));
		}

		private GetLoginAuthResponse ExecutePort(string userName, string base64EncryptedPassword, string base64ClientModulus)
		{
			GetLoginAuth requestDto = new GetLoginAuth
			{
				@ModelName@Name = userName,
				Base64EncryptedPassword = base64EncryptedPassword,
				Base64ClientModulus = base64ClientModulus
			};

			return (GetLoginAuthResponse)base.ExecuteService(requestDto);
		}
	}
}