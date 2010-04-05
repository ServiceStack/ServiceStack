using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class DataContractTests
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

			var dtoStr = dto.SerializeToString();
			Assert.That(dtoStr, Is.EqualTo("{ErrorCode:ErrorCode,Message:Message,StackTrace:StackTrace,Errors:[]}"));
		}
	}
}