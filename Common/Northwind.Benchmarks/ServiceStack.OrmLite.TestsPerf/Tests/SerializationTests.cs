using System;
using System.Collections.Generic;
using System.IO;
using Northwind.Common.ComplexModel;
using Northwind.Perf;
using NUnit.Framework;
using Platform.Text;
using ProtoBuf;
using ServiceStack.Common.Text;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.OrmLite.TestsPerf.Tests
{
	[TestFixture]
	public class SerializationTests
		: PerfTestBase
	{
		//private const bool DoSerializationPerf = true;
		//private const bool DoDeserializationPerf = true;

		public SerializationTests()
		{
			this.MultipleIterations = new List<int> { 1000, 10000 };

			Serializer.GlobalOptions.InferTagFromName = true;
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

		public byte[] ProtoBufToBytes<T>(T dto)
		{
			using (var ms = new MemoryStream())
			{
				Serializer.Serialize(ms, dto);
				var bytes = ms.ToArray();
				return bytes;
			}
		}

		public T ProtoBufFromBytes<T>(byte[] bytes)
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

		public void AssertEqual<T>(T dto, T originalDto)
		{
			Assert.That(originalDto.Equals(dto));
		}

		private void AssertAllAreEqual<T>(T dto)
		{
			AssertEqual(With_DataContractSerializer(dto), dto);
			AssertEqual(With_JsonDataContractSerializer(dto), dto);
			try
			{
				AssertEqual(With_ProtoBuf(dto), dto);
			}
			catch (Exception)
			{
				Log("Error in ProtoBuf");
			}
			AssertEqual(With_StringSerializer(dto), dto);
			try
			{
				AssertEqual(With_TextSerializer(dto), dto);
			}
			catch (Exception)
			{
				Log("Error in ProtoBuf");
			}
		}

		private void DoAll<T>(T dto)
		{
			AssertAllAreEqual(dto);

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var totalAvg = RunMultipleTimes(() => DataContractSerializer.Instance.Parse(dto), "DataContractSerializer.Instance.Parse(dto)");
			totalAvg += RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<T>(dtoXml), "DataContractDeserializer.Instance.Parse<T>(dtoXml)");
			Log("Total Avg: " + totalAvg / 2);

			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			totalAvg = RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(dto), "JsonDataContractSerializer.Instance.Parse(dto)");
			totalAvg += RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<T>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<T>(dtoJson)");
			Log("Total Avg: " + totalAvg / 2);

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
			catch (Exception)
			{
				Log("Error in ProtoBuf");
			}

			var dtoString = StringSerializer.SerializeToString(dto);
			totalAvg = RunMultipleTimes(() => StringSerializer.SerializeToString(dto), "StringSerializer.SerializeToString(dto)");
			totalAvg += RunMultipleTimes(() => StringSerializer.DeserializeFromString<T>(dtoString), "StringSerializer.DeserializeFromString<T>(dtoString)");
			Log("Total Avg: " + totalAvg / 2);

			var dtoPlatformText = TextSerializer.SerializeToString(dto);
			totalAvg = RunMultipleTimes(() => TextSerializer.SerializeToString(dto), "TextSerializer.SerializeToString(dto)");
			totalAvg += RunMultipleTimes(() => TextSerializer.DeserializeFromString<T>(dtoPlatformText), "TextSerializer.DeserializeFromString<T>(dtoPlatformText)");
			Log("Total Avg: " + totalAvg / 2);
		}

		[Test]
		public void deserialize_Customer()
		{
			DoAll(DtoFactory.CustomerDto);
		}

		[Test]
		public void deserialize_Order()
		{
			DoAll(DtoFactory.OrderDto);
		}

		[Test]
		public void deserialize_Supplier()
		{
			DoAll(DtoFactory.SupplierDto);
		}

		[Test]
		public void deserialize_MultiOrderProperties()
		{
			DoAll(DtoFactory.MultiOrderProperties);
		}

		[Test]
		public void deserialize_MultiCustomerProperties()
		{
			DoAll(DtoFactory.MultiCustomerProperties);
		}

		[Test]
		public void deserialize_CustomerOrderArrayDto()
		{
			DoAll(DtoFactory.CustomerOrderArrayDto);
		}

		[Test]
		public void deserialize_CustomerOrderListDto()
		{
			DoAll(DtoFactory.CustomerOrderListDto);
		}

		[Test]
		public void deserialize_ArrayDtoWithOrders()
		{
			DoAll(DtoFactory.ArrayDtoWithOrders);
		}

	}

}