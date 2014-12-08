using System.Collections.Generic;
using ServiceStack;

namespace Check.ServiceModel
{
    public class MetadataTest : IReturn<MetadataTestResponse>
    {
        public int Id { get; set; }
    }

    public class MetadataTestResponse
    {
        public int Id { get; set; }
        public List<MetadataTestChild> Results { get; set; }
    }

    public class MetadataTestChild
    {
        public string Name { get; set; }

        public List<MetadataTestNestedChild> Results { get; set; }
    }

    public class MetadataTestNestedChild
    {
        public string Name { get; set; }
    }
}