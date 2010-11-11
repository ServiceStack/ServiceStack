using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.ServiceModel.Serialization;
using DataContractSerializer = System.Runtime.Serialization.DataContractSerializer;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types")]
	public class Reverse
	{
		[DataMember]
		public string Value { get; set; }
	}

	[TestFixture]
	public class MessageSerializationTests
	{
		static string xml = "<Reverse xmlns=\"http://schemas.servicestack.net/types\"><Value>test</Value></Reverse>";
		Reverse request = new Reverse { Value = "test" };
		string msgXml = "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body>" + xml + "</s:Body></s:Envelope>";

		[Test]
		public void Can_Deserialize_Message_from_GetBody()
		{
			var msg = Message.CreateMessage(MessageVersion.Default, "Reverse", request);
			//Console.WriteLine("BODY: " + msg.GetReaderAtBodyContents().ReadOuterXml());

			var fromRequest = msg.GetBody<Reverse>(new DataContractSerializer(typeof(Reverse)));
			Assert.That(fromRequest.Value, Is.EqualTo(request.Value));
		}

		[Test]
		public void Can_Deserialize_Message_from_GetReaderAtBodyContents()
		{
			var msg = Message.CreateMessage(MessageVersion.Default, "Reverse", request);
			using (var reader = msg.GetReaderAtBodyContents())
			{
				var requestXml = reader.ReadOuterXml();
				var fromRequest = (Reverse)DataContractDeserializer.Instance.Parse(requestXml, typeof(Reverse));
				Assert.That(fromRequest.Value, Is.EqualTo(request.Value));
			}
		}

		internal class SimpleBodyWriter : BodyWriter
		{
			private readonly string message;

			public SimpleBodyWriter(string message)
				: base(false)
			{
				this.message = message;
			}

			protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
			{
				writer.WriteRaw(message);
			}
		}

		[Test]
		public void Can_create_entire_message_from_xml()
		{

			//var msg = Message.CreateMessage(MessageVersion.Default,
			//    "Reverse", new SimpleBodyWriter(msgXml));

			var doc = new XmlDocument();
			doc.LoadXml(msgXml);

			using (var xnr = new XmlNodeReader(doc))
			{
				var msg = Message.CreateMessage(xnr, msgXml.Length, MessageVersion.Soap12WSAddressingAugust2004);

				var xml = msg.GetReaderAtBodyContents().ReadOuterXml();
				Console.WriteLine("BODY: " + ServiceStack.ServiceModel.Serialization.DataContractSerializer.Instance.Parse(request));
				Console.WriteLine("EXPECTED BODY: " + xml);

				var fromRequest = (Reverse)DataContractDeserializer.Instance.Parse(xml, typeof(Reverse));
				Assert.That(fromRequest.Value, Is.EqualTo(request.Value));
			}

			//var fromRequest = msg.GetBody<Request>(new DataContractSerializer(typeof(Request)));
		}

		[Test]
		public void What_do_the_different_soap_payloads_look_like()
		{
			var doc = new XmlDocument();
			doc.LoadXml(msgXml);

			//var action = "Request";
			string action = null;
			var soap12 = Message.CreateMessage(MessageVersion.Soap12, action, request);
			var soap12WSAddressing10 = Message.CreateMessage(MessageVersion.Soap12WSAddressing10, action, request);
			var soap12WSAddressingAugust2004 = Message.CreateMessage(MessageVersion.Soap12WSAddressingAugust2004, action, request);

			Console.WriteLine("Soap12: " + GetMessageEnvelope(soap12));
			Console.WriteLine("Soap12WSAddressing10: " + GetMessageEnvelope(soap12WSAddressing10));
			Console.WriteLine("Soap12WSAddressingAugust2004: " + GetMessageEnvelope(soap12WSAddressingAugust2004));
		}

		public string GetMessageEnvelope(Message msg)
		{
			var sb = new StringBuilder();
			using (var sw = XmlWriter.Create(new StringWriter(sb)))
			{
				msg.WriteMessage(sw);
				sw.Flush();
				return sb.ToString();
			}
		}

	}
}