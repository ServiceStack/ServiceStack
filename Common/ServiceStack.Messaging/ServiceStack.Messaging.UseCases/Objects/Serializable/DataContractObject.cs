using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization;

namespace ServiceStack.Messaging.UseCases.Objects.Serializable
{
    [DataContract(Namespace = "http://services.worldwide.bbc.co.uk/schema/enterprise/service/Test/3.0.0")]
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