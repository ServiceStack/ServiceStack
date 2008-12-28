using Ddn.Common.Services.Service;
using Utopia.Common.Service;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Logic.LogicInterface;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	/// <summary>
	/// This should be run from an Async endpoint as the client doesn't need a response.
	/// </summary>
	[MessagingRestriction(MessagingRestriction.AsyncOneWay & MessagingRestriction.HttpPost)]
	public class NotifyLogoutClientSessionsPort : IService
	{
		public object Execute(CallContext context)
		{
			var request = (NotifyLogoutClientSessions)context.Request.Dto;
			var facade = context.Request.GetFacade<I@ServiceName@Facade>();
			
			facade.LogoutClientSessions(request.@ModelName@Id, request.ClientSessionIds);
			
			return null;
		}
	}
}