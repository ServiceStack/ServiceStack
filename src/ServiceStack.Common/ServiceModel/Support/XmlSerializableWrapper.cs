#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace ServiceStack.ServiceModel.Support
{
    public sealed class XmlSerializerWrapper : XmlObjectSerializer
    {
        System.Xml.Serialization.XmlSerializer serializer;
        string defaultNS;
        readonly Type objectType;

        public XmlSerializerWrapper(Type type)
            : this(type, null, null)
        {

        }

        public XmlSerializerWrapper(Type type, string name, string ns)
        {
            this.objectType = type;
            if (!String.IsNullOrEmpty(ns))
            {
                this.defaultNS = ns;
                this.serializer = new System.Xml.Serialization.XmlSerializer(type, ns);
            }
            else
            {
                this.defaultNS = GetNamespace(type);
                this.serializer = new System.Xml.Serialization.XmlSerializer(type);
            }
        }

        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            throw new NotImplementedException();
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            throw new NotImplementedException();
        }
        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteObject(XmlDictionaryWriter writer, object graph)
        {
            this.serializer.Serialize(writer, graph);
        }

        public override object ReadObject(XmlDictionaryReader reader)
        {
            string readersNS;

            readersNS = (String.IsNullOrEmpty(reader.NamespaceURI)) ? "" : reader.NamespaceURI;
            if (String.Compare(this.defaultNS, readersNS) != 0)
            {
                this.serializer = new System.Xml.Serialization.XmlSerializer(this.objectType, readersNS);
                this.defaultNS = readersNS;
            }

            return (this.serializer.Deserialize(reader));
        }

        /// <summary>
        /// Gets the namespace from an attribute marked on the type's definition
        /// </summary>
        /// <param name="type"></param>
        /// <returns>Namespace of type</returns>
        public static string GetNamespace(Type type)
        {
            Attribute[] attrs = (Attribute[])type.GetCustomAttributes(typeof(DataContractAttribute), true);
            if (attrs.Length > 0)
            {
                DataContractAttribute dcAttr = (DataContractAttribute)attrs[0];
                return dcAttr.Namespace;
            }
            attrs = (Attribute[])type.GetCustomAttributes(typeof(XmlRootAttribute), true);
            if (attrs.Length > 0)
            {
                XmlRootAttribute xmlAttr = (XmlRootAttribute)attrs[0];
                return xmlAttr.Namespace;
            }
            attrs = (Attribute[])type.GetCustomAttributes(typeof(XmlTypeAttribute), true);
            if (attrs.Length > 0)
            {
                XmlTypeAttribute xmlAttr = (XmlTypeAttribute)attrs[0];
                return xmlAttr.Namespace;
            }
            attrs = (Attribute[])type.GetCustomAttributes(typeof(XmlElementAttribute), true);
            if (attrs.Length > 0)
            {
                XmlElementAttribute xmlAttr = (XmlElementAttribute)attrs[0];
                return xmlAttr.Namespace;
            }
            return null;
        }
    }
}
#endif