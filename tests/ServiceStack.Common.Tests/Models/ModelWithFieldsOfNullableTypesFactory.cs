namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithFieldsOfNullableTypesFactory
        : ModelFactoryBase<ModelWithFieldsOfNullableTypes>
    {
        public static ModelWithFieldsOfNullableTypesFactory Instance
            = new ModelWithFieldsOfNullableTypesFactory();

        public override void AssertIsEqual(
            ModelWithFieldsOfNullableTypes actual, ModelWithFieldsOfNullableTypes expected)
        {
            ModelWithFieldsOfNullableTypes.AssertIsEqual(actual, expected);
        }

        public override ModelWithFieldsOfNullableTypes CreateInstance(int i)
        {
            return ModelWithFieldsOfNullableTypes.CreateConstant(i);
        }
    }
}