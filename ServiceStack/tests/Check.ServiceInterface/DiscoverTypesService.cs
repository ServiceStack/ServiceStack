using System.Collections.Generic;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class ArrayElementInDictionary
    {
        public int Id { get; set; }
    }
    
    public class DiscoverTypes : IReturn<DiscoverTypes>
    {
        public Dictionary<string, ArrayElementInDictionary[]> ElementInDictionary { get; set; }
    }
    
    public class DiscoverTypesService : Service
    {
        public object Any(DiscoverTypes request) => request;
    }
}