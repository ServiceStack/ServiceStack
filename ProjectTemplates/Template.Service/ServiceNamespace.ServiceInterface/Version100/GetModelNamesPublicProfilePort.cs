/*
// $Id: Get@ModelName@sPort.cs 453 2008-12-11 10:09:31Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 453 $
// Modified Date : $LastChangedDate: 2008-12-11 10:09:31 +0000 (Thu, 11 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using Ddn.Common.Services.Extensions;
using Ddn.Common.Services.Service;
using Utopia.Common.Service;
using @ServiceNamespace@.Logic;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Logic.LogicInterface.Requests;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using ModelTranslators = @ServiceModelNamespace@Translators.Version100.DomainToService;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.Rest)]
	public class Get@ModelName@sPublicProfilePort : IService
	{
		/// <summary>
		/// Retrieves the public profile of supplied user ids.
		/// 
		/// This service request does not need to be authenticated as it only returns 'public data'
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public object Execute(CallContext context)
		{
			var request = context.Request.GetDto<Get@ModelName@sPublicProfile>();

			var facade = context.Request.GetFacade<I@ServiceName@Facade>();
			var results = facade.Get@ModelName@s(new @ModelName@sRequest {
				ValidateSession = false,
				@ModelName@Ids = request.@ModelName@Ids,
				GlobalIds = request.@ModelName@GlobalIds,
				@ModelName@Names = request.@ModelName@Names,
			});

			return new Get@ModelName@sPublicProfileResponse {
				PublicProfiles = ModelTranslators.@ModelName@PublicProfileToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
