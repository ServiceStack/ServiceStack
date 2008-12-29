using System;
using System.IO;
using System.Xml;
using ServiceStack.Common.DesignPatterns.Translator;
using ServiceStack.Common.Services.Service;

namespace ServiceStack.Common.Services.Support.Service
{
    public class XmlMessageInspector : ITranslator<IXmlServiceRequest, string>
    {
        class ServiceRequest : IXmlServiceRequest
        {
            public int? Version { get; set; }
            public string OperationName { get; set; }
        }

        public IXmlServiceRequest Parse(string domainProduct)
        {
            const string VERSION_TAG_NAME = "Version";
            const string XML_START_TAG = "<";

            if (domainProduct == null)
            {
                throw new ArgumentNullException(domainProduct);
            }
            if (!domainProduct.StartsWith(XML_START_TAG))
            {
                throw new ArgumentException("invalid xml", "domainProduct");
            }

            var request = new ServiceRequest();
            
            using (var reader = new StringReader(domainProduct))
            {
                var xmlReader = new XmlTextReader(reader);
                var isFirst = true;
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType != XmlNodeType.Element) continue;
                    if (isFirst)
                    {
                        request.OperationName = xmlReader.LocalName;
                        isFirst = false; 
                    }
                    if (VERSION_TAG_NAME == xmlReader.LocalName)
                    {
                        int versionNo;
                        if (int.TryParse(xmlReader.ReadString(), out versionNo))
                        {
                            request.Version = versionNo;
                        }
                        break;
                    }
                }
            }

            return request;
        }

    }
}