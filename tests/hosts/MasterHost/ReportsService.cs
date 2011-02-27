using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace MasterHost
{
	[DataContract]
	[Description("View last results of ServiceStack's runners")]
	[RestService("/reports")]
	[RestService("/reports/{Name}")]
	public class Reports
	{
		[DataMember]
		public string FilterHost { get; set; }
	}

	[DataContract]
	public class ReportsResponse : IHasResponseStatus
	{
		public ReportsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
			this.Results = new List<Report>();
		}

		[DataMember]
		public List<Report> Results { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	[DataContract]
	public class Report
	{
		public Report()
		{
			this.Tests = new List<ReportTest>();
		}

		//[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string HostEnvironment { get; set; }

		[DataMember]
		public string BaseUrl { get; set; }

		[DataMember]
		public DateTime LastModified { get; set; }

		[DataMember]
		public string Host { get; set; }

		[DataMember]
		public string ServiceName { get; set; }

		[DataMember]
		public string UserHostAddress { get; set; }

		[DataMember]
		public int MaxStatusCode { get; set; }

		[DataMember]		
		public List<ReportTest> Tests { get; set; }
	}

	[DataContract]
	public class ReportTest
	{
		[DataMember]
		public string RequestPath { get; set; }

		[DataMember]
		public string AbsoluteUri { get; set; }

		[DataMember]
		public string RawUrl { get; set; }

		[DataMember]
		public string PathInfo { get; set; }

		[DataMember]
		public string ResponseContentType { get; set; }

		[DataMember]
		public int StatusCode { get; set; }

		[DataMember]
		public string ErrorCode { get; set; }

		[DataMember]
		public string ErrorMessage { get; set; }

		[DataMember]
		public string StackTrace { get; set; }
	}


	public class ReportsService : RestServiceBase<Reports>
	{
		public IDbConnectionFactory DbFactory { get; set; }

		public override object OnGet(Reports request)
		{
			var response = new ReportsResponse
			{
				Results = request.FilterHost.IsNullOrEmpty()
					? DbFactory.Exec(dbCmd => dbCmd.Select<Report>())
					: DbFactory.Exec(dbCmd => dbCmd.Select<Report>("ServiceName = {0}", request.FilterHost))
			};
			return response;
		}
	}


}