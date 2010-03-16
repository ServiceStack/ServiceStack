using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.Messaging.Tests.Services
{
	[DataContract]
	public class AlwaysFails
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class AlwaysFailsResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class AlwaysFailsService
		: IService<AlwaysFails>, IAsyncService<AlwaysFails>
	{
		public string Result { get; set; }

		public object Execute(AlwaysFails request)
		{
			throw new NotSupportedException("This service always fails");
		}

		public void ExecuteAsync(AlwaysFails request)
		{
		}
	}

}