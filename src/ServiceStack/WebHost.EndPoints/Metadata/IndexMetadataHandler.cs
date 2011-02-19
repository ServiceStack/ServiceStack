using System;
using ServiceStack.Common.Web;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public class IndexMetadataHandler : BaseSoapMetadataHandler
	{
		public override EndpointType EndpointType { get { return EndpointType.Soap12; } }

		protected override string CreateMessage(Type dtoType)
		{
			return null;
			//throw new System.NotImplementedException();
		}
	}
}