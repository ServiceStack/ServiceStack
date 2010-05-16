using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using RedisWebServices.ServiceModel.Operations.Common;
using RedisWebServices.ServiceModel.Operations.Hash;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.Common.Extensions;

namespace RedisWebServices.Tests
{
	public class HashOperationsTests
		: TestBase
	{
		private const string HashId = "testhash";

		Dictionary<string, string> stringMap;
		Dictionary<string, int> stringIntMap;

		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();
			stringMap = new Dictionary<string, string> {
     			{"one","a"}, {"two","b"}, {"three","c"}, {"four","d"}
     		};
			stringIntMap = new Dictionary<string, int> {
     			{"one",1}, {"two",2}, {"three",3}, {"four",4}
     		};
		}

		[Test]
		public void Test_GetAllEntriesFromHash()
		{
			stringMap.ForEach(x => RedisExec(r => r.SetEntryInHash(HashId, x.Key, x.Value)));

			var response = base.Send<GetAllEntriesFromHashResponse>(
				new GetAllEntriesFromHash { Id = HashId }, x => x.ResponseStatus);

			var keyValuePairs = stringMap.ConvertAll(x => new KeyValuePair(x.Key, x.Value));

			Assert.That(response.KeyValuePairs, Is.EquivalentTo(keyValuePairs));
		}

		[Test]
		public void Test_GetHashCount()
		{
			stringMap.ForEach(x => RedisExec(r => r.SetEntryInHash(HashId, x.Key, x.Value)));

			var response = base.Send<GetHashCountResponse>(
				new GetHashCount { Id = HashId }, x => x.ResponseStatus);

			Assert.That(response.Count, Is.EqualTo(stringMap.Count));
		}

		[Test]
		public void Test_GetHashKeys()
		{
			stringMap.ForEach(x => RedisExec(r => r.SetEntryInHash(HashId, x.Key, x.Value)));

			var response = base.Send<GetHashKeysResponse>(
				new GetHashKeys { Id = HashId }, x => x.ResponseStatus);

			var keys = stringMap.ConvertAll(x => x.Key);
			Assert.That(response.Keys, Is.EquivalentTo(keys));
		}

		[Test]
		public void Test_GetHashValues()
		{
			stringMap.ForEach(x => RedisExec(r => r.SetEntryInHash(HashId, x.Key, x.Value)));

			var response = base.Send<GetHashValuesResponse>(
				new GetHashValues { Id = HashId }, x => x.ResponseStatus);

			var values = stringMap.ConvertAll(x => x.Value);
			Assert.That(response.Values, Is.EquivalentTo(values));
		}

		[Test]
		public void Test_GetValueFromHash()
		{
			stringMap.ForEach(x => RedisExec(r => r.SetEntryInHash(HashId, x.Key, x.Value)));

			var key = ((KeyValuePair<string, string>)stringMap.First()).Key;

			var response = base.Send<GetValueFromHashResponse>(
				new GetValueFromHash { Id = HashId, Key = key }, x => x.ResponseStatus);

			Assert.That(response.Value, Is.EqualTo(stringMap[key]));
		}

		[Test]
		public void Test_GetValuesFromHash()
		{
			stringMap.ForEach(x => RedisExec(r => r.SetEntryInHash(HashId, x.Key, x.Value)));

			var keys = stringMap.ConvertAll(x => x.Key);

			var response = base.Send<GetValuesFromHashResponse>(
				new GetValuesFromHash { Id = HashId, Keys = keys }, x => x.ResponseStatus);

			var values = stringMap.ConvertAll(x => x.Value);

			Assert.That(response.Values, Is.EqualTo(values));
		}

		[Test]
		public void Test_HashContainsEntry()
		{
			stringMap.ForEach(x => RedisExec(r => r.SetEntryInHash(HashId, x.Key, x.Value)));

			var key = ((KeyValuePair<string, string>)stringMap.First()).Key;

			var response = base.Send<HashContainsEntryResponse>(
				new HashContainsEntry { Id = HashId, Key = key }, x => x.ResponseStatus);
			Assert.That(response.Result, Is.True);

			response = base.Send<HashContainsEntryResponse>(
				new HashContainsEntry { Id = HashId, Key = "notexists" }, x => x.ResponseStatus);
			Assert.That(response.Result, Is.False);
		}

		[Test]
		public void Test_IncrementValueInHash()
		{
			RedisExec(r => r.SetEntryInHash(HashId, TestKey, 10.ToString()));

			var response = base.Send<IncrementValueInHashResponse>(
				new IncrementValueInHash { Id = HashId, Key = TestKey, IncrementBy = 2 }, x => x.ResponseStatus);

			Assert.That(response.Value, Is.EqualTo(10 + 2));
		}

		[Test]
		public void Test_RemoveEntryFromHash()
		{
			RedisExec(r => r.SetEntryInHash(HashId, TestKey, 10.ToString()));

			var response = base.Send<RemoveEntryFromHashResponse>(
				new RemoveEntryFromHash { Id = HashId, Key = TestKey }, x => x.ResponseStatus);
			Assert.That(response.Result, Is.True);

			var value = RedisExec(r => r.GetValueFromHash(HashId, TestKey));
			Assert.That(value, Is.Null);
		}

		[Test]
		public void Test_SetEntryInHash()
		{
			var response = base.Send<SetEntryInHashResponse>(
				new SetEntryInHash { Id = HashId, Key = TestKey, Value = TestValue }, x => x.ResponseStatus);

			Assert.That(response.Result, Is.True);

			var value = RedisExec(r => r.GetValueFromHash(HashId, TestKey));
			Assert.That(value, Is.EqualTo(TestValue));
		}

		[Test]
		public void Test_SetEntryInHashIfNotExists()
		{
			var response = base.Send<SetEntryInHashIfNotExistsResponse>(
				new SetEntryInHashIfNotExists { Id = HashId, Key = TestKey, Value = TestValue }, x => x.ResponseStatus);
			Assert.That(response.Result, Is.True);

			var value = RedisExec(r => r.GetValueFromHash(HashId, TestKey));
			Assert.That(value, Is.EqualTo(TestValue));

			response = base.Send<SetEntryInHashIfNotExistsResponse>(
				new SetEntryInHashIfNotExists { Id = HashId, Key = TestKey, Value = TestValue }, x => x.ResponseStatus);
			Assert.That(response.Result, Is.False);
		}

		[Test]
		public void Test_SetRangeInHash()
		{
			var keyValuePairs = stringMap.ConvertAll(x => new KeyValuePair(x.Key, x.Value));

			var response = base.Send<SetRangeInHashResponse>(
				new SetRangeInHash { Id = HashId, KeyValuePairs = keyValuePairs }, x => x.ResponseStatus);

			var allHashEntries = RedisExec(r => r.GetAllEntriesFromHash(HashId));

			Assert.That(allHashEntries, Is.EquivalentTo(stringMap));
		}

	}
}