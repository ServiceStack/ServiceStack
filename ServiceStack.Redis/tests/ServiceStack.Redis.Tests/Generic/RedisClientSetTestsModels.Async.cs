using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Tests.Support;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture]
    public class RedisClientSetTestsModelWithFieldsOfDifferentTypesAsync
        : RedisClientSetTestsBaseAsync<ModelWithFieldsOfDifferentTypes>
    {
        private readonly IModelFactory<ModelWithFieldsOfDifferentTypes> factory =
            new ModelWithFieldsOfDifferentTypesFactory();

        protected override IModelFactory<ModelWithFieldsOfDifferentTypes> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientSetTestsStringAsync
        : RedisClientSetTestsBaseAsync<string>
    {
        private readonly IModelFactory<string> factory = new BuiltInsFactory();

        protected override IModelFactory<string> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientSetTestsShipperAsync
        : RedisClientSetTestsBaseAsync<Shipper>
    {
        private readonly IModelFactory<Shipper> factory = new ShipperFactory();

        protected override IModelFactory<Shipper> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientSetTestsIntAsync
        : RedisClientSetTestsBaseAsync<int>
    {
        private readonly IModelFactory<int> factory = new IntFactory();

        protected override IModelFactory<int> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientSetTestsCustomTypeAsync
        : RedisClientSetTestsBaseAsync<CustomType>
    {
        private readonly IModelFactory<CustomType> factory = new CustomTypeFactory();

        protected override IModelFactory<CustomType> Factory
        {
            get { return factory; }
        }
    }


    //public class RedisClientSetTestsDateTimeAsync
    //    : RedisClientSetTestsBaseAsync<DateTime>
    //{
    //    private readonly IModelFactory<DateTime> factory = new DateTimeFactory();

    //    protected override IModelFactory<DateTime> Factory
    //    {
    //        get { return factory; }
    //    }
    //}

}