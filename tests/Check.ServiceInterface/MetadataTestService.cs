using System.Collections.Generic;
using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class MetadataTestService : Service
    {
        public object Any(MetadataTest request)
        {
            return new MetadataTestResponse
            {
                Id = request.Id,
                Results = new List<MetadataTestChild>
                {
                    new MetadataTestChild
                    {
                        Name = "foo",
                        Results = new List<MetadataTestNestedChild>
                        {
                            new MetadataTestNestedChild { Name = "bar" },
                        }
                    }
                }
            };
        }
    }
}