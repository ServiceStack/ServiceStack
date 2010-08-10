using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Examples.ServiceModel.Operations
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// </summary>
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class DeleteAllUsers
	{
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class DeleteAllUsersResponse
	{
		public DeleteAllUsersResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public long UserId { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}