/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate$
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using Ddn.Common.Services.Service;
using Utopia.Common.Service;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Logic.LogicInterface;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	/// <summary>
	/// Store user password service port
	/// </summary>
	[MessagingRestriction(MessagingRestriction.Secure & MessagingRestriction.HttpPost)]
	public class Store@ModelName@PasswordPort : IService
	{
		public object Execute(CallContext context)
		{
			// Extract Store@ModelName@Password DTO
			var request = context.Request.GetDto<Store@ModelName@Password>();

			// Passthrough to facade
			var facade = context.Request.GetFacade<I@ServiceName@Facade>();
			return facade.Store@ModelName@Password(request.@ModelName@GlobalId, request.Base64OldEncryptedPassword, request.Base64NewEncryptedPassword);
		}
	}
}