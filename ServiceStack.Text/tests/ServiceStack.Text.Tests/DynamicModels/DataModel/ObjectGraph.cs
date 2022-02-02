using System;
using System.Runtime.Serialization;
#if !NETCORE
using System.Security.Permissions;
#endif

namespace ServiceStack.Text.Tests.DynamicModels.DataModel
{
#if !NETCORE
    [Serializable]
#endif
    public class ObjectGraph
#if !NETCORE
        : ISerializable
#endif
    {
        private readonly CustomCollection internalCollection;

        public ObjectGraph()
        {
            internalCollection = new CustomCollection();
        }

#if !NETCORE
        protected ObjectGraph(SerializationInfo info, StreamingContext context)
        {
            internalCollection = (CustomCollection)info.GetValue("col", typeof(CustomCollection));
            Data = (DataContainer)info.GetValue("data", typeof(DataContainer));
        }
#endif

        public CustomCollection MyCollection
        {
            get { return internalCollection; }
        }

        public Uri AddressUri
        {
            get { return internalCollection.AddressUri; }
            set { internalCollection.AddressUri = value; }
        }

        public Type SomeType
        {
            get { return internalCollection.SomeType; }
            set { internalCollection.SomeType = value; }
        }

        public int IntValue
        {
            get { return internalCollection.IntValue; }
            set { internalCollection.IntValue = value; }
        }

        public DataContainer Data { get; set; }

#if !NETCORE
        #region ISerializable Members

        [SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("col", internalCollection);
            info.AddValue("data", Data);
        }

        #endregion
#endif
    }
}