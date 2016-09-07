#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using ServiceStack.Text;
using static System.String;

namespace ServiceStack.Serialization
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
            if (!IsNullOrEmpty(ns))
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

            readersNS = (IsNullOrEmpty(reader.NamespaceURI)) ? "" : reader.NamespaceURI;
            if (Compare(this.defaultNS, readersNS) != 0)
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
            var dcAttr = type.FirstAttribute<DataContractAttribute>();
            if (dcAttr != null)
            {
                return dcAttr.Namespace;
            }
            var xrAttr = type.FirstAttribute<XmlRootAttribute>();
            if (xrAttr != null)
            {
                return xrAttr.Namespace;
            }
            var xtAttr = type.FirstAttribute<XmlTypeAttribute>();
            if (xtAttr != null)
            {
                return xtAttr.Namespace;
            }
            var xeAttr = type.FirstAttribute<XmlElementAttribute>();
            return xeAttr?.Namespace;
        }
    }
}
#endif