using System.Threading.Tasks;
using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
/// <summary>
    /// The Echoes web service.
    /// </summary>
    public class EchoesService : Service
    {
        public IServiceClient Client { get; set; }

        /// <summary>
        /// GET echoes.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public object Post(Echoes request)
        {
            return new Echo { Sentence = request.Sentence };
        }

        public async Task<object> Any(AsyncTest request)
        {
            var response = await Client.PostAsync(new Echoes { Sentence = "Foo" });
            return response;
        }
    }
}