using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Common.Services.Tests.Support.DataContracts
{
    [CollectionDataContract(Namespace = "http://servicestack.net/types/", ItemName = "Id")]
    public class ArrayOfIntId : List<int>
    {
        public ArrayOfIntId() { }
        public ArrayOfIntId(IEnumerable<int> collection) : base(collection) { }
    }
}