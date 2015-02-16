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

        public object Any(GetExample request)
        {
            return new GetExampleResponse
            {
                MenuExample1 = new MenuExample
                {
                    MenuItemExample1 = new MenuItemExample { Name1 = "foo" }
                }
            };
        }

        public object Any(MetadataRequest request)
        {
            return request;
        }
    }

    public class MetadataRequest : IReturn<AutoQueryMetadataResponse>
    {
        public MetadataType MetadataType { get; set; }
    }

}