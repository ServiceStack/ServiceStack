using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Northwind.Perf;
using NUnit.Framework;
using Platform.Text;
using ProtoBuf;
using ServiceStack.Common.Text;
using ServiceStack.ServiceModel.Serialization;

namespace Northwind.Benchmarks.Serialization
{
	public class SerializationTestBase
		: PerfTestBase
	{

		public SerializationTestBase()
		{
			this.MultipleIterations = new List<int> { 1000, 10000 };
		}

		public void LogDto(string dtoString)
		{
			Log("Len: " + dtoString.Length + ", " + dtoString);
		}

		public T With_DataContractSerializer<T>(T dto)
		{
			var dtoString = DataContractSerializer.Instance.Parse(dto);
			LogDto(dtoString);
			return DataContractDeserializer.Instance.Parse<T>(dtoString);
		}

		public T With_JsonDataContractSerializer<T>(T dto)
		{
			var dtoString = JsonDataContractSerializer.Instance.Parse(dto);
			LogDto(dtoString);
			return JsonDataContractDeserializer.Instance.Parse<T>(dtoString);
		}

		public static byte[] ProtoBufToBytes<T>(T dto)
		{
			using (var ms = new MemoryStream())
			{
				Serializer.Serialize(ms, dto);
				var bytes = ms.ToArray();
				return bytes;
			}
		}

		public static T ProtoBufFromBytes<T>(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			{
				return Serializer.Deserialize<T>(ms);
			}
		}

		public T With_ProtoBuf<T>(T dto)
		{
			var bytes = ProtoBufToBytes(dto);

			Log("Len: " + bytes.Length + ", {protobuf bytes}");

			return ProtoBufFromBytes<T>(bytes);
		}

		public T With_JsonNet<T>(T dto)
		{
			var dtoString = JsonConvert.SerializeObject(dto);
			LogDto(dtoString);
			return JsonConvert.DeserializeObject<T>(dtoString);
		}

		public T With_StringSerializer<T>(T dto)
		{
			var dtoString = StringSerializer.SerializeToString(dto);
			LogDto(dtoString);
			return StringSerializer.DeserializeFromString<T>(dtoString);
		}

		public T With_TextSerializer<T>(T dto)
		{
			var dtoString = TextSerializer.SerializeToString(dto);
			LogDto(dtoString);
			return TextSerializer.DeserializeFromString<T>(dtoString);
		}

		protected void AssertEqual<T>(T dto, T originalDto)
		{
			Assert.That(originalDto.Equals(dto));
		}

		protected void AssertAllAreEqual<T>(T dto)
		{
			AssertEqual(With_DataContractSerializer(dto), dto);
			AssertEqual(With_JsonDataContractSerializer(dto), dto);
			try
			{
				AssertEqual(With_ProtoBuf(dto), dto);
			}
			catch (Exception ex)
			{
				Log("AssertEqual Error in ProtoBuf: {0}", ex);
			}
			AssertEqual(With_JsonNet(dto), dto);
			AssertEqual(With_StringSerializer(dto), dto);
			try
			{
				AssertEqual(With_TextSerializer(dto), dto);
			}
			catch (Exception ex)
			{
				Log("AssertEqual Error in TextSerializer: {0}", ex);
			}
		}

		protected void SerializeDto<T>(T dto)
		{
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var totalAvg = RunMultipleTimes(() => DataContractSerializer.Instance.Parse(dto), "DataContractSerializer.Instance.Parse(dto)");
			totalAvg += RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<T>(dtoXml), "DataContractDeserializer.Instance.Parse<T>(dtoXml)");
			Log("Len: " + dtoXml.Length);
			Log("Total Avg: " + totalAvg / 2);

			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			totalAvg = RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(dto), "JsonDataContractSerializer.Instance.Parse(dto)");
			totalAvg += RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<T>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<T>(dtoJson)");
			Log("Total Avg: " + totalAvg / 2);

			//Very slow
			//var dtoJayrock = JsonConvert.ExportToString(dto);
			//RunMultipleTimes(() => JsonConvert.ExportToString(dto), "JsonConvert.ExportToString(dto)");
			//RunMultipleTimes(() => JsonConvert.Import(typeof(T), dtoJayrock), "JsonConvert.Import(typeof(T), dtoJayrock)");

			try
			{
				var dtoProtoBuf = ProtoBufToBytes(dto);
				totalAvg = RunMultipleTimes(() => ProtoBufToBytes(dto), "ProtoBufToBytes(customer)");
				totalAvg += RunMultipleTimes(() => ProtoBufFromBytes<T>(dtoProtoBuf), "ProtoBufFromBytes<T>(dtoProtoBuf)");
				Log("Total Avg: " + totalAvg / 2);
			}
			catch (Exception ex)
			{
				Log("Error in ProtoBuf: {0}", ex);
			}

			var dtoJsonNet = JsonConvert.SerializeObject(dto);
			totalAvg = RunMultipleTimes(() => JsonConvert.SerializeObject(dto), "JsonConvert.SerializeObject(dto)");
			totalAvg += RunMultipleTimes(() => JsonConvert.DeserializeObject<T>(dtoJsonNet), "JsonConvert.DeserializeObject<T>(dtoJsonNet)");
			Log("Total Avg: " + totalAvg / 2);

			var dtoString = StringSerializer.SerializeToString(dto);
			totalAvg = RunMultipleTimes(() => StringSerializer.SerializeToString(dto), "StringSerializer.SerializeToString(dto)");
			totalAvg += RunMultipleTimes(() => StringSerializer.DeserializeFromString<T>(dtoString), "StringSerializer.DeserializeFromString<T>(dtoString)");
			Log("Len: " + dtoString.Length);
			Log("Total Avg: " + totalAvg / 2);

			var dtoPlatformText = TextSerializer.SerializeToString(dto);
			totalAvg = RunMultipleTimes(() => TextSerializer.SerializeToString(dto), "TextSerializer.SerializeToString(dto)");
			totalAvg += RunMultipleTimes(() => TextSerializer.DeserializeFromString<T>(dtoPlatformText), "TextSerializer.DeserializeFromString<T>(dtoPlatformText)");
			Log("Total Avg: " + totalAvg / 2);
		}
	}
}