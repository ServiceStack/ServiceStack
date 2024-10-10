#if NETFX
using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Serialization;
using Message = System.ServiceModel.Channels.Message;
using DataContractSerializer = ServiceStack.Serialization.DataContractSerializer;

namespace ServiceStack.WebHost.Endpoints.Tests;

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

		var fromRequest = msg.GetBody<Reverse>(new System.Runtime.Serialization.DataContractSerializer(typeof(Reverse)));
		Assert.That(fromRequest.Value, Is.EqualTo(request.Value));
	}

	[Test]
	public void Can_Deserialize_Message_from_GetReaderAtBodyContents()
	{
		var msg = Message.CreateMessage(MessageVersion.Default, "Reverse", request);
		using (var reader = msg.GetReaderAtBodyContents())
		{
			var requestXml = reader.ReadOuterXml();
			var fromRequest = (Reverse)DataContractSerializer.Instance.DeserializeFromString(requestXml, typeof(Reverse));
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

#if NETFRAMEWORK
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
			Console.WriteLine("BODY: " + DataContractSerializer.Instance.SerializeToString(request));
			Console.WriteLine("EXPECTED BODY: " + xml);

			var fromRequest = (Reverse)DataContractSerializer.Instance.DeserializeFromString(xml, typeof(Reverse));
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


	protected static Message GetRequestMessage(string requestXml)
	{
		var doc = new XmlDocument();
		doc.LoadXml(requestXml);

		var msg = Message.CreateMessage(new XmlNodeReader(doc), int.MaxValue,
			MessageVersion.Soap11WSAddressingAugust2004);
		//var msg = Message.CreateMessage(MessageVersion.Soap12WSAddressingAugust2004, 
		//    "*", new XmlBodyWriter(requestXml));

		return msg;
	}

	[Test]
	public void Can_create_message_from_xml()
	{
		var requestXml =
			"<?xml version=\"1.0\" encoding=\"utf-8\"?>"
			+ "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\""
			+ " xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\""
			+ " xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body>"
			+ "<Reverse xmlns=\"http://schemas.servicestack.net/types\"><Value>Testing</Value></Reverse>"
			+ "</soap:Body></soap:Envelope>";

		var requestMsg = GetRequestMessage(requestXml);

		using (var reader = requestMsg.GetReaderAtBodyContents())
		{
			requestXml = reader.ReadOuterXml();
		}

		var requestType = typeof(Reverse);
		var request = (Reverse)DataContractSerializer.Instance.DeserializeFromString(requestXml, requestType);
		Assert.That(request.Value, Is.EqualTo("Testing"));
	}
#endif

	public class DtoBodyWriter : BodyWriter
	{
		private readonly object dto;
		public DtoBodyWriter(object dto)
			: base(true)
		{
			this.dto = dto;
		}

		protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
		{
			var xml = DataContractSerializer.Instance.SerializeToString(dto);
			writer.WriteString(xml);
		}
	}

	public class XmlBodyWriter : BodyWriter
	{
		private readonly string xml;
		public XmlBodyWriter(string xml)
			: base(true)
		{
			this.xml = xml;
		}

		protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
		{
			writer.WriteString(xml);
		}
	}
}

#endif
