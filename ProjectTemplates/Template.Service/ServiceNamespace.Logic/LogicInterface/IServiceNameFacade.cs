/*
// $Id: I@ServiceName@Facade.cs 637 2008-12-19 10:48:00Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 637 $
// Modified Date : $LastChangedDate: 2008-12-19 10:48:00 +0000 (Fri, 19 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Collections.Generic;
using Ddn.Common.DesignPatterns.Facade;
using @DomainModelNamespace@.@ServiceName@;
using @ServiceNamespace@.Logic.LogicInterface.Requests;

namespace @ServiceNamespace@.Logic.LogicInterface
{
	public interface I@ServiceName@Facade : ILogicFacade
	{
		LoginAuth GetLoginAuth(string userName, string base64EncryptedPassword, string base64ClientModulus);
		List<@ModelName@> Get@ModelName@s(@ModelName@sRequest request);
        
		bool Store@ModelName@Details(@ModelName@Details userDetail);
		bool Store@ModelName@Password(Guid userGlobalId, string base64OldEncryptedPassword, string base64NewEncryptedPassword);
		void StoreNew@ModelName@(@ModelName@Details userDetails, CreditCardInfo cardInfo, string base64NewEncryptedPassword, string base64ConfirmEncryptedPassword);

		void LogoutClientSessions(int userId, ICollection<Guid> clientSessionId);
	}
}