using ServiceStack.Text;

namespace ServiceStack.Formats
{
    public class SpanFormats : IPlugin
    {
        public MemoryProvider MemoryProvider { get; set; }

        public void Register(IAppHost appHost)
        {
            if (MemoryProvider != null)
                MemoryProvider.Instance = MemoryProvider;
            
            appHost.ContentTypes.RegisterAsync(MimeTypes.Json, 
                responseSerializer:null,
                streamDeserializer: JsonSerializer.DeserializeFromStreamAsync);
            
            appHost.ContentTypes.RegisterAsync(MimeTypes.Jsv, 
                responseSerializer:null,
                streamDeserializer: TypeSerializer.DeserializeFromStreamAsync);
        }
    }
}