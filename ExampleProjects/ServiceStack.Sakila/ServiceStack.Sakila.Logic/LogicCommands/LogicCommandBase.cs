using ServiceStack.Logging;
using ServiceStack.Sakila.DataAccess;
using ServiceStack.ServiceInterface;
using ServiceStack.Validation;

namespace ServiceStack.Sakila.Logic.LogicCommands
{
	public abstract class LogicCommandBase<ReturnType> : IAction<ReturnType>, IValidatableCommand<ReturnType>
	{
		private AppContext appContext;
		protected ILog log;

		public AppContext AppContext
		{
			get { return this.appContext; }
			set
			{
				this.appContext = value;
				this.log = this.appContext.LogFactory.GetLogger(GetType());
			}
		}

		public SakilaServiceDataAccessProvider Provider { get; set; }

		protected void ThrowAnyValidationErrors(ValidationResult validationResult)
		{
			var hasErrors = false;
			foreach (var validationError in validationResult.Errors)
			{
				hasErrors = true;
				validationError.ErrorMessage = this.appContext.ResourceManager.GetString(validationError.ErrorCode);
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