using System.Threading.Tasks;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class AsyncTest : IReturn<Echo> { }

    /// <summary>
    /// The Echo interface.
    /// </summary>
    public interface IEcho
    {
        /// <summary>
        /// Gets or sets the sentence to echo.
        /// </summary>
        string Sentence { get; set; }
    }

    /// <summary>
    /// The Echo.
    /// </summary>
    public class Echo : IEcho
    {
        /// <summary>
        /// Gets or sets the sentence.
        /// </summary>
        public string Sentence { get; set; }
    }

    /// <summary>
    /// The Echoes operation endpoints.
    /// </summary>
    [Api("Echoes a sentence")]
    [Route("/echoes", "POST", Summary = @"Echoes a sentence.")]
    public class Echoes : IReturn<Echo>
    {
        /// <summary>
        /// Gets or sets the sentence to echo.
        /// </summary>
        [ApiMember(Name = "Sentence",
            DataType = "string",
            Description = "The sentence to echo.",
            IsRequired = true,
            ParameterType = "form")]
        public string Sentence { get; set; }
    }
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