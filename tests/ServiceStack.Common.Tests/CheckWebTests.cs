using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
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

    [Api("Echoes a sentence")]
    [Route("/echoes", "POST", Summary = @"Echoes a sentence.")]
    public class Echoes : IReturn<IEcho>
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

    [Explicit]
    public class CheckWebTests
    {
        private const string BaseUri = "http://localhost:55799/";

        [Test]
        public void Can_send_echoes_POST()
        {
            var client = new JsonServiceClient(BaseUri);

            var response = client.Post(new Echoes { Sentence = "Foo" });

            response.PrintDump();
        }
    }
}