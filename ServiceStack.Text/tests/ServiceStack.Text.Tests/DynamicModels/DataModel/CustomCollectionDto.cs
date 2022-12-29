using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ServiceStack.Text.Tests.DynamicModels.DataModel
{
    [RuntimeSerializable]
    public class CustomCollectionDto
    {
        public Exception Exception { get; set; }

        public CustomException CustomException { get; set; }

        public Uri AddressUri { get; set; }

        public Type SomeType { get; set; }

        public int IntValue { get; set; }
    }
}