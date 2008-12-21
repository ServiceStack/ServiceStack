using System;
using System.Linq;
using ServiceStack.Common.Services.Support.Config;
using ServiceStack.Common.Services.Tests.Support.DataContracts;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Common.Services.Tests.Serialization
{
    [TestFixture]
    public class StringConverterTests
    {
        [Test]
        public void create_super_list_type_from_string()
        {
            var dtoType = typeof(ArrayOfIntId);
            var textConverter = new StringConverter(dtoType);
            var textValue = "1,2,3";
            var convertedValue = textValue.Split(',').ToList().ConvertAll(x => Convert.ToInt32(x));
            var result = textConverter.Parse(textValue);
            Assert.That(result, Is.EquivalentTo(convertedValue));
        }
    }
}