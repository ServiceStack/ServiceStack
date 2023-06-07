using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    public class TypeInfoTests
    {
        [Serializable]
        class MyClass : IComparable
        {
            public int CompareTo(object obj)
            {
                return 0;
            }
        }

        [Test]
#if !NETCORE
        [TestCase("[{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[{ '__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[{\n'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[{\t'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[ {'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[\n{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[\t{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[ { '__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[\n{\n'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[\t{\t'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'} ]")]
        [TestCase("[{ '__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}\t]")]
        [TestCase("[{\n'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}\n]")]
        [TestCase("[{\t'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass' }]")]
        [TestCase("[ {'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'\t}]")]
        [TestCase("[\n{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'\n}]")]
        [TestCase("[\t{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'\t}\n]")]
        [TestCase("[ { '__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass', }]")]
        [TestCase("[\n{\n'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
        [TestCase("[\t{\t'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass'}]")]
#else
	//In .NET Core (NET Standard 1.1) there is no way to find type in all assemblies without specifing assembly of the type
	//Methods for enumerating all loaded assemblies or get all loaded types are absent
	//So in this case we have to specify assembly of the type explicity
	//See https://github.com/dotnet/coreclr/issues/919
        [TestCase("[{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[{ '__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[{\n'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[{\t'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[ {'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[\n{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[\t{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[ { '__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[\n{\n'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[\t{\t'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'} ]")]
        [TestCase("[{ '__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}\t]")]
        [TestCase("[{\n'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}\n]")]
        [TestCase("[{\t'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests' }]")]
        [TestCase("[ {'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'\t}]")]
        [TestCase("[\n{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'\n}]")]
        [TestCase("[\t{'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'\t}\n]")]
        [TestCase("[ { '__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests', }]")]
        [TestCase("[\n{\n'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
        [TestCase("[\t{\t'__type':'ServiceStack.Text.Tests.JsonTests.TypeInfoTests+MyClass, ServiceStack.Text.Tests'}]")]
#endif
        public void TypeAttrInObject(string json)
        {
            json = json.Replace('\'', '"');
            var deserDto = JsonSerializer.DeserializeFromString<List<IComparable>>(json);
            Console.WriteLine(json);
            Assert.IsNotNull(deserDto);
            Assert.AreEqual(1, deserDto.Count);
            Assert.IsNotNull(deserDto[0]);
        }
    }
}