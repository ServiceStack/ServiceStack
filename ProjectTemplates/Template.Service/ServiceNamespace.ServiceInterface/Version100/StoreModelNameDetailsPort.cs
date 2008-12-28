/*
// $Id: Get@ModelName@sPort.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using Ddn.Common.Services.Service;
using Utopia.Common.Service;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Logic.LogicInterface;
using DtoTranslators = @ServiceModelNamespace@Translators.Version100.ServiceToDomain;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class Store@ModelName@DetailsPort : IService
	{
		/// <summary>
		/// Used by Json and Soap requests if this service *is not* a 'IXElementService'
		/// </summary>
		/// <returns></returns>
		public object Execute(CallContext context)
		{
			var request = context.Request.GetDto<Store@ModelName@Details>();
			var userDetails = DtoTranslators.@ModelName@DetailsFromDtoTranslator.Instance.Parse(request.@ModelName@Details);
			var facade = context.Request.GetFacade<I@ServiceName@Facade>();
			return facade.Store@ModelName@Details(userDetails);
		}
	}
}
