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
using @DomainModelNamespace@.@ServiceName@;
using @DomainModelNamespace@.Validation;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Logic;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.ServiceInterface.Translators;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	/// <summary>
	/// Login authentication service port.
	/// 
	/// As this is returning both 'Public' and 'Secure' session ids you should only login over a secure channel.
	/// </summary>
	[MessagingRestriction(MessagingRestriction.Secure & MessagingRestriction.HttpPost)]
	public class GetLoginAuthPort : IService
	{
		public object Execute(CallContext context)
		{
			// Extract request DTO
			var request = context.Request.GetDto<GetLoginAuth>();

			// Make the login attempt
			var facade = context.Request.GetFacade<I@ServiceName@Facade>();
			LoginAuth auth = facade.GetLoginAuth(request.@ModelName@Name, 
				request.Base64EncryptedPassword, request.Base64ClientModulus);

			if (auth != null)
			{
				// Login authentication success
				return new GetLoginAuthResponse {
            		ResponseStatus = ResponseStatusTranslator.CreateSuccessResponse(
						MessageCodes.LoginWasSuccessful.ToString()),

					@ModelName@Id = auth.@ModelName@Id,
					PublicSessionId = auth.PublicSessionId,
					SecureSessionId = auth.SecureSessionId,
					ServerTime = auth.ServerTime,
					ExpiryDate = auth.ExpiryDate,
				};
			}
			else
			{
				// Login authentication failure will return generic error message
				return new GetLoginAuthResponse
				{
					ResponseStatus = ResponseStatusTranslator.CreateErrorResponse(
						ErrorCodes.Invalid@ModelName@OrPassword.ToString()),
				};
			}
		}
	}
}