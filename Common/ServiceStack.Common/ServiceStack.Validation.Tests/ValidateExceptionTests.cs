using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Validation.Tests
{
	[TestFixture]
	public class ValidateExceptionTests
	{

		[Test]
		public void ValidationException_with_only_ErrorCode_uses_that_as_the_message()
		{
			var errorCode = "ThisIsAnErrorCode";
			var exception = ValidationException.CreateException(errorCode);
			Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
			Assert.That(exception.Message, Is.EqualTo(errorCode.SplitCamelCase()));
			Assert.That(exception.ErrorMessage, Is.EqualTo(errorCode.SplitCamelCase()));
		}

		[Test]
		public void ValidationException_with_ErrorCode_and_ErrorMessage_uses_that_as_the_message()
		{
			var errorCode = "ThisIsAnErrorCode";
			var errorMessage = "Custom error message";
			var exception = ValidationException.CreateException(errorCode, errorMessage);
			Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
			Assert.That(exception.Message, Is.EqualTo(errorMessage));
			Assert.That(exception.ErrorMessage, Is.EqualTo(errorMessage));
		}

	}
}