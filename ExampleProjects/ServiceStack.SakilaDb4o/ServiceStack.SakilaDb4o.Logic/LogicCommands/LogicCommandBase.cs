using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Validation;
using ServiceStack.DataAccess;

namespace ServiceStack.SakilaDb4o.Logic.LogicCommands
{
	public abstract class LogicCommandBase<ReturnType> : IAction<ReturnType>, IValidatableCommand<ReturnType>
	{
		protected ILog log;

		public IOperationContext AppContext { get; set; }

		public IPersistenceProvider Provider { get; set; }

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