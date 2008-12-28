/*
// $Id: Get@ModelName@sByIdAction.cs 383 2008-12-09 10:59:18Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 383 $
// Modified Date : $LastChangedDate: 2008-12-09 10:59:18 +0000 (Tue, 09 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Collections.Generic;
using Ddn.Common.Services.Extensions;
using Ddn.Common.Services.Session;
using @DomainModelNamespace@.@ServiceName@;
using @DomainModelNamespace@.Validation;
using @ServiceNamespace@.Logic.LogicInterface.Requests;
using @ServiceNamespace@.Logic.Translators.DataToDomain;
using DataModel = @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class Get@ModelName@sLogicCommand : LogicCommandBase<List<@ModelName@>>
	{
		public @ModelName@sRequest Request { get; set; }
		private @ModelName@ClientSession userSession;

		@ModelName@ClientSession GetClientSession()
		{
			if (this.userSession == null)
			{
				if (this.Request.SessionId == null) return null;
				this.userSession = this.AppContext.SessionManager.Get@ModelName@ClientSession(
						this.Request.SessionId.@ModelName@Id, this.Request.SessionId.ClientSessionId);
			}
			return this.userSession;
		}

		public override List<@ModelName@> Execute()
		{
			ThrowAnyValidationErrors(Validate());
			var db@ModelName@s = Provider.Get@ModelName@s(this.Request.@ModelName@Ids, this.Request.GlobalIds, this.Request.@ModelName@Names);
			if (Request.ValidateSession)
			{
				var filtered@ModelName@s = Filter@ModelName@s(db@ModelName@s);
				return @ModelName@Translator.Instance.ParseAll(filtered@ModelName@s);
			}
			return @ModelName@Translator.Instance.ParseAll(db@ModelName@s);
		}

		/// <summary>
		/// Filters the users based on the @ModelName@s' client session.
		/// </summary>
		/// <param name="db@ModelName@s">The db users.</param>
		/// <returns></returns>
		private List<DataModel.@ModelName@> Filter@ModelName@s(IEnumerable<DataModel.@ModelName@> db@ModelName@s)
		{
			var filtered@ModelName@s = new List<DataModel.@ModelName@>();
			var clientSession = GetClientSession();
			
			//If the user does not have a valid session than they are not authorized to receive any data.
			if (clientSession == null)
			{
				return new List<DataModel.@ModelName@>();
			}

			foreach (var user in db@ModelName@s)
			{
				//if they are authenticated then they can only see their own users data.
				if (user.Id == clientSession.@ModelName@Id)
				{
					filtered@ModelName@s.Add(user);
				}
			}
			return filtered@ModelName@s;
		}

		public override ValidationResult Validate()
		{
			var validationResult = base.Validate();
			if (Request.ValidateSession)
			{
				var clientSession = GetClientSession();
				if (clientSession == null)
				{
					validationResult.Errors.Add(new ValidationError(ErrorCodes.InvalidOrExpiredSession.ToString()));
				}
			}
			return validationResult;
		}
	}
}