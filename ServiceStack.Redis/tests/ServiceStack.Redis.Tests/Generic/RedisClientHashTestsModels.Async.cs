using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Tests.Support;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture]
    public class RedisClientHashTestsModelWithFieldsOfDifferentTypesAsync
        : RedisClientHashTestsBaseAsync<ModelWithFieldsOfDifferentTypes>
    {
        private readonly IModelFactory<ModelWithFieldsOfDifferentTypes> factory =
            new ModelWithFieldsOfDifferentTypesFactory();

        protected override IModelFactory<ModelWithFieldsOfDifferentTypes> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientHashTestsStringAsync
        : RedisClientHashTestsBaseAsync<string>
    {
        private readonly IModelFactory<string> factory = new BuiltInsFactory();

        protected override IModelFactory<string> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientHashTestsShipperAsync
        : RedisClientHashTestsBaseAsync<Shipper>
    {
        private readonly IModelFactory<Shipper> factory = new ShipperFactory();

        protected override IModelFactory<Shipper> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientHashTestsIntAsync
        : RedisClientHashTestsBaseAsync<int>
    {
        private readonly IModelFactory<int> factory = new IntFactory();

        protected override IModelFactory<int> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientHashTestsCustomTypeAsync
        : RedisClientSetTestsBaseAsync<CustomType>
    {
        private readonly IModelFactory<CustomType> factory = new CustomTypeFactory();

        protected override IModelFactory<CustomType> Factory
        {
            get { return factory; }
        }
    }

    //public class RedisClientHashTestsDateTimeAsync
    //    : RedisClientHashTestsBaseAsync<DateTime>
    //{
    //    private readonly IModelFactory<DateTime> factory = new DateTimeFactory();

    //    protected override IModelFactory<DateTime> Factory
    //    {
    //        get { return factory; }
    //    }
    //}

}