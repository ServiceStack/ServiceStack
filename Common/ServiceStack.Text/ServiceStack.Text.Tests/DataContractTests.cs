using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class DataContractTests
		: TestBase
	{
		[Test]
		public void Only_Serializes_DataMember_fields_for_DataContracts()
		{
			var dto = new ResponseStatus
			{
				ErrorCode = "ErrorCode",
				Message = "Message",
				StackTrace = "StackTrace",
				Errors = new List<ResponseError>(),
			};

			Serialize(dto);
		}

		[DataContract]
		public class EmptyDataContract
		{
		}

		[Test]
		public void Can_Serialize_Empty_DataContract()
		{
			var dto = new EmptyDataContract();
			Serialize(dto);
		}

	}
}