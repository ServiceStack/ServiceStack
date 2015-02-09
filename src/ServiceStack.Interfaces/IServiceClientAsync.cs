using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack
{
	public interface IServiceClientAsync : IRestClientAsync
	{
		Task<TResponse> SendAsync<TResponse>(object requestDto);
        Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requests);
    }
}