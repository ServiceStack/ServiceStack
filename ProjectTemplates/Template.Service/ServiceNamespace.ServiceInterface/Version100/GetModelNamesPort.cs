/*
// $Id: Get@ModelName@sPort.cs 695 2008-12-23 14:34:01Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 695 $
// Modified Date : $LastChangedDate: 2008-12-23 14:34:01 +0000 (Tue, 23 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using Ddn.Common.Services.Extensions;
using Ddn.Common.Services.Service;
using Utopia.Common.Service;
using @DomainModelNamespace@.Validation;
using @ServiceModelNamespace@Translators.Version100.DomainToService;
using @ServiceModelNamespace@Translators.Version100.ServiceToDomain;
using @ServiceNamespace@.Logic;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Logic.LogicInterface.Requests;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.ServiceInterface.Translators;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	/// <summary>
	/// Get's users private information
	/// 
	/// Requires authentication.
	/// </summary>
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class Get@ModelName@sPort : IService
	{
		public object Execute(CallContext context)
		{
			// Extract request DTO
			var request = context.Request.GetDto<Get@ModelName@s>();

			// Retrieve the users
			var facade = context.Request.GetFacade<I@ServiceName@Facade>();
			try
			{
				var results = facade.Get@ModelName@s(new @ModelName@sRequest {
					SessionId = SessionIdFromDtoTranslator.Instance.Parse(request.SessionId),
					@ModelName@Ids = request.Ids,
					@ModelName@Names = request.@ModelName@Names,
				});
				return new Get@ModelName@sResponse {
					@ModelName@s = @ModelName@ToDtoTranslator.Instance.ParseAll(results)
				};
			}
			catch (ValidationException ve)
			{
				return new Get@ModelName@sResponse { ResponseStatus = ResponseStatusTranslator.Instance.Parse(ve) };
			}
		}
	}
}
