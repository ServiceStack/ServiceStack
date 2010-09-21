using System.Linq;
using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// </summary>
	[DataContract]
	public class GetUsers
	{
		[DataMember]
		public ArrayOfLong UserIds { get; set; }

		[DataMember]
		public ArrayOfString UserNames { get; set; }
	}

	[DataContract]
	public class GetUsersResponse
	{
		public GetUsersResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ArrayOfUser Users { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}