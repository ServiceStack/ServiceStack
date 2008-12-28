/*
// $Id: @ServiceName@Facade.cs 678 2008-12-22 19:23:55Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 678 $
// Modified Date : $LastChangedDate: 2008-12-22 19:23:55 +0000 (Mon, 22 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Collections.Generic;
using Ddn.Common.DesignPatterns.Command;
using Ddn.Common.Services.Service;
using Ddn.DataAccess;
using Ddn.Logging;
using Utopia.Common.Logic;
using @DomainModelNamespace@.@ServiceName@;
using @ServiceNamespace@.DataAccess;
using @ServiceNamespace@.Logic.LogicCommands;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Logic.LogicInterface.Requests;

namespace @ServiceNamespace@.Logic
{
	public class @ServiceName@Facade : LogicFacadeBase, I@ServiceName@Facade
	{

		private readonly ILog log;

		private AppContext AppContext { get; set; }

		private IPersistenceProvider PersistenceProvider { get; set; }

		private @ServiceName@DataAccessProvider Provider { get; set; }

		private string ClientIPAddress { get; set; }

		public @ServiceName@Facade(AppContext appContext, IPersistenceProviderManager providerManager, string clientIPAddress)
		{
			this.AppContext = appContext;

			// Create new connection
			this.PersistenceProvider = providerManager.CreateProvider();

			// Wrap connection in Data Access Provider
			this.Provider = new @ServiceName@DataAccessProvider(PersistenceProvider);

			this.ClientIPAddress = clientIPAddress;

			this.log = this.AppContext.LogFactory.GetLogger(GetType());
		}

		public LoginAuth GetLoginAuth(string userName, string base64EncryptedPassword, string base64ClientModulus)
		{
			return Execute(new GetLoginAuthLogicCommand {
				@ModelName@Name = userName,
				Base64EncryptedPassword = base64EncryptedPassword,
				Base64ClientModulus = base64ClientModulus,
				ClientIPAddress = this.ClientIPAddress
			});
		}

		public List<@ModelName@> Get@ModelName@s(@ModelName@sRequest request)
		{
			return Execute(new Get@ModelName@sLogicCommand {
				Request = request
			});
		}

		public bool Store@ModelName@Details(@ModelName@Details userDetail)
		{
			return Execute(new Store@ModelName@DetailsLogicCommand {
				@ModelName@Details = userDetail
			});
		}

		public bool Store@ModelName@Password(Guid userGlobalId, string base64OldEncryptedPassword,
			string base64NewEncryptedPassword)
		{
			return Execute(new Change@ModelName@PasswordLogicCommand {
				@ModelName@GlobalId = userGlobalId,
				Base64OldEncryptedPassword = base64OldEncryptedPassword,
				Base64NewEncryptedPassword = base64NewEncryptedPassword
			});
		}

		public void StoreNew@ModelName@(@ModelName@Details userDetails, CreditCardInfo cardInfo, string base64NewEncryptedPassword, string base64ConfirmEncryptedPassword)
		{
			Execute(new StoreNew@ModelName@LogicCommand {
				@ModelName@Details = userDetails,
				CardInfo = cardInfo,
				Base64EncryptedPassword = base64NewEncryptedPassword,
				Base64EncryptedConfirmPassword = base64ConfirmEncryptedPassword,
			});
		}

		public void LogoutClientSessions(int userId, ICollection<Guid> clientSessionIds)
		{
			Execute(new LogoutClientSessionLogicCommand {
				@ModelName@Id = userId,
				ClientSessionIds = clientSessionIds,
			});
		}

		public override void Dispose()
		{
			// Close the connection
			this.PersistenceProvider.Dispose();
		}

		protected override void Init<T>(ICommand<T> command)
		{
			var action = (IAction<T>)command;
			action.AppContext = this.AppContext;
			action.Provider = this.Provider;
		}
	}
}
