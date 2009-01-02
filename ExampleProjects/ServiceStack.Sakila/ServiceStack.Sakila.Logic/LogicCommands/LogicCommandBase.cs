using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Sakila.DataAccess;
using ServiceStack.Validation;

namespace ServiceStack.Sakila.Logic.LogicCommands
{
	public abstract class LogicCommandBase<ReturnType> : IAction<ReturnType>, IValidatableCommand<ReturnType>
	{
		protected ILog log;

		public IOperationContext AppContext { get; set; }

		public SakilaServiceDataAccessProvider Provider { get; set; }

		protected void ThrowAnyValidationErrors(ValidationResult validationResult)
		{
			var hasErrors = false;
			foreach (var validationError in validationResult.Errors)
			{
				hasErrors = true;
				validationError.ErrorMessage = this.AppContext.Resources.GetString(validationError.ErrorCode);
			}
			if (hasErrors)
			{
				throw new ValidationException(validationResult);
			}
		}

		public abstract ReturnType Execute();

		public virtual ValidationResult Validate()
		{
			return new ValidationResult();
		}

	}
}