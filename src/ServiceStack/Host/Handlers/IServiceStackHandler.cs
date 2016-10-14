using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public interface IServiceStackHandler
    {
        string RequestName { get; }

        Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName);
        void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName);
    }
}