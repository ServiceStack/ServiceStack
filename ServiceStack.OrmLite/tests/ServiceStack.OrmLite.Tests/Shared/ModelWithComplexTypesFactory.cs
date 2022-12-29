namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithComplexTypesFactory
        : ModelFactoryBase<ModelWithComplexTypes>
    {
        public static ModelWithComplexTypesFactory Instance
            = new ModelWithComplexTypesFactory();

        public override void AssertIsEqual(
            ModelWithComplexTypes actual, ModelWithComplexTypes expected)
        {
            ModelWithComplexTypes.AssertIsEqual(actual, expected);
        }

        public override ModelWithComplexTypes CreateInstance(int i)
        {
            return ModelWithComplexTypes.CreateConstant(i);
        }
    }
}