using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Tests.Support;

namespace ServiceStack.Redis.Tests.Generic
{
    // TODO: error, missing fixture?
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

    [TestFixture]
    public class RedisClientListTestsString
        : RedisClientListTestsBase<string>
    {
        private readonly IModelFactory<string> factory = new BuiltInsFactory();

        protected override IModelFactory<string> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientListTestsShipper
        : RedisClientListTestsBase<Shipper>
    {
        private readonly IModelFactory<Shipper> factory = new ShipperFactory();

        protected override IModelFactory<Shipper> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientListTestsInt
        : RedisClientListTestsBase<int>
    {
        private readonly IModelFactory<int> factory = new IntFactory();

        protected override IModelFactory<int> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientListTestsCustomType
        : RedisClientSetTestsBase<CustomType>
    {
        private readonly IModelFactory<CustomType> factory = new CustomTypeFactory();

        protected override IModelFactory<CustomType> Factory
        {
            get { return factory; }
        }
    }

    [TestFixture]
    public class RedisClientlistTestCustomType_Failing
        : RedisClientListTestsBase<CustomType>
    {
        private readonly IModelFactory<CustomType> factory = new CustomTypeFactory();

        protected override IModelFactory<CustomType> Factory
        {
            get { return factory; }
        }
    }

    //public class RedisClientListTestsDateTime
    //    : RedisClientListTestsBase<DateTime>
    //{
    //    private readonly IModelFactory<DateTime> factory = new DateTimeFactory();

    //    protected override IModelFactory<DateTime> Factory
    //    {
    //        get { return factory; }
    //    }
    //}
}