using ServiceStack;

namespace Check.ServiceModel
{
    [Route("/{Version}/userdata", "GET")]
    public class SwaggerVersionTest
    {
        public string Version { get; set; }
    }
}