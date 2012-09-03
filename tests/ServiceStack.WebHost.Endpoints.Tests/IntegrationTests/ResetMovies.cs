using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{

	[DataContract]
	[Description("Resets the database back to the original Top 5 movies.")]
	[Route("/reset-movies")]
	public class ResetMovies { }

	[DataContract]
	public class ResetMoviesResponse
		: IHasResponseStatus
	{
		public ResetMoviesResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class ResetMoviesService : RestServiceBase<ResetMovies>
	{
		public IDbConnectionFactory DbFactory { get; set; }

		public override object OnPost(ResetMovies request)
		{
			ConfigureDatabase.Init(DbFactory);

			return new ResetMoviesResponse();
		}
	}

}