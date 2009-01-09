using System.Collections.Generic;
using ServiceStack.Common.Extensions;
using ServiceStack.DesignPatterns.Translator;
using ServiceStack.Validation;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceNamespace@.ServiceInterface.Version100.Translators
{
	public class ResponseStatusTranslator : ITranslator<ResponseStatus, ValidationResult>
	{
		public static readonly ResponseStatusTranslator Instance = new ResponseStatusTranslator();

		public ResponseStatus Parse(ValidationException validationException)
		{
			return CreateErrorResponse(validationException.ErrorCode, validationException.Message, validationException.Violations);
		}

		public ResponseStatus Parse(ValidationResult validationResult)
		{
			return validationResult.IsValid
						? CreateSuccessResponse(validationResult.SuccessMessage)
						: CreateErrorResponse(validationResult.ErrorCode, validationResult.ErrorMessage, validationResult.Errors);
		}

		public static ResponseStatus CreateSuccessResponse(string message)
		{
			return new ResponseStatus { Message = message };
		}

		public static ResponseStatus CreateErrorResponse(string errorCode)
		{
			var errorMessage = errorCode.SplitCamelCase();
			return CreateErrorResponse(errorCode, errorMessage, null);
		}

		public static ResponseStatus CreateErrorResponse(string errorCode, string errorMessage)
		{
			return CreateErrorResponse(errorCode, errorMessage, null);
		}

		public static ResponseStatus CreateErrorResponse(string errorCode, string errorMessage, IEnumerable<ValidationError> validationErrors)
		{
			var to = new ResponseStatus {
				ErrorCode = errorCode,
				Message = errorMessage,
			};
			if (validationErrors != null)
			{
				foreach (var validationError in validationErrors)
				{
					var error = new ResponseError {
						ErrorCode = validationError.ErrorCode,
						FieldName = validationError.FieldName,
						Message = validationError.ErrorMessage,
					};
					to.Errors.Add(error);
				}
			}
			return to;
		}
	}
}