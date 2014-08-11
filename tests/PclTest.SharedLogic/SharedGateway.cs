using System.Threading.Tasks;
using PclTest.ServiceModel;
using ServiceStack;

namespace PclTest.SharedLogic
{
    public class SharedGateway
    {
        public IServiceClient ServiceClient { get; set; }

        public SharedGateway(string url = null)
        {
            ServiceClient = new JsonServiceClient(url ?? "http://localhost:81/");
        }

        public async Task<string> SayHello(string name)
        {
            var response = await ServiceClient.GetAsync(new Hello { Name = name });
            return response.Result;
        }
    }
}
