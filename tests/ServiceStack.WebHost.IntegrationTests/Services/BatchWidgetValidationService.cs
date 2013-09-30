using System;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Route("/BatchWidgetValidation")]
	public class BatchWidgetValidationRequest
	{
		[DataMember]
		public WidgetValidationRequest[] Batch { get; set; }
	}

	[DataContract]
	public class BatchWidgetValidationResponse
	{
		[DataMember]
		public WidgetValidationResponse[] Batch { get; set; }
	}

	public class BatchWidgetValidationRequestService 
		: IService
	{
		public object Any(BatchWidgetValidationRequest request)
		{
			throw new NotImplementedException();
		}
	}

	[DataContract]
	[Route("/WidgetValidation")]
	public class WidgetValidationRequest
	{
		[DataMember]
		public int OwnerID { get; set; }

		[DataMember]
		public string SellerID { get; set; }

		[DataMember]
		public string WidgetNumber { get; set; }

		[DataMember]
		public string Quantity { get; set; }
	}

	[DataContract]
	public class WidgetValidationResponse
	{
		[DataMember]
		public string SellerWidgetNumber { get; set; }

		[DataMember]
		public string MatchType { get; set; }

		[DataMember]
		public decimal WidgetPrice { get; set; }

		[DataMember]
		public string WidgetName { get; set; }
	}

	public class WidgetValidationRequestService
		: IService
	{
		public object Any(WidgetValidationRequest request)
		{
			throw new NotImplementedException();
		}
	}
}