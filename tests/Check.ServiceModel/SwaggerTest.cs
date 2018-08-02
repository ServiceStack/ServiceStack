using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceModel
{
    [Route("/{Version}/userdata", "GET")]
    public class SwaggerVersionTest
    {
        public string Version { get; set; }
    }

    [Route("/swagger/range")]
    public class SwaggerRangeTest
    {
        public string IntRange { get; set; }

        public string DoubleRange { get; set; }
    }
}