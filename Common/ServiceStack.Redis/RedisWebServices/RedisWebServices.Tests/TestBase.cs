using System;
using System.Collections.Generic;
using NUnit.Framework;
using RedisWebServices.ServiceModel.Operations.Admin;
using RedisWebServices.ServiceModel.Operations.Common;
using RedisWebServices.ServiceModel.Operations.Hash;
using RedisWebServices.ServiceModel.Operations.List;
using RedisWebServices.ServiceModel.Operations.Set;
using RedisWebServices.ServiceModel.Operations.SortedSet;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.Common.Extensions;
using ServiceStack.Configuration;
using ServiceStack.Redis;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.Tests
{
	[TestFixture]
	public class TestBase
	{
		protected const string TestKey = "testkey";
		protected const string TestValue = "Hello";
		protected static readonly List<string> StringValues = new List<string> { "A", "B", "C" };
		protected static List<int> IntValues = new List<int> { 1, 2, 3 };


		public static TestHost TestHost;
		public static TestConfig TestConfig;

		static TestBase()
		{
			TestHost = new TestHost();
			TestHost.Init();

			TestConfig = new TestConfig(new ConfigurationResourceManager());
		}

		[TestFixtureSetUp]
		public virtual void TestFixtureSetUp()
		{
			this.ServiceClient = !TestConfig.RunIntegrationTests
				? (IServiceClient)new TestServiceClient(TestHost)
				: new XmlServiceClient(TestConfig.IntegrationTestsBaseUrl);
		}

		[SetUp]
		public virtual void OnBeforeEachTest()
		{
			FlushAll();
		}

		protected IServiceClient ServiceClient { get; set; }

		protected void SendOneWay(object request)
		{
			this.ServiceClient.SendOneWay(request);
		}

		protected T Send<T>(object request, Func<T, ResponseStatus> getResponseStatusFn)
		{
			var response = this.ServiceClient.Send<T>(request);
			var responseStatus = getResponseStatusFn(response);

			if (responseStatus.ErrorCode != null)
			{
				throw new ServiceResponseException(responseStatus);
			}

			return response;
		}

		protected void FlushAll()
		{
			this.Send<FlushAllResponse>(new FlushAll(), x => x.ResponseStatus);
		}

		protected void SetEntry(string key, string value)
		{
			this.Send<SetEntryResponse>(
				new SetEntry { Key = key, Value = value }, x => x.ResponseStatus);
		}

		protected void SetEntry(string key, string value, TimeSpan expireIn)
		{
			this.Send<SetEntryWithExpiryResponse>(
				new SetEntryWithExpiry { Key = key, Value = value, ExpireIn = expireIn },
				x => x.ResponseStatus);
		}

		protected string GetValue(string key)
		{
			var response = this.Send<GetValueResponse>(
				new GetValue { Key = key }, x => x.ResponseStatus);

			return response.Value;
		}

		protected void AddItemToList(string listId, string item)
		{
			this.Send<AddItemToListResponse>(
				new AddItemToList { Id = listId, Item = item }, x => x.ResponseStatus);
		}

		protected void AddItemToSet(string setId, string item)
		{
			this.Send<AddItemToSetResponse>(
				new AddItemToSet { Id = setId, Item = item }, x => x.ResponseStatus);
		}

		protected void AddItemToSortedSet(string setId, string item)
		{
			this.Send<AddItemToSortedSetResponse>(
				new AddItemToSortedSet { Id = setId, Item = item }, x => x.ResponseStatus);
		}

		protected void SetEntryInHash(string hashId, string key, string value)
		{
			this.Send<SetEntryInHashResponse>(
				new SetEntryInHash { Id = hashId, Key = key, Value = value },
				x => x.ResponseStatus);
		}

		protected void SetRangeInHash(string hashId, Dictionary<string, string> entries)
		{
			var kvps = entries.ConvertAll(x => new KeyValuePair(x.Key, x.Value));
			this.Send<SetRangeInHashResponse>(
				new SetRangeInHash { Id = hashId, KeyValuePairs = kvps },
				x => x.ResponseStatus);
		}

		protected string GetValueFromHash(string hashId, string key)
		{
			var response = this.Send<GetValueFromHashResponse>(
				new GetValueFromHash { Id = hashId, Key = key }, x => x.ResponseStatus);

			return response.Value;
		}

		protected Dictionary<string, string> GetAllEntriesFromHash(string hashId)
		{
			var response = this.Send<GetAllEntriesFromHashResponse>(
				new GetAllEntriesFromHash { Id = hashId, }, x => x.ResponseStatus);

			var stringMap = new Dictionary<string, string>();
			response.KeyValuePairs.ForEach(x => stringMap[x.Key] = x.Value);
			return stringMap;
		}

		protected void AddRangeToList(string listId, List<string> items)
		{
			this.Send<AddRangeToListResponse>(
				new AddRangeToList { Id = listId, Items = items }, x => x.ResponseStatus);
		}

		protected string GetItemFromList(string listId, int index)
		{
			var response = this.Send<GetItemFromListResponse>(
				new GetItemFromList { Id = listId, Index = index }, x => x.ResponseStatus);

			return response.Item;
		}

		protected List<string> GetAllItemsFromList(string listId)
		{
			var response = this.Send<GetAllItemsFromListResponse>(
				new GetAllItemsFromList { Id = listId }, x => x.ResponseStatus);

			return response.Items;
		}

		protected string PopItemFromSet(string setId)
		{
			var response = this.Send<PopItemFromSetResponse>(
				new PopItemFromSet { Id = setId }, x => x.ResponseStatus);

			return response.Item;
		}

		protected void AddRangeToSet(string setId, List<string> items)
		{
			this.Send<AddRangeToSetResponse>(
				new AddRangeToSet { Id = setId, Items = items }, x => x.ResponseStatus);
		}

		protected List<string> GetAllItemsFromSet(string setId)
		{
			var response = this.Send<GetAllItemsFromSetResponse>(
				new GetAllItemsFromSet { Id = setId }, x => x.ResponseStatus);

			return response.Items;
		}

		protected void AddRangeToSortedSet(string setId, List<string> items)
		{
			var iws = items.ConvertAll(x => new ItemWithScore(x, 0));
			this.Send<AddRangeToSortedSetResponse>(
				new AddRangeToSortedSet { Id = setId, Items = iws }, x => x.ResponseStatus);
		}

		protected void AddRangeToSortedSet(string setId, Dictionary<string, double> itemsMap)
		{
			var iws = itemsMap.ConvertAll(x => new ItemWithScore(x.Key, x.Value));
			this.Send<AddRangeToSortedSetResponse>(
				new AddRangeToSortedSet { Id = setId, Items = iws }, x => x.ResponseStatus);
		}

		protected string PopItemWithHighestScoreFromSortedSet(string setId)
		{
			var response = this.Send<PopItemWithHighestScoreFromSortedSetResponse>(
				new PopItemWithHighestScoreFromSortedSet { Id = setId }, x => x.ResponseStatus);

			return response.Item;
		}

		protected List<string> GetAllItemsFromSortedSet(string setId)
		{
			var response = this.Send<GetAllItemsFromSortedSetResponse>(
				new GetAllItemsFromSortedSet { Id = setId }, x => x.ResponseStatus);

			return response.Items;
		}

	}
}