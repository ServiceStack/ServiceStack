using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Reflection;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class ReflectionUtilTests
    {
        public enum TestClassType
        {
            One = 1,
            Two = 2,
            Three = 3
        }

        public class TestClass2
        {
            public TestClassType Type { get; set; }
        }

        public class TestClass
        {
            [Required]
            public string Member1 { get; set; }

            public string Member2 { get; set; }

            [Required]
            public string Member3 { get; set; }

            [StringLength(1)]
            public string Member4 { get; set; }
        }

        public class DtoWithStringArray
        {
            public string[] Data { get; set; }
        }

        public class DtoWithEnumArray
        {
            public TestClassType[] Data { get; set; }
        }

        public class RecursiveDto
        {
            public string Name { get; set; }
            public RecursiveDto Child { get; set; }
        }

        public class DtoWithRecursiveArray
        {
            public RecursiveDto[] Paths { get; set; }
        }

        public class RecursiveArrayDto
        {
            public string Name { get; set; }
            public RecursiveArrayDto[] Nodes { get; set; }
        }

        public class MindTwister
        {
            public string Name { get; set; }
            public RecursiveArrayDto[] Arrays { get; set; }
            public Vortex Vortex { get; set; }
        }

        public class Vortex
        {
            public int Id { get; set; }
            public RecursiveArrayDto Arrays { get; set; }
            public MindTwister[] Twisters { get; set; }
        }

        [Test]
        public void Can_PopulateRecursiveDto()
        {
            var obj = (RecursiveDto)AutoMappingUtils.PopulateWith(new RecursiveDto());
            Assert.That(obj.Name, Is.Not.Null);
            Assert.IsNotNull(obj.Child);
            Assert.That(obj.Child.Name, Is.Not.Null);
        }

        [Test]
        public void Can_PopulateArrayOfRecursiveDto()
        {
            var obj = (DtoWithRecursiveArray)AutoMappingUtils.PopulateWith(new DtoWithRecursiveArray());
            Assert.IsNotNull(obj.Paths);
            Assert.Greater(obj.Paths.Length, 0);
            Assert.IsNotNull(obj.Paths[0]);
            Assert.That(obj.Paths[0].Name, Is.Not.Null);
            Assert.IsNotNull(obj.Paths[0].Child);
            Assert.That(obj.Paths[0].Child.Name, Is.Not.Null);
        }

        [Test]
        public void Can_PopulateRecursiveArrayDto()
        {
            var obj = (RecursiveArrayDto)AutoMappingUtils.PopulateWith(new RecursiveArrayDto());
            Assert.That(obj.Name, Is.Not.Null);
            Assert.IsNotNull(obj.Nodes[0]);
            Assert.That(obj.Nodes[0].Name, Is.Not.Null);
            Assert.IsNotNull(obj.Nodes[0].Nodes);
            Assert.That(obj.Nodes[0].Nodes[0].Name, Is.Not.Null);
        }

        [Test]
        public void Can_PopulateTheVortex()
        {
            var obj = (MindTwister)AutoMappingUtils.PopulateWith(new MindTwister());
            Console.WriteLine("Mindtwister = " + ServiceStack.Text.XmlSerializer.SerializeToString(obj)); // TypeSerializer and JsonSerializer blow up on this structure with a Null Reference Exception!
            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.Name);
            Assert.IsNotNull(obj.Arrays);
            Assert.IsNotNull(obj.Vortex);
        }

        [Test]
        public void Can_PopulateObjectWithStringArray()
        {
            var obj = (DtoWithStringArray)AutoMappingUtils.PopulateWith(new DtoWithStringArray());
            Assert.IsNotNull(obj.Data);
            Assert.Greater(obj.Data.Length, 0);
            Assert.IsNotNull(obj.Data[0]);
        }

        [Test]
        public void Can_PopulateObjectWithNonZeroEnumArray()
        {
            var obj = (DtoWithEnumArray)AutoMappingUtils.PopulateWith(new DtoWithEnumArray());
            Assert.IsNotNull(obj.Data);
            Assert.Greater(obj.Data.Length, 0);
            Assert.That(Enum.IsDefined(typeof(TestClassType), obj.Data[0]), "Values in created array should be valid for the enum");
        }

        [Test]
        public void PopulateObject_UsesDefinedEnum()
        {
            var requestObj = (TestClass2)AutoMappingUtils.PopulateWith(Activator.CreateInstance(typeof(TestClass2)));
            Assert.True(Enum.IsDefined(typeof(TestClassType), requestObj.Type));
        }

        [Test]
        public void PopulateObject_UsesDefinedEnum_OnNestedTypes()
        {
            var requestObj = (Dictionary<string, TestClass2>)AutoMappingUtils.CreateDefaultValue(typeof(Dictionary<string, TestClass2>), new Dictionary<Type, int>());
            Assert.True(Enum.IsDefined(typeof(TestClassType), requestObj.First().Value.Type));
        }

        [Test]
        public void GetTest()
        {
            var propertyAttributes = AutoMappingUtils.GetPropertyAttributes<RequiredAttribute>(typeof(TestClass));
            var propertyNames = propertyAttributes.ToList().ConvertAll(x => x.Key.Name);
            Assert.That(propertyNames, Is.EquivalentTo(new[] { "Member1", "Member3" }));
        }

        [Test]
        public void Populate_Same_Objects()
        {
            var toObj = ModelWithFieldsOfDifferentTypes.Create(1);
            var fromObj = ModelWithFieldsOfDifferentTypes.Create(2);

            var obj3 = AutoMappingUtils.PopulateWith(toObj, fromObj);

            Assert.IsTrue(obj3 == toObj);
            Assert.That(obj3.Bool, Is.EqualTo(fromObj.Bool));
            Assert.That(obj3.DateTime, Is.EqualTo(fromObj.DateTime));
            Assert.That(obj3.Double, Is.EqualTo(fromObj.Double));
            Assert.That(obj3.Guid, Is.EqualTo(fromObj.Guid));
            Assert.That(obj3.Id, Is.EqualTo(fromObj.Id));
            Assert.That(obj3.LongId, Is.EqualTo(fromObj.LongId));
            Assert.That(obj3.Name, Is.EqualTo(fromObj.Name));
        }

        [Test]
        public void Populate_Different_Objects_with_different_property_types()
        {
            var toObj = ModelWithFieldsOfDifferentTypes.Create(1);
            var fromObj = ModelWithOnlyStringFields.Create("2");

            var obj3 = AutoMappingUtils.PopulateWith(toObj, fromObj);

            Assert.IsTrue(obj3 == toObj);
            Assert.That(obj3.Id, Is.EqualTo(2));
            Assert.That(obj3.Name, Is.EqualTo(fromObj.Name));
        }

        [Test]
        public void Populate_From_Properties_With_Attribute()
        {
            var originalToObj = ModelWithOnlyStringFields.Create("id-1");
            var toObj = ModelWithOnlyStringFields.Create("id-1");
            var fromObj = ModelWithOnlyStringFields.Create("id-2");

            AutoMappingUtils.PopulateFromPropertiesWithAttribute(toObj, fromObj,
                typeof(IndexAttribute));

            Assert.That(toObj.Id, Is.EqualTo(originalToObj.Id));
            Assert.That(toObj.AlbumId, Is.EqualTo(originalToObj.AlbumId));

            //Properties with IndexAttribute
            Assert.That(toObj.Name, Is.EqualTo(fromObj.Name));
            Assert.That(toObj.AlbumName, Is.EqualTo(fromObj.AlbumName));
        }

        [Test]
        public void Populate_From_Properties_With_Non_Default_Values()
        {
            var toObj = ModelWithFieldsOfDifferentTypes.Create(1);
            var fromObj = ModelWithFieldsOfDifferentTypes.Create(2);

            var originalToObj = ModelWithFieldsOfDifferentTypes.Create(1);
            var originalGuid = toObj.Guid;

            fromObj.Name = null;
            fromObj.Double = default(double);
            fromObj.Guid = default(Guid);

            toObj.PopulateWithNonDefaultValues(fromObj);

            Assert.That(toObj.Name, Is.EqualTo(originalToObj.Name));
            Assert.That(toObj.Double, Is.EqualTo(originalToObj.Double));
            Assert.That(toObj.Guid, Is.EqualTo(originalGuid));

            Assert.That(toObj.Id, Is.EqualTo(fromObj.Id));
            Assert.That(toObj.LongId, Is.EqualTo(fromObj.LongId));
            Assert.That(toObj.Bool, Is.EqualTo(fromObj.Bool));
            Assert.That(toObj.DateTime, Is.EqualTo(fromObj.DateTime));
        }

        [Test]
        public void Populate_From_Nullable_Properties_With_Non_Default_Values()
        {
            var toObj = ModelWithFieldsOfDifferentTypes.Create(1);
            var fromObj = ModelWithFieldsOfDifferentTypesAsNullables.Create(2);

            var originalToObj = ModelWithFieldsOfDifferentTypes.Create(1);

            fromObj.Name = null;
            fromObj.Double = default(double);
            fromObj.Guid = default(Guid);
            fromObj.Bool = default(bool);

            toObj.PopulateWithNonDefaultValues(fromObj);

            Assert.That(toObj.Name, Is.EqualTo(originalToObj.Name));

            Assert.That(toObj.Double, Is.EqualTo(fromObj.Double));
            Assert.That(toObj.Guid, Is.EqualTo(fromObj.Guid));
            Assert.That(toObj.Bool, Is.EqualTo(fromObj.Bool));
            Assert.That(toObj.Id, Is.EqualTo(fromObj.Id));
            Assert.That(toObj.LongId, Is.EqualTo(fromObj.LongId));
            Assert.That(toObj.DateTime, Is.EqualTo(fromObj.DateTime));
        }

        [Test]
        public void Translate_Between_Models_of_differrent_types_and_nullables()
        {
            var fromObj = ModelWithFieldsOfDifferentTypes.CreateConstant(1);

            var toObj = fromObj.ConvertTo<ModelWithFieldsOfDifferentTypesAsNullables>();

            Console.WriteLine(toObj.Dump());

            ModelWithFieldsOfDifferentTypesAsNullables.AssertIsEqual(fromObj, toObj);
        }

        [Test]
        public void Translate_Between_Models_of_nullables_and_differrent_types()
        {
            var fromObj = ModelWithFieldsOfDifferentTypesAsNullables.CreateConstant(1);

            var toObj = fromObj.ConvertTo<ModelWithFieldsOfDifferentTypes>();

            Console.WriteLine(toObj.Dump());

            ModelWithFieldsOfDifferentTypesAsNullables.AssertIsEqual(toObj, fromObj);
        }

        [Test]
        public void Can_get_result_of_Task()
        {
            var tcs = new TaskCompletionSource<string>();
            var task = tcs.Task;
            tcs.SetResult("foo");

            var fn = task.GetType().GetFastGetter("Result");
            var value = fn(task);
            Assert.That(value, Is.EqualTo("foo"));

            fn = task.GetType().GetFastGetter("Result");
            value = fn(task);
            Assert.That(value, Is.EqualTo("foo"));
        }
    }
}
