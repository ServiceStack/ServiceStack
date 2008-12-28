using System;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints.Endpoints
{
	public class BaseSoap11MetadataHandler : BaseSoapMetadataHandler
	{
		public override string EndpointType { get { return "Soap11"; } }

		protected override string CreateMessage(Type dtoType)
		{
			var requestObj = ReflectionUtils.PopulateObject(Activator.CreateInstance(dtoType));
			var xml = DataContractSerializer.Instance.Parse(requestObj, true);
			var soapEnvelope = string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>

{0}

    </soap:Body>
</soap:Envelope>", xml);
			return soapEnvelope;
		}
	}
}