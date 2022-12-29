using System;
using System.Collections.Generic;

namespace ServiceStack.Text.Tests.DynamicModels.DataModel
{
#if !NETCORE
    [Serializable]
#endif
    public sealed class DataContainer : DataContainerBase
    {
        public IEnumerable<Type> TypeList { get; set; }
        public Exception Exception { get; set; }
        public object Object { get; set; }
        public Type Type { get; set; }
        //public IEnumerable<object> ObjectList { get; set; }
    }
}