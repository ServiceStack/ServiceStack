using Ddn.Common.DesignPatterns.Facade;
using Ddn.Common.Services.Service;
using Utopia.Common;
using Utopia.Common.Service;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.ServiceInterface.Translators;
using DtoTranslators = @ServiceModelNamespace@Translators.Version100.ServiceToDomain;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class StoreNew@ModelName@Port : IService
	{
		/// <summary>
		/// Used by Json and Soap requests if this service *is not* a 'IXElementService'
		/// </summary>
		/// <returns></returns>
		public object Execute(CallContext context)
		{
			var request = context.Request.GetDto<StoreNew@ModelName@>();
			var facade = context.Request.GetFacade<I@ServiceName@Facade>();

			var userDetails = DtoTranslators.@ModelName@DetailsFromDtoTranslator.Instance.Parse(request.@ModelName@Details);
			var cardInfo = DtoTranslators.CreditCardFromDtoTranslator.Instance.Parse(request.PrimaryCreditCard);

			IValidatableCommand<bool> command;

			using (var initOnlyContext = facade.AcquireInitContext(InitOptions.InitialiseOnly))
			{
				facade.StoreNew@ModelName@(userDetails, cardInfo, request.Base64EncryptedPassword, request.Base64EncryptedConfirmPassword);
				command = (IValidatableCommand<bool>)initOnlyContext.InitialisedObject;
			}

			var response = new StoreNew@ModelName@Response {
				ResponseStatus = ResponseStatusTranslator.Instance.Parse(command.Validate())
			};
			if (response.ResponseStatus.ErrorCode == null)
			{
				//This needs to be synchronous as the client will attempt to auto-login after creating a new user
				command.Execute();
			}
			return response;
		}
	}
}
