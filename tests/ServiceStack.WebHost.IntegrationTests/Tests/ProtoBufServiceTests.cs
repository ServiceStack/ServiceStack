using System;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Plugins.ProtoBuf;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[DataContract]
	public class ProtoBufEmail
	{
		[DataMember(Order = 1)]
		public string ToAddress { get; set; }
		[DataMember(Order = 2)]
		public string FromAddress { get; set; }
		[DataMember(Order = 3)]
		public string Subject { get; set; }
		[DataMember(Order = 4)]
		public string Body { get; set; }
		[DataMember(Order = 5)]
		public byte[] AttachmentData { get; set; }

		public bool Equals(ProtoBufEmail other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.ToAddress, ToAddress) 
				&& Equals(other.FromAddress, FromAddress) 
				&& Equals(other.Subject, Subject) 
				&& Equals(other.Body, Body)
				&& other.AttachmentData.EquivalentTo(AttachmentData);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (ProtoBufEmail)) return false;
			return Equals((ProtoBufEmail) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = (ToAddress != null ? ToAddress.GetHashCode() : 0);
				result = (result*397) ^ (FromAddress != null ? FromAddress.GetHashCode() : 0);
				result = (result*397) ^ (Subject != null ? Subject.GetHashCode() : 0);
				result = (result*397) ^ (Body != null ? Body.GetHashCode() : 0);
				result = (result*397) ^ (AttachmentData != null ? AttachmentData.GetHashCode() : 0);
				return result;
			}
		}
	}

	[DataContract]
	public class ProtoBufEmailResponse
	{
		[DataMember(Order = 1)]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class ProtoBufEmailService : ServiceInterface.Service
	{
        public object Any(ProtoBufEmail request)
		{
			return request;
		}
	}


	[TestFixture]
	public class ProtoBufServiceTests
	{
		[Test]
		public void Can_Send_ProtoBuf_request()
		{
			var client = new ProtoBufServiceClient(Config.ServiceStackBaseUri);

			var request = new ProtoBufEmail {
				ToAddress = "to@email.com",
				FromAddress = "from@email.com",
				Subject = "Subject",
				Body = "Body",
				AttachmentData = Encoding.UTF8.GetBytes("AttachmentData"),
			};

			try
			{
				var response = client.Send<ProtoBufEmail>(request);

				Console.WriteLine(response.Dump());

				Assert.That(response.Equals(request));
			}
			catch (WebServiceException webEx)
			{
				Console.WriteLine(webEx.ResponseDto.Dump());
				Assert.Fail(webEx.Message);
			}
		}

	}
}