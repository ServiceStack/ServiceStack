using System;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public class Soap12MetadataHandler : BaseSoapMetadataHandler
	{
		public override EndpointType EndpointType { get { return EndpointType.Soap12; } }

		protected override string CreateMessage(Type dtoType)
		{
			var requestObj = ReflectionUtils.PopulateObject(Activator.CreateInstance(dtoType));
			var xml = DataContractSerializer.Instance.Parse(requestObj, true);
			var soapEnvelope = string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
    <soap12:Body>

{0}

    </soap12:Body>
</soap12:Envelope>", xml);
			return soapEnvelope;
		}
	}
}