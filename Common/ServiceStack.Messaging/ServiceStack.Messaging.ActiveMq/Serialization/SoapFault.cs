using System;
using System.IO;
using System.Security;
using System.Text;
using System.Xml;

namespace ServiceStack.Messaging.ActiveMq.Serialization
{
    /// <summary>
    /// Creates a 1.2 SOAP Fault
    /// refer to http://msdn2.microsoft.com/en-us/library/ms189538.aspx
    /// </summary>
    internal class SoapFault
    {
        //TODO: add support for validation exceptions and customizable plugin model to recreate Exceptions from SOAPExceptions
        private const string NS_ADDR = "http://www.w3.org/2005/08/addressing";
        private const string NS_SOAP = "http://www.w3.org/2003/05/soap-envelope";
        private const string NS_SOAP_PREFIX = "s";
        private const string NS_DDN_SERVICES = "http://ddn.services/2007/06";

        private readonly string action;
        private readonly Exception ex;
        private readonly string originalRequest;

        internal SoapFault(string action, Exception ex)
        {
            this.action = action;
            this.ex = ex;
        }

        internal SoapFault(string action, Exception ex, string originalRequest)
            : this(action, ex)
        {
            this.originalRequest = originalRequest;
        }

        public override string ToString()
        {
            string exceptionBody = string.Empty;
            IXmlSerializable serializableException = ex as IXmlSerializable;
            if (serializableException != null)
            {
                exceptionBody = serializableException.ToXml();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlTextWriter xw = new XmlTextWriter(ms, Encoding.UTF8))
                {
                    xw.WriteStartDocument();
                    xw.WriteStartElement("s", "Envelope", NS_SOAP);
                    xw.WriteStartElement("s", "Body", NS_SOAP);
                    xw.WriteStartElement("s", "Fault", NS_SOAP);

                    xw.WriteStartElement("s", "Code", NS_SOAP);
                    xw.WriteElementString("s", "Value", NS_SOAP, ex.GetType().FullName);
                    xw.WriteEndElement(); // Code

                    xw.WriteStartElement("s", "Reason", NS_SOAP);
                    xw.WriteAttributeString("xml", "lang", null, "en-US");
                    xw.WriteElementString("s", "Text", NS_SOAP, ex.Message);
                    xw.WriteEndElement(); // Reason

                    xw.WriteElementString("s", "Node", NS_SOAP, action);

                    xw.WriteStartElement("s", "Detail", NS_SOAP);

                    xw.WriteStartElement("s", "Exception", NS_SOAP);
                    xw.WriteAttributeString("xmlns", "Text", null, NS_DDN_SERVICES);

                    xw.WriteElementString("Type", ex.GetType().FullName);
                    xw.WriteElementString("Message", ex.Message);
                    xw.WriteElementString("StackTrace", ex.StackTrace);
                    xw.WriteElementString("Body", exceptionBody);
                    xw.WriteElementString("OriginalRequest", originalRequest);
                    xw.WriteElementString("DateTime", DateTime.Now.ToString());

                    xw.WriteEndElement(); // Exception
                    xw.WriteEndElement(); // Detail
                    xw.WriteEndElement(); // Fault
                    xw.WriteEndElement(); // Body
                    xw.WriteEndElement(); // Envelope

                    xw.Flush();

                    ms.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(ms))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}