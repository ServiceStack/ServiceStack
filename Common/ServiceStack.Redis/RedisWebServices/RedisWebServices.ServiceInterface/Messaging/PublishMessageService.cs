using RedisWebServices.ServiceModel.Operations.Messaging;

namespace RedisWebServices.ServiceInterface.Messaging
{
	public class PublishMessageService
		: RedisServiceBase<PublishMessage>
	{
		protected override object Run(PublishMessage request)
		{
			RedisExec(r => r.PublishMessage(request.ToChannel, request.Message));

			return new PublishMessageResponse();
		}
	}
}