using RedisWebServices.ServiceModel.Operations.Admin;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class FlushDbService
		: RedisServiceBase<FlushDb>
	{
		protected override object Run(FlushDb request)
		{
			if (request.Db == 0)
			{
				RedisNativeExec(r => r.FlushDb());
			}
			else
			{
				using (var redisClient = ClientsManager.GetClient())
				{
					redisClient.Db = request.Db;
					redisClient.FlushDb();
				}
			}
			
			return new FlushDbResponse();
		}
	}
}