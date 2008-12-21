using System;

namespace ServiceStack.Common.Host.Endpoints
{
	public class BaseIndexMetadataHandler : BaseSoapMetadataHandler
	{
		public override string EndpointType { get { return ""; } }

		protected override string CreateMessage(Type dtoType)
		{
			return null;
			//throw new System.NotImplementedException();
		}
	}
}