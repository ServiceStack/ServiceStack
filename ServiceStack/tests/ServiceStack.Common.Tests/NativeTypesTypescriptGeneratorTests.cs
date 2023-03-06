using NUnit.Framework;
using ServiceStack.NativeTypes.TypeScript;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class NativeTypesTypescriptGeneratorTests
    {
        [Test]
        public void TypeScript_Generator_Generates_2D_Array()
        {
            // arrange
            var sut = new TypeScriptGenerator(null);

            // act
            var result = sut.Type("String[][]", null);

            // assert
            Assert.AreEqual("string[][]", result);
        }

        [Test]
        public void TypeScript_Generator_Generates_Single_Dimension_Array()
        {
            // arrange
            var sut = new TypeScriptGenerator(null);

            // act
            var result = sut.Type("String[]", null);

            // assert
            Assert.AreEqual("string[]", result);
        }
    }
}
