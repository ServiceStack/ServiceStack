using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Validation.Tests.Support;
using ServiceStack.Validation.Validators;

namespace ServiceStack.Validation.Tests
{
	[TestFixture]
	public class ValidateSessionTests
	{
		public abstract class ModelBase
		{
			public virtual ValidationResult Validate()
			{
				return ModelValidator.ValidateObject(this);
			}
		}

		private class ExampleModel1 : ModelBase
		{
			public ExampleModel1()
			{
				this.UserId = 1;
				this.SessionId = new Guid("127716AB-4B9C-4a3f-8023-CDB13E15E347");
			}

			[NotNull]
			public int UserId { get; set; }

			[NotNull]
			public Guid SessionId { get; set; }

			[ValidSession]
			public UserSessionId UserSessionId
			{
				get { return new UserSessionId(this.UserId, this.SessionId); }
			}
		}

		[Test]
		public void Host_without_UserSessionManager_configured_should_throw_exception()
		{
			var model = new ExampleModel1();
			try
			{
				model.Validate();
				Assert.Fail("Should throw exception");
			}
			catch (Exception ex)
			{
				return;
			}
		}

	}
}