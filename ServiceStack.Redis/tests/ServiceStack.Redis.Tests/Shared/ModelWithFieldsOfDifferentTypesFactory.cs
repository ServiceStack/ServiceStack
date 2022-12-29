using System;

namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithFieldsOfDifferentTypesFactory
        : ModelFactoryBase<ModelWithFieldsOfDifferentTypes>
    {
        public static ModelWithFieldsOfDifferentTypesFactory Instance
            = new ModelWithFieldsOfDifferentTypesFactory();

        public override void AssertIsEqual(
            ModelWithFieldsOfDifferentTypes actual, ModelWithFieldsOfDifferentTypes expected)
        {
            ModelWithFieldsOfDifferentTypes.AssertIsEqual(actual, expected);
        }

        public override ModelWithFieldsOfDifferentTypes CreateInstance(int i)
        {
            return ModelWithFieldsOfDifferentTypes.CreateConstant(i);
        }
    }
}