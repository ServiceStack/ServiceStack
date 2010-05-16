using RedisWebServices.ServiceModel.Operations.Admin;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class SaveService
		: RedisServiceBase<Save>
	{
		protected override object Run(Save request)
		{
			if (request.InBackground)
			{
				RedisExec(r => r.SaveAsync());
			}
			else
			{
				RedisExec(r => r.Save());
			}
			
			return new SaveResponse();
		}
	}
}