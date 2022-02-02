using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Tests.Support;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture]
    public class RedisClientListTestsModelWithFieldsOfDifferentTypesAsync
        : RedisClientListTestsBaseAsync<ModelWithFieldsOfDifferentTypes>
    {
        private readonly IModelFactory<ModelWithFieldsOfDifferentTypes> factory =
            new ModelWithFieldsOfDifferentTypesFactory();

        protected override IModelFactory<ModelWithFieldsOfDifferentTypes> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientListTestsStringAsync
        : RedisClientListTestsBaseAsync<string>
    {
        private readonly IModelFactory<string> factory = new BuiltInsFactory();

        protected override IModelFactory<string> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientListTestsShipperAsync
        : RedisClientListTestsBaseAsync<Shipper>
    {
        private readonly IModelFactory<Shipper> factory = new ShipperFactory();

        protected override IModelFactory<Shipper> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientListTestsIntAsync
        : RedisClientListTestsBaseAsync<int>
    {
        private readonly IModelFactory<int> factory = new IntFactory();

        protected override IModelFactory<int> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientListTestsCustomTypeAsync
        : RedisClientSetTestsBaseAsync<CustomType>
    {
        private readonly IModelFactory<CustomType> factory = new CustomTypeFactory();

        protected override IModelFactory<CustomType> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientlistTestCustomType_FailingAsync
        : RedisClientListTestsBaseAsync<CustomType>
    {
        private readonly IModelFactory<CustomType> factory = new CustomTypeFactory();

        protected override IModelFactory<CustomType> Factory
        {
            get { return factory; }
        }
    }

    //public class RedisClientListTestsDateTimeAsync
    //    : RedisClientListTestsBaseAsync<DateTime>
    //{
    //    private readonly IModelFactory<DateTime> factory = new DateTimeFactory();

    //    protected override IModelFactory<DateTime> Factory
    //    {
    //        get { return factory; }
    //    }
    //}
}