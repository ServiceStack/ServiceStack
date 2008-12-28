/*
// $Id$
//
// Revision      : $Revision: 695 $
// Modified Date : $LastChangedDate: 2008-12-23 14:34:01 +0000 (Tue, 23 Dec 2008) $
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using Ddn.Common.Services.Service;
using Ddn.Common.Testing;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@.@ServiceName@;
using @DomainModelNamespace@.Validation;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Tests.Support;
using @ServiceNamespace@.ServiceInterface.Version100;
using DtoOps = @ServiceModelNamespace@.Version100.Operations.@ServiceName@;

namespace @ServiceNamespace@.Tests.ServiceInterface.Version100
{
	[TestFixture]
	public class GetLoginAuthTest : BaseAppTestFixture
	{
		public GetLoginAuthTest()
			: base(new TestParameters())
		{
		}

		private Mock<I@ServiceName@Facade> MoqFacade { get; set; }
		private CallContext CallContext { get; set; }
		private LoginAuth LoginAuthSuccess { get; set; }

		[SetUp]
		public void SetUp()
		{
			this.MoqFacade = new Mock<I@ServiceName@Facade>();

			this.CallContext = base.CreateCallContext(this.MoqFacade.Object, null);

			this.LoginAuthSuccess = new LoginAuth
			{
				@ModelName@Id = default(int),
                PublicSessionId = Guid.NewGuid(),
				SecureSessionId = Guid.NewGuid(),
				ServerTime = DateTime.UtcNow,
			};
		}

		private GetLoginAuthResponse ExecutePort(string userName, string base64EncryptedPassword, string base64ClientModulus, LoginAuth returnValue)
		{
			// Create request DTO and insert into call context
			this.CallContext.Request.Dto = new DtoOps.GetLoginAuth
			{
				@ModelName@Name = userName,
				Base64EncryptedPassword = base64EncryptedPassword,
				Base64ClientModulus = base64ClientModulus,
			};

			// Set facade to expect provided values
			this.MoqFacade.Expect(facade => facade.GetLoginAuth(userName, base64EncryptedPassword, base64ClientModulus))
				.Returns(returnValue)
				.AtMostOnce();

			// Execute port
			return (DtoOps.GetLoginAuthResponse) new GetLoginAuthPort().Execute(this.CallContext);
		}

		[Test]
		public void LoginSuccessExecute()
		{
			GetLoginAuthResponse response = ExecutePort("username", "encryptedpassword", "modulus", this.LoginAuthSuccess);

			this.MoqFacade.VerifyAll();

			Assert.That(response.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(response.ResponseStatus.Message, Is.EqualTo(MessageCodes.LoginWasSuccessful.ToString()));
			Assert.That(response.@ModelName@Id, Is.EqualTo(this.LoginAuthSuccess.@ModelName@Id));
		}

		[Test]
		public void Invalid@ModelName@NameExecute()
		{
			GetLoginAuthResponse response = ExecutePort("invalidusername", "encryptedpassword", "modulus", null);

			this.MoqFacade.VerifyAll();

			Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(ErrorCodes.Invalid@ModelName@OrPassword.ToString()));
			Assert.That(response.@ModelName@Id, Is.EqualTo(default(int)));
			Assert.That(response.PublicSessionId, Is.EqualTo(default(Guid)));
		}

		[Test]
		public void InvalidPasswordExecute()
		{
			GetLoginAuthResponse response = ExecutePort("username", "invalidencryptedpassword", "modulus", null);

			this.MoqFacade.VerifyAll();

			Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(ErrorCodes.Invalid@ModelName@OrPassword.ToString()));
			Assert.That(response.@ModelName@Id, Is.EqualTo(default(int)));
			Assert.That(response.PublicSessionId, Is.EqualTo(default(Guid)));
		}
	}
}