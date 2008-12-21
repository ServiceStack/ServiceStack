using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using ServiceStack.Common.DesignPatterns.Serialization;

namespace ServiceStack.Common.Services.Serialization
{
    public class DataContractSerializer : IXmlSerializer 
    {
        public static DataContractSerializer Instance = new DataContractSerializer();

		public string Parse<XmlDto>(XmlDto from, bool indentXml)
		{
			try
			{
				using (var ms = new MemoryStream())
				{
					using (var xw = new XmlTextWriter(ms, Encoding.UTF8))
					{
						if (indentXml)
						{
							xw.Formatting = Formatting.Indented;	
						}

						var serializer = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
						serializer.WriteObject(xw, from);
						xw.Flush();
						ms.Seek(0, SeekOrigin.Begin);
						using (var reader = new StreamReader(ms))
						{
							return reader.ReadToEnd();
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException(string.Format("Error serializing object of type {0}", from.GetType().FullName), ex);
			}
		}

    	public string Parse<XmlDto>(XmlDto from)
        {
    		return Parse(from, false);
        }
    }
}