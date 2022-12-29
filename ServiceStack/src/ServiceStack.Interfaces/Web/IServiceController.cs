using System;
using System.Threading.Tasks;
using ServiceStack.Messaging;

namespace ServiceStack.Web
{
    /// <summary>
    /// Responsible for executing the operation within the specified context.
    /// </summary>
    /// <value>The operation types.</value>
    public interface IServiceController : IServiceExecutor
    {
        /// <summary>
        /// Returns the first matching RestPath
        /// </summary>
        IRestPath GetRestPathForRequest(string httpMethod, string pathInfo);

        /// <summary>
        /// Executes the MQ DTO request.
        /// </summary>
        object ExecuteMessage(IMessage mqMessage);

        /// <summary>
        /// Executes the MQ DTO request with the supplied request context
        /// </summary>
        object ExecuteMessage(IMessage dto, IRequest request);

        /// <summary>
        /// Executes the DTO request under the supplied request context.
        /// </summary>
        object Execute(object requestDto, IRequest request);

        /// <summary>
        /// Executes the DTO request under supplied context and option to Execute Request/Response Filters.
        /// </summary>
        object Execute(object requestDto, IRequest request, bool applyFilters);

        /// <summary>
        /// Executes the DTO request with an empty request context.
        /// </summary>
        object Execute(object requestDto);

        /// <summary>
        /// Executes the DTO request with the current HttpRequest and option to Execute Request/Response Filters.
        /// </summary>
        object Execute(IRequest request, bool applyFilters);

        /// <summary>
        /// Execute Service Gateway Requests
        /// </summary>
        Task<object> GatewayExecuteAsync(object requestDto, IRequest req, bool applyFilters);
    }
}