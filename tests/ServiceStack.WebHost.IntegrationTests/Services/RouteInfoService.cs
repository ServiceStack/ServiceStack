using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using DeliveryService.Model.Types;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace DeliveryService.Model.Operations
{
	[Description("POST the route information based on the Application Token associated to a route and Associate ID")]
	[Route("/RouteInfo", "POST")]
	[Route("/RouteInfo/{AppToken}")]
	[Route("/RouteInfo/{AppToken}/{HasProduct}")]
	[DataContract]
	public class RouteInfo
	{
		[DataMember]
		public string AppToken { get; set; }

		[DataMember]
		public bool? HasProduct { get; set; }
	}

	[DataContract]
	public class RouteInfoResponse : IHasResponseStatus
	{
		public RouteInfoResponse()
		{
			this.ResponseStatus = new ResponseStatus();
			this.Customers = new List<Customer>();
			this.Outcomes = new List<Outcome>();
			this.DD = new Dictionary<string, Dictionary<string, string>>();
			this.Tweak = new Dictionary<string, int>();
		}

		[DataMember]
		public List<Customer> Customers { get; set; }

		[DataMember]
		public List<Outcome> Outcomes { get; set; }

		[DataMember]
		public Dictionary<string, Dictionary<string, string>> DD { get; set; }

		[DataMember]
		public Dictionary<string, int> Tweak { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class RouteInfoService : Service
	{
        public object Any(RouteInfo request)
		{
			throw new NotImplementedException();
		}
	}
}

namespace DeliveryService.Model.Types
{
	[DataContract]
	public class Outcome
	{
		[DataMember]
		public string UID { get; set; }

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public List<OutcomeReason> Reasons { get; set; }
	}


	[DataContract]
	public class OutcomeReason
	{
		[DataMember]
		public string UID { get; set; }

		[DataMember]
		public string Message { get; set; }

	}
}


namespace DeliveryService.Model.Types
{
	[DataContract]
	public class Customer
	{
		[DataMember]
		public string UID { get; set; }
		[DataMember]
		public int RoutePos { get; set; }
		[DataMember]
		public string Invoice { get; set; }
		[DataMember]
		public string FirstName { get; set; }
		[DataMember]
		public string LastName { get; set; }
		[DataMember]
		public string Address { get; set; }
		[DataMember]
		public string City { get; set; }
		[DataMember]
		public string State { get; set; }
		[DataMember]
		public string ZipCode { get; set; }
		[DataMember]
		public string HmPhone { get; set; }
		[DataMember]
		public string WkPhone { get; set; }
		[DataMember]
		public string ClPhone { get; set; }
		[DataMember]
		public string ArrivalETA { get; set; }
		[DataMember]
		public string CompletionCode { get; set; }
		[DataMember]
		public string ConfirmationCode { get; set; }
		[DataMember]
		public bool IsPosted { get; set; }
		[DataMember]
		public string Lat { get; set; }
		[DataMember]
		public string Long { get; set; }

	}
}