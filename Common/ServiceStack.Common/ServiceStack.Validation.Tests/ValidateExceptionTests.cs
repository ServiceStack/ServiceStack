using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Validation.Tests
{
	[TestFixture]
	public class ValidateExceptionTests
	{
		const string errorCode = "ThisIsAnErrorCode";
		const string errorMessage = "This is an error code";
		const string customErrorMessage = "Custom error message";

		[Test]
		public void ValidationException_with_only_ErrorCode_uses_that_as_the_message()
		{
			var exception = ValidationException.CreateException(errorCode);
			Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
			Assert.That(exception.Message, Is.EqualTo(errorCode.SplitCamelCase()));
			Assert.That(exception.ErrorMessage, Is.EqualTo(errorCode.SplitCamelCase()));
		}

		[Test]
		public void ValidationException_with_ErrorCode_and_ErrorMessage_uses_that_as_the_message()
		{
			var exception = ValidationException.CreateException(errorCode, customErrorMessage);
			Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
			Assert.That(exception.Message, Is.EqualTo(customErrorMessage));
			Assert.That(exception.ErrorMessage, Is.EqualTo(customErrorMessage));
		}

		[Test]
		public void ValidationException_using_ValidationError_with_ErrorCode()
		{
			var exception = ValidationException.CreateException(new ValidationError(errorCode));
			Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
			Assert.That(exception.Message, Is.EqualTo(errorMessage));
			Assert.That(exception.ErrorMessage, Is.EqualTo(errorMessage));
		}

		[Test]
		public void ValidationException_using_ValidationError_with_ErrorCode_FieldName_and_ErrorMessage()
		{
			var exception = ValidationException.CreateException(new ValidationError(errorCode, "fieldName", customErrorMessage));
			Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
			Assert.That(exception.Message, Is.EqualTo(customErrorMessage));
			Assert.That(exception.ErrorMessage, Is.EqualTo(customErrorMessage));
		}

		[Test]
		public void ValidationException_with_empty_ValidationResult()
		{
			var exception = new ValidationException(new ValidationResult());
			Assert.That(exception.ErrorCode, Is.Null);
			Assert.That(exception.Message, Is.Null);
			Assert.That(exception.ErrorMessage, Is.Null);
		}

		[Test]
		public void ValidationException_using_ValidationResult_with_ErrorCode()
		{
			var exception = new ValidationException(new ValidationResult(null, null, errorCode));
			Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
			Assert.That(exception.Message, Is.EqualTo(errorMessage));
			Assert.That(exception.ErrorMessage, Is.EqualTo(errorMessage));
		}

	}
}