using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	//Request DTO
	[Route("/geoinfo")]
	[DataContract]
	public class GeoInfo
	{
		[DataMember]
		public string AppToken { get; set; }

		[DataMember]
		public int OrderId { get; set; }

		[DataMember]
		public GeoPoint GeoCode { get; set; }
	}

	[Serializable]
	public class GeoPoint
	{
		public long t { get; set; }
		public decimal latitude { get; set; }
		public decimal longitude { get; set; }
	}

	//Response DTO
	public class GeoInfoResponse : IHasResponseStatus
	{
		public string Result { get; set; }
		public ResponseStatus ResponseStatus { get; set; } //Where Exceptions get auto-serialized
	}

	public class GeoInfoService : ServiceInterface.Service
	{
		public object Post(GeoInfo request)
		{
			return new GeoInfoResponse
			{
				Result = "Incoming Geopoint: Latitude="
					+ request.GeoCode.latitude.ToString()
					+ " Longitude="
					+ request.GeoCode.longitude.ToString()
			};
		}
	}
}