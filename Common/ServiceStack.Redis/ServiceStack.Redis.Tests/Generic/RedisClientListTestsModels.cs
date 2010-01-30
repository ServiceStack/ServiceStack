using System;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Redis.Tests.Generic
{
	public class RedisClientListTestsModelWithFieldsOfDifferentTypes
		: RedisClientListTestsBase<ModelWithFieldsOfDifferentTypes>
	{
		private readonly IModelFactory<ModelWithFieldsOfDifferentTypes> factory = 
			new ModelWithFieldsOfDifferentTypesFactory();

		protected override IModelFactory<ModelWithFieldsOfDifferentTypes> Factory
		{
			get { return factory; }
		}
	}

	public class RedisClientListTestsString
		: RedisClientListTestsBase<string>
	{
		private readonly IModelFactory<string> factory = new StringFactory();

		protected override IModelFactory<string> Factory
		{
			get { return factory; }
		}
	}

	public class RedisClientListTestsShipper
		: RedisClientListTestsBase<Shipper>
	{
		private readonly IModelFactory<Shipper> factory = new ShipperFactory();

		protected override IModelFactory<Shipper> Factory
		{
			get { return factory; }
		}
	}

}