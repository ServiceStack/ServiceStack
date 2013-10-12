using System.Threading.Tasks;

namespace ServiceStack
{
	public interface IServiceClientAsync : IRestClientAsync
	{
		Task<TResponse> SendAsync<TResponse>(object requestDto);
	}
}