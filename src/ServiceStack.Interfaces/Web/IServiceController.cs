using ServiceStack.Messaging;

namespace ServiceStack.Web
{
    /// <summary>
    /// Responsible for executing the operation within the specified context.
    /// </summary>
    /// <value>The operation types.</value>
    public interface IServiceController
    {
        /// <summary>
        /// Returns the first matching RestPath
        /// </summary>
        IRestPath GetRestPathForRequest(string httpMethod, string pathInfo);

        /// <summary>
        /// Executes the MQ DTO request.
        /// </summary>
        object ExecuteMessage<T>(IMessage<T> mqMessage);

        /// <summary>
        /// Executes the MQ DTO request with the supplied requestContext
        /// </summary>
        object ExecuteMessage<T>(IMessage<T> dto, IRequest requestContext);

        /// <summary>
        /// Executes the DTO request under the supplied requestContext.
        /// </summary>
        object Execute(object requestDto, IRequest request);

        /// <summary>
        /// Executes the DTO request with an empty RequestContext.
        /// </summary>
        object Execute(object requestDto);
    }
}