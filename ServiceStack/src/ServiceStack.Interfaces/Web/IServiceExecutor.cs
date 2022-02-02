using System.Threading.Tasks;

namespace ServiceStack.Web
{
    public interface IServiceExecutor
    {
        /// <summary>
        /// Executes the DTO request under the supplied request context.
        /// </summary>
        Task<object> ExecuteAsync(object requestDto, IRequest request);
    }
}