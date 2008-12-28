/*
// $Id: ActionBase.cs 599 2008-12-18 12:08:26Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 599 $
// Modified Date : $LastChangedDate: 2008-12-18 12:08:26 +0000 (Thu, 18 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using Ddn.Common.Services.Service;
using Ddn.Logging;
using Utopia.Common;
using @DomainModelNamespace@.Validation;
using @ServiceNamespace@.DataAccess;

namespace @ServiceNamespace@.Logic.LogicCommands
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

		public @ServiceName@DataAccessProvider Provider { get; set; }

		protected void ThrowAnyValidationErrors(ValidationResult validationResult)
		{
			var hasErrors = false;
			foreach (var validationError in validationResult.Errors)
			{
				hasErrors = true;
				validationError.ErrorMessage = this.appContext.GetErrorMessage(validationError.ErrorCode);
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