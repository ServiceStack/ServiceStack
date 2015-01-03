using ServiceStack;

namespace Check.ServiceModel
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

    public class CachedEcho
    {
        public bool Reload { get; set; }
        public string Sentence { get; set; }
    }
}