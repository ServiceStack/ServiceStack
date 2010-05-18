using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using RedisWebServices.ServiceModel.Operations.Common;
using RedisWebServices.ServiceModel.Types;

namespace RedisWebServices.Tests
{
	public class CommonOperationsTests
		: TestBase
	{
		[Test]
		public void Test_AppendToValue()
		{
			RedisExec(r => r.SetEntry(TestKey, "Hello"));

			var request = new AppendToValue { Key = TestKey, Value = ", World!" };
			var response = base.Send<AppendToValueResponse>(request, x => x.ResponseStatus);

			const string expected = "Hello, World!";
			Assert.That(response.ValueLength, Is.EqualTo(expected.Length));

			var value = RedisExec(r => r.GetValue(TestKey));

			Assert.That(value, Is.EqualTo(expected));
		}

		[Test]
		public void Test_ContainsKey()
		{
			RedisExec(r => r.SetEntry(TestKey, "Hello"));

			var response = base.Send<ContainsKeyResponse>(
				new ContainsKey { Key = TestKey }, x => x.ResponseStatus);

			Assert.That(response.Result, Is.True);

			var notExists = base.Send<ContainsKeyResponse>(
				new ContainsKey { Key = "notExists" }, x => x.ResponseStatus);

			Assert.That(notExists.Result, Is.False);
		}

		[Test]
		public void Test_DecrementValue()
		{
			RedisExec(r => r.SetEntry(TestKey, 10.ToString()));

			var response = base.Send<DecrementValueResponse>(
				new DecrementValue { Key = TestKey, DecrementBy = 2 }, x => x.ResponseStatus);

			Assert.That(response.Value, Is.EqualTo(10 - 2));
		}

		[Test]
		public void Test_Echo()
		{
			var response = base.Send<EchoResponse>(
				new Echo { Text = TestValue }, x => x.ResponseStatus);

			Assert.That(response.Text, Is.EqualTo(TestValue));
		}

		[Test]
		public void Test_ExpireEntry()
		{
			RedisExec(r => r.SetEntry(TestKey, TestValue));

			var response = base.Send<ExpireEntryResponse>(
				new ExpireEntry { Key = TestKey, ExpireIn = TimeSpan.FromSeconds(1) },
				x => x.ResponseStatus);

			Assert.That(response.Result, Is.True);

			var testValue = RedisExec(r => r.GetValue(TestKey));
			Assert.That(testValue, Is.EqualTo(TestValue));

			Thread.Sleep(TimeSpan.FromSeconds(2));

			var refreshedValue = RedisExec(r => r.GetValue(TestKey));
			Assert.That(refreshedValue, Is.Null);
		}

		[Test]
		public void Test_GetAllKeys()
		{
			StringValues.ForEach(x =>
				RedisExec(r => r.SetEntry(x, TestValue)));

			var response = base.Send<GetAllKeysResponse>(
				new GetAllKeys(), x => x.ResponseStatus);

			Assert.That(response.Keys, Is.EquivalentTo(StringValues));
		}

		[Test]
		public void Test_GetAndSetEntry()
		{
			RedisExec(r => r.SetEntry(TestKey, "A"));

			var response = base.Send<GetAndSetEntryResponse>(
				new GetAndSetEntry { Key = TestKey, Value = "B" }, x => x.ResponseStatus);

			Assert.That(response.ExistingValue, Is.EqualTo("A"));

			var currentValue = RedisExec(r => r.GetValue(TestKey));
			Assert.That(currentValue, Is.EqualTo("B"));
		}

		[Test]
		public void Test_GetEntryType()
		{
			RedisExec(r => r.SetEntry(TestKey, TestValue));
			RedisExec(r => r.AddItemToList("TestList", TestValue));
			RedisExec(r => r.AddItemToSet("TestSet", TestValue));
			RedisExec(r => r.AddItemToSortedSet("TestSortedSet", TestValue));
			RedisExec(r => r.SetEntryInHash("TestHash", TestKey, TestValue));

			var responseString = base.Send<GetEntryTypeResponse>(
				new GetEntryType { Key = TestKey }, x => x.ResponseStatus);
			Assert.That(responseString.KeyType, Is.EqualTo(KeyType.String.ToString()));

			var responseList = base.Send<GetEntryTypeResponse>(
				new GetEntryType { Key = "TestList" }, x => x.ResponseStatus);
			Assert.That(responseList.KeyType, Is.EqualTo(KeyType.List.ToString()));

			var responseSet = base.Send<GetEntryTypeResponse>(
				new GetEntryType { Key = "TestSet" }, x => x.ResponseStatus);
			Assert.That(responseSet.KeyType, Is.EqualTo(KeyType.Set.ToString()));

			var responseSortedSet = base.Send<GetEntryTypeResponse>(
				new GetEntryType { Key = "TestSortedSet" }, x => x.ResponseStatus);
			Assert.That(responseSortedSet.KeyType, Is.EqualTo(KeyType.SortedSet.ToString()));

			var responseHash = base.Send<GetEntryTypeResponse>(
				new GetEntryType { Key = "TestHash" }, x => x.ResponseStatus);
			Assert.That(responseHash.KeyType, Is.EqualTo(KeyType.Hash.ToString()));
		}

		[Test]
		public void Test_GetRandomKey()
		{
			RedisExec(r => r.SetEntry(TestKey, TestValue));

			var response = base.Send<GetRandomKeyResponse>(
				new GetRandomKey(), x => x.ResponseStatus);

			Assert.That(response.Key, Is.EqualTo(TestKey));
		}

		[Test]
		[Ignore("TODO: requires a bit of work to test right")]
		public void Test_GetSortedEntryValues()
		{
		}

		[Test]
		public void Test_GetSubstring()
		{
			RedisExec(r => r.SetEntry(TestKey, TestValue));

			var response = base.Send<GetSubstringResponse>(
				new GetSubstring { Key = TestKey, FromIndex = 0, ToIndex = 5 }, x => x.ResponseStatus);

			Assert.That(response.Value, Is.EqualTo(TestValue.Substring(0, 5)));
		}

		[Test]
		public void Test_GetTimeToLive()
		{
			var expireIn = TimeSpan.FromSeconds(2);
			RedisExec(r => r.SetEntry(TestKey, TestValue, expireIn));

			var response = base.Send<GetTimeToLiveResponse>(
				new GetTimeToLive { Key = TestKey }, x => x.ResponseStatus);

			Assert.That(response.TimeRemaining, Is.LessThanOrEqualTo(expireIn));
		}

		[Test]
		public void Test_GetValue()
		{
			RedisExec(r => r.SetEntry(TestKey, TestValue));

			var response = base.Send<GetValueResponse>(
				new GetValue { Key = TestKey }, x => x.ResponseStatus);

			Assert.That(response.Value, Is.EqualTo(TestValue));
		}

		[Test]
		public void Test_GetValues()
		{
			StringValues.ForEach(x =>
				RedisExec(r => r.SetEntry(x, x)));

			var response = base.Send<GetValuesResponse>(
				new GetValues { Keys = StringValues }, x => x.ResponseStatus);

			Assert.That(response.Values, Is.EqualTo(StringValues));
		}

		[Test]
		public void Test_IncrementValue()
		{
			RedisExec(r => r.SetEntry(TestKey, 10.ToString()));

			var response = base.Send<IncrementValueResponse>(
				new IncrementValue { Key = TestKey, IncrementBy = 2 }, x => x.ResponseStatus);

			Assert.That(response.Value, Is.EqualTo(10 + 2));
		}

		[Test]
		public void Test_Ping()
		{
			var response = base.Send<PingResponse>(
				new Ping(), x => x.ResponseStatus);

			Assert.That(response.Result, Is.True);
		}

		[Test]
		public void Test_RemoveEntry()
		{
			RedisExec(r => r.SetEntry(TestKey, TestValue));

			var response = base.Send<RemoveEntryResponse>(
				new RemoveEntry { Keys = new List<string> { TestKey } }, x => x.ResponseStatus);

			Assert.That(response.Result, Is.True);

			var testValue = RedisExec(r => r.GetValue(TestKey));

			Assert.That(testValue, Is.Null);
		}

		[Test]
		public void Test_SearchKeys()
		{
			RedisExec(r => r.SetEntry("A1", TestValue));
			RedisExec(r => r.SetEntry("A2", TestValue));
			RedisExec(r => r.SetEntry("B", TestValue));
			RedisExec(r => r.SetEntry("C", TestValue));

			var response = base.Send<SearchKeysResponse>(
				new SearchKeys { Pattern = "A*" }, x => x.ResponseStatus);

			Assert.That(response.Keys, Is.EquivalentTo(new List<string> { "A1", "A2" }));
		}

		[Test]
		public void Test_SetEntry()
		{
			var response = base.Send<SetEntryResponse>(
				new SetEntry { Key = TestKey, Value = TestValue }, x => x.ResponseStatus);

			var testValue = RedisExec(r => r.GetValue(TestKey));

			Assert.That(testValue, Is.EqualTo(TestValue));
		}

		[Test]
		public void Test_SetEntryIfNotExists()
		{
			var response = base.Send<SetEntryIfNotExistsResponse>(
				new SetEntryIfNotExists { Key = "Key1", Value = "B" }, x => x.ResponseStatus);

			Assert.That(response.Result, Is.True);

			response = base.Send<SetEntryIfNotExistsResponse>(
				new SetEntryIfNotExists { Key = "Key1", Value = "C" }, x => x.ResponseStatus);

			Assert.That(response.Result, Is.False);
		}

	}

}