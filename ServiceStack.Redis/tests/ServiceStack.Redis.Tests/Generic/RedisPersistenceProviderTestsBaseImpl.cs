using System;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Redis.Tests.Generic
{
    public class RedisPersistenceProviderTestsModelWithFieldsOfDifferentTypes
        : RedisPersistenceProviderTestsBase<ModelWithFieldsOfDifferentTypes>
    {
        private readonly IModelFactory<ModelWithFieldsOfDifferentTypes> factory =
            new ModelWithFieldsOfDifferentTypesFactory();

        protected override IModelFactory<ModelWithFieldsOfDifferentTypes> Factory
        {
            get { return factory; }
        }
    }

    public class RedisPersistenceProviderTestsStringFactory
        : RedisPersistenceProviderTestsBase<string>
    {
        private readonly IModelFactory<string> factory = new BuiltInsFactory();

        protected override IModelFactory<string> Factory
        {
            get { return factory; }
        }
    }

    public class RedisPersistenceProviderTestsShipper
        : RedisPersistenceProviderTestsBase<Shipper>
    {
        private readonly IModelFactory<Shipper> factory = new ShipperFactory();

        protected override IModelFactory<Shipper> Factory
        {
            get { return factory; }
        }
    }

}