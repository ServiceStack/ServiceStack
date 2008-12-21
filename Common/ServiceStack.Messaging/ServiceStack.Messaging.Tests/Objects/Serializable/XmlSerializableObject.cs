using System;
using System.Collections.Generic;
using System.Text;

using System.Xml.Serialization;

namespace ServiceStack.Messaging.Tests.Objects.Serializable
{
    [Serializable]
    [XmlTypeAttribute(Namespace = "http://services.ddn.co.uk/schema/enterprise/service/Test/3.0.0")]
    [XmlRootAttribute(Namespace = "http://services.ddn.co.uk/schema/enterprise/service/Test/3.0.0", IsNullable = false)]
    public class XmlSerializableObject 
    {
        private string value;

        [XmlElement(DataType = "normalizedString")]
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }
    }
}
