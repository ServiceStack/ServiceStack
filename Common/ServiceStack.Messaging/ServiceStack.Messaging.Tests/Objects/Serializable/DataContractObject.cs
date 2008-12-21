using System.Runtime.Serialization;

namespace ServiceStack.Messaging.Tests.Objects.Serializable
{
    [DataContract(Namespace = "http://services.ddn.co.uk/schema/enterprise/service/Test/3.0.0")]
    public class DataContractObject
    {
        private string value;

        [DataMember]
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }
    }
}