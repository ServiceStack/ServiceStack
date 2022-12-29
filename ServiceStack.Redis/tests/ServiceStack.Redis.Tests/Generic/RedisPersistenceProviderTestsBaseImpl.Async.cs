using System;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Redis.Tests.Generic
{
    public class RedisPersistenceProviderTestsModelWithFieldsOfDifferentTypesAsync
        : RedisPersistenceProviderTestsBaseAsync<ModelWithFieldsOfDifferentTypes>
    {
        private readonly IModelFactory<ModelWithFieldsOfDifferentTypes> factory =
            new ModelWithFieldsOfDifferentTypesFactory();

        protected override IModelFactory<ModelWithFieldsOfDifferentTypes> Factory
        {
            get { return factory; }
        }
    }

    public class RedisPersistenceProviderTestsStringFactoryAsync
        : RedisPersistenceProviderTestsBaseAsync<string>
    {
        private readonly IModelFactory<string> factory = new BuiltInsFactory();

        protected override IModelFactory<string> Factory
        {
            get { return factory; }
        }
    }

    public class RedisPersistenceProviderTestsShipperAsync
        : RedisPersistenceProviderTestsBaseAsync<Shipper>
    {
        private readonly IModelFactory<Shipper> factory = new ShipperFactory();

        protected override IModelFactory<Shipper> Factory
        {
            get { return factory; }
        }
    }

}