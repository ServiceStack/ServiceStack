using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class KeyValueDataContractDeserializerTests
	{
		[Test]
		public void Can_Create_RequestOfAllTypes_from_string_map()
		{
			var request = RequestOfAllTypes.Create(1);
			var map = new Dictionary<string, string> {
                {"Byte",request.Byte.ToString()},
                {"Char",request.Char.ToString()},
                {"DateTime",request.DateTime.ToShortestXsdDateTimeString()},
                {"Decimal",request.Decimal.ToString()},
                {"Double",request.Double.ToString()},
                {"Float",request.Float.ToString()},
                {"Guid",request.Guid.ToString()},
                {"Int",request.Int.ToString()},
                {"Long",request.Long.ToString()},
                {"Short",request.Short.ToString()},
                {"String",request.String},
                {"TimeSpan",request.TimeSpan.ToString()},
                {"UInt",request.UInt.ToString()},
                {"ULong",request.ULong.ToString()},
			};

			var toRequest = KeyValueDataContractDeserializer.Instance.Parse(map, typeof(RequestOfAllTypes));

			Assert.That(request.Equals(toRequest), Is.True);
		}

		[Test]
		public void Can_Create_RequestOfAllTypes_from_partial_string_map()
		{
			var request = RequestOfAllTypes.Create(1);
			var map = new Dictionary<string, string> {
                {"Byte",request.Byte.ToString()},
                {"DateTime",request.DateTime.ToShortestXsdDateTimeString()},
                {"Double",request.Double.ToString()},
                {"Guid",request.Guid.ToString()},
                {"Long",request.Long.ToString()},
                {"String",request.String},
                {"UInt",request.UInt.ToString()},
			};

			var toRequest = (RequestOfAllTypes)KeyValueDataContractDeserializer.Instance
				.Parse(map, typeof(RequestOfAllTypes));

			Assert.That(toRequest.Byte, Is.EqualTo(request.Byte));
			Assert.That(toRequest.DateTime, Is.EqualTo(request.DateTime));
			Assert.That(toRequest.Double, Is.EqualTo(request.Double));
			Assert.That(toRequest.Guid, Is.EqualTo(request.Guid));
			Assert.That(toRequest.Long, Is.EqualTo(request.Long));
			Assert.That(toRequest.String, Is.EqualTo(request.String));
			Assert.That(toRequest.UInt, Is.EqualTo(request.UInt));
		}

		[Test]
		public void Can_Create_RequestOfComplexTypes_from_string_map()
		{
			var request = RequestOfComplexTypes.Create(1);
			var map = new Dictionary<string, string> {
                {"IntArray",request.IntArray.Join()},
                {"IntList",request.IntList.Join()},
                {"IntMap",request.IntMap.Join()},
                {"StringArray",request.StringArray.Join()},
                {"StringList",request.StringList.Join()},
                {"StringMap",request.StringMap.Join()},
                {"StringIntMap",request.StringIntMap.Join()},
                {"RequestOfAllTypes",TypeSerializer.SerializeToString(request.RequestOfAllTypes)},
			};

			var toRequest = KeyValueDataContractDeserializer.Instance.Parse(map, typeof(RequestOfComplexTypes));

			Assert.That(request.Equals(toRequest), Is.True);
		}

	}

}