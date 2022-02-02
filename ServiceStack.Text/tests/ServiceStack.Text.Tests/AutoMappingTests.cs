using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Xml.Linq;
#if !NETCORE
using System.Web.Script.Serialization;
#endif
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using ServiceStack.Text.Tests.DynamicModels;
using ServiceStack.Web;

namespace ServiceStack.Text.Tests
{
    public class User
    {
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        public Car Car { get; set; }
    }

    public class UserFields
    {
        public string FirstName;
        public string LastName;
        public Car Car;
    }

    public class SubUser : User { }
    public class SubUserFields : UserFields { }

    public class Car
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class SubCar : Car
    {
        public string Custom { get; set; }
    }

    public class CarWithSpecs : Car
    {
        public List<CarSpec> Specs { get; set; }
    }

    public class CarSpec
    {
        public string Item { get; set; }
        public string Value { get; set; }
    }

    public class UserDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Car { get; set; }
    }

    public class UsersData
    {
        public int Id { get; set; }
        
        public User User { get; set; }
        public List<User> UsersList { get; set; } = new List<User>();
        public Dictionary<int, User> UsersMap { get; set; } = new Dictionary<int, User>();
    }
    public class UsersDto
    {
        public int Id { get; set; }

        public UserDto User { get; set; }
        public List<UserDto> UsersList { get; set; } = new List<UserDto>();
        public Dictionary<int, UserDto> UsersMap { get; set; } = new Dictionary<int, UserDto>();
    }

    public enum Color
    {
        Red,
        Green,
        Blue
    }

    public enum OtherColor
    {
        Red,
        Green,
        Blue
    }


    public class IntNullableId
    {
        public int? Id { get; set; }
    }

    public class IntId
    {
        public int Id { get; set; }
    }

    public class BclTypes
    {
        public int Int { get; set; }
        public long Long { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
    }

    public class BclTypeStrings
    {
        public string Int { get; set; }
        public string Long { get; set; }
        public string Double { get; set; }
        public string Decimal { get; set; }
    }

    public class NullableConversion
    {
        public decimal Amount { get; set; }
    }

    public class NullableConversionDto
    {
        public decimal? Amount { get; set; }
    }

    public class DtoWithEnum
    {
        public string Name { get; set; }
        public Color Color { get; set; }
    }

    public class NullableEnumConversion
    {
        public Color Color { get; set; }
    }

    public class ReallyNullableEnumConversion
    {
        public Color? Color { get; set; }
    }

    public class EnumConversion
    {
        public Color Color { get; set; }
        public Color? NullableColor { get; set; }
    }

    public class NullableEnumConversionDto
    {
        public OtherColor? Color { get; set; }
    }

    public class EnumConversionDto
    {
        public OtherColor Color { get; set; }
    }

    public class EnumConversionString
    {
        public string Color { get; set; }
        public string NullableColor { get; set; }
    }

    public class EnumConversionInt
    {
        public int Color { get; set; }
        public int? NullableColor { get; set; }
    }

    public class ModelWithEnumerable
    {
        public IEnumerable<User> Collection { get; set; }
    }

    public class ModelWithList
    {
        public List<User> Collection { get; set; }
    }

    public class ModelWithArray
    {
        public User[] Collection { get; set; }
    }

    public class ModelWithHashSet
    {
        public HashSet<User> Collection { get; set; }
    }

    public class ModelWithIgnoredFields
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [ReadOnly]
        public int Ignored { get; set; }
    }

    public class ReadOnlyAttribute : AttributeBase { }
    
    [EnumAsInt]
    public enum MyEnumAsInt
    {
        ZeroValue = 0,
        OneValue = 1,
        DefaultValue = 2,
    };

    class MyEnumAsIntSource
    {
        public MyEnumAsInt MyEnumAsInt { get; set; } = MyEnumAsInt.DefaultValue;
    }

    class MyEnumAsIntTarget
    {
        public MyEnumAsInt MyEnumAsInt { get; set; } = MyEnumAsInt.DefaultValue;
    }

    [TestFixture]
    public class AutoMappingTests
    {
        [Test]
        public void Can_convert_Default_Enum_Values()
        {
            var from = new MyEnumAsIntSource { MyEnumAsInt = MyEnumAsInt.ZeroValue };
            var to = from.ConvertTo<MyEnumAsIntTarget>();
            
            Assert.That(to.MyEnumAsInt, Is.EqualTo(from.MyEnumAsInt));
        }
        
        [Test]
        public void Does_populate()
        {
            var user = new User {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var userDto = new UserDto().PopulateWith(user);

            Assert.That(userDto.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(userDto.LastName, Is.EqualTo(user.LastName));
            Assert.That(userDto.Car, Is.EqualTo("{Name:BMW X6,Age:3}"));
        }

        [Test]
        public void Does_translate()
        {
            var user = new User
            {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var userDto = user.ConvertTo<UserDto>();

            Assert.That(userDto.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(userDto.LastName, Is.EqualTo(user.LastName));
            Assert.That(userDto.Car, Is.EqualTo("{Name:BMW X6,Age:3}"));
        }

        [Test]
        public void Does_enumstringconversion_translate()
        {
            var conversion = new EnumConversion { Color = Color.Blue };
            var conversionDto = conversion.ConvertTo<EnumConversionString>();

            Assert.That(conversionDto.Color, Is.EqualTo("Blue"));
        }

        [Test]
        public void Does_convert_to_EnumConversionInt()
        {
            var conversion = new EnumConversion
            {
                Color = Color.Green,
                NullableColor = Color.Green,
            };
            var conversionDto = conversion.ConvertTo<EnumConversionInt>();

            Assert.That(conversionDto.Color, Is.EqualTo(1));
            Assert.That(conversionDto.NullableColor, Is.EqualTo(1));
        }

        [Test]
        public void Does_convert_from_EnumConversionInt()
        {
            var conversion = new EnumConversionInt
            {
                Color = 1,
                NullableColor = 1,
            };
            var conversionDto = conversion.ConvertTo<EnumConversion>();

            Assert.That(conversionDto.Color, Is.EqualTo(Color.Green));
            Assert.That(conversionDto.NullableColor, Is.EqualTo(Color.Green));
        }

        [Test]
        public void Does_convert_to_EnumConversionString()
        {
            var conversion = new EnumConversion
            {
                Color = Color.Green,
                NullableColor = Color.Green,
            };
            var conversionDto = conversion.ConvertTo<EnumConversionString>();

            Assert.That(conversionDto.Color, Is.EqualTo("Green"));
            Assert.That(conversionDto.NullableColor, Is.EqualTo("Green"));
        }

        [Test]
        public void Does_convert_from_EnumConversionString()
        {
            var conversion = new EnumConversionString
            {
                Color = "Green",
                NullableColor = "Green",
            };
            var conversionDto = conversion.ConvertTo<EnumConversion>();

            Assert.That(conversionDto.Color, Is.EqualTo(Color.Green));
            Assert.That(conversionDto.NullableColor, Is.EqualTo(Color.Green));
        }

        [Test]
        public void Does_nullableconversion_translate()
        {
            var conversion = new NullableConversion { Amount = 123.45m };
            var conversionDto = conversion.ConvertTo<NullableConversionDto>();

            Assert.That(conversionDto.Amount, Is.EqualTo(123.45m));
        }

        [Test]
        public void Does_Enumnullableconversion_translate()
        {
            var conversion = new NullableEnumConversion { Color = Color.Green };
            var conversionDto = conversion.ConvertTo<NullableEnumConversionDto>();

            Assert.That(conversionDto.Color, Is.EqualTo(OtherColor.Green));
        }

        [Test]
        public void Does_Enumconversion_translate()
        {
            var conversion = new NullableEnumConversion { Color = Color.Green };
            var conversionDto = conversion.ConvertTo<EnumConversionDto>();

            Assert.That(conversionDto.Color, Is.EqualTo(OtherColor.Green));
        }

        [Test]
        public void Does_ReallyEnumnullableconversion_translate()
        {
            var conversion = new ReallyNullableEnumConversion { Color = Color.Green };
            var conversionDto = conversion.ConvertTo<NullableEnumConversionDto>();

            Assert.That(conversionDto.Color, Is.EqualTo(OtherColor.Green));
        }

        [Test]
        public void Does_RealyEnumconversion_translate()
        {
            var conversion = new ReallyNullableEnumConversion { Color = Color.Green };
            var conversionDto = conversion.ConvertTo<EnumConversionDto>();

            Assert.That(conversionDto.Color, Is.EqualTo(OtherColor.Green));
        }

        [Test]
        public void Does_Enumconversion_translateFromNull()
        {
            var conversion = new ReallyNullableEnumConversion { Color = null };
            var conversionDto = conversion.ConvertTo<EnumConversionDto>();

            Assert.That(conversionDto.Color, Is.EqualTo(default(OtherColor)));
        }

        [Test]
        public void Does_translate_nullableInt_to_and_from()
        {
            var nullable = new IntNullableId();

            var nonNullable = nullable.ConvertTo<IntId>();

            nonNullable.Id = 10;

            var expectedNullable = nonNullable.ConvertTo<IntNullableId>();

            Assert.That(expectedNullable.Id.Value, Is.EqualTo(nonNullable.Id));
        }

        [Test]
        public void Does_translate_from_properties_to_fields()
        {
            var user = new User
            {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = user.ConvertTo<UserFields>();
            Assert.That(to.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.Car.Name, Is.EqualTo(user.Car.Name));
            Assert.That(to.Car.Age, Is.EqualTo(user.Car.Age));
        }

        [Test]
        public void Does_translate_from_fields_to_properties()
        {
            var user = new UserFields
            {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = user.ConvertTo<UserFields>();
            Assert.That(to.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.Car.Name, Is.EqualTo(user.Car.Name));
            Assert.That(to.Car.Age, Is.EqualTo(user.Car.Age));
        }

        [Test]
        public void Does_translate_from_inherited_propeties()
        {
            var user = new SubUser
            {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = user.ConvertTo<UserFields>();
            Assert.That(to.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.Car.Name, Is.EqualTo(user.Car.Name));
            Assert.That(to.Car.Age, Is.EqualTo(user.Car.Age));
        }

        [Test]
        public void Does_translate_to_inherited_propeties()
        {
            var user = new User
            {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = user.ConvertTo<SubUserFields>();
            Assert.That(to.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.Car.Name, Is.EqualTo(user.Car.Name));
            Assert.That(to.Car.Age, Is.EqualTo(user.Car.Age));
        }

        [Test]
        public void Can_convert_BclTypes()
        {
            Assert.That("from".ConvertTo<string>(), Is.EqualTo("from"));
            Assert.That(1.ConvertTo<long>(), Is.EqualTo(1L));
            Assert.That(2L.ConvertTo<int>(), Is.EqualTo(2));
            Assert.That(3.3d.ConvertTo<float>(), Is.EqualTo(3.3f));
            Assert.That(4.4d.ConvertTo<decimal>(), Is.EqualTo(4.4m));
        }

        [Test]
        public void Does_coerce_from_BclTypes_to_strings()
        {
            var from = new BclTypes
            {
                Int = 1,
                Long = 2,
                Double = 3.3,
                Decimal = 4.4m,
            };

            var to = from.ConvertTo<BclTypeStrings>();
            Assert.That(to.Int, Is.EqualTo("1"));
            Assert.That(to.Long, Is.EqualTo("2"));
            Assert.That(to.Double, Is.EqualTo("3.3"));
            Assert.That(to.Decimal, Is.EqualTo("4.4"));
        }

        [Test]
        public void Does_coerce_from_strings_to_BclTypes()
        {
            var from = new BclTypeStrings
            {
                Int = "1",
                Long = "2",
                Double = "3.3",
                Decimal = "4.4",
            };

            var to = from.ConvertTo<BclTypes>();
            Assert.That(to.Int, Is.EqualTo(1));
            Assert.That(to.Long, Is.EqualTo(2));
            Assert.That(to.Double, Is.EqualTo(3.3d));
            Assert.That(to.Decimal, Is.EqualTo(4.4m));
        }

        [Test]
        public void Does_map_only_properties_with_specified_Attribute()
        {
            var user = new User
            {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = new User();
            to.PopulateFromPropertiesWithAttribute(user, typeof(DataMemberAttribute));

            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.FirstName, Is.Null);
            Assert.That(to.Car, Is.Null);
        }

        [Test]
        public void Does_convert_ModelWithAllTypes()
        {
            var to = ModelWithAllTypes.Create(1);
            var from = to.ConvertTo<ModelWithAllTypes>();

            Assert.That(to.Equals(from));
        }

        public bool MatchesUsers(IEnumerable<User> u1s, IEnumerable<User> u2s)
        {
            if (u1s == null || u2s == null)
                return false;

            var u1sList = u1s.ToList();
            var u2sList = u2s.ToList();

            if (u1sList.Count != u2sList.Count)
                return false;

            for (var i = 0; i < u1sList.Count; i++)
            {
                var u1 = u1sList[i];
                var u2 = u2sList[i];

                if (u1.FirstName != u2.FirstName)
                    return false;
                if (u1.LastName != u2.LastName)
                    return false;
                if (u1.Car.Name != u2.Car.Name)
                    return false;
                if (u1.Car.Age != u2.Car.Age)
                    return false;
            }

            return true;
        }

        [Test]
        public void Does_convert_models_with_collections()
        {
            var from = new ModelWithEnumerable
            {
                Collection = new[] {
                    new User { FirstName = "First1", LastName = "Last1", Car = new Car { Name = "Car1", Age = 1} },
                    new User { FirstName = "First2", LastName = "Last2", Car = new Car { Name = "Car2", Age = 2} },
                }
            };

            Assert.That(MatchesUsers(from.Collection, from.ConvertTo<ModelWithEnumerable>().Collection));
            Assert.That(MatchesUsers(from.Collection, from.ConvertTo<ModelWithList>().Collection));
            Assert.That(MatchesUsers(from.Collection, from.ConvertTo<ModelWithArray>().Collection));
            Assert.That(MatchesUsers(from.Collection, from.ConvertTo<ModelWithHashSet>().Collection));

            Assert.That(MatchesUsers(from.Collection, from.Collection.ConvertTo<IEnumerable<User>>()));
            Assert.That(MatchesUsers(from.Collection, from.Collection.ConvertTo<List<User>>()));
            Assert.That(MatchesUsers(from.Collection, from.Collection.ConvertTo<User[]>()));
            Assert.That(MatchesUsers(from.Collection, from.Collection.ConvertTo<HashSet<User>>()));

            var array = from.Collection.ToArray();
            Assert.That(MatchesUsers(array, from.Collection.ConvertTo<IEnumerable<User>>()));
            Assert.That(MatchesUsers(array, from.Collection.ConvertTo<List<User>>()));
            Assert.That(MatchesUsers(array, from.Collection.ConvertTo<User[]>()));
            Assert.That(MatchesUsers(array, from.Collection.ConvertTo<HashSet<User>>()));

            var hashset = from.Collection.ToSet();
            Assert.That(MatchesUsers(hashset, from.Collection.ConvertTo<IEnumerable<User>>()));
            Assert.That(MatchesUsers(hashset, from.Collection.ConvertTo<List<User>>()));
            Assert.That(MatchesUsers(hashset, from.Collection.ConvertTo<User[]>()));
            Assert.That(MatchesUsers(hashset, from.Collection.ConvertTo<HashSet<User>>()));
        }

        public class Customer
        {
            public CompanyInfo CompanyInfo { get; set; }
            // other properties
        }

        public class CustomerDto
        {
            public CompanyInfoDto CompanyInfo { get; set; }
        }

        public class CompanyInfo
        {
            public int Id { get; set; }
            public string ITN { get; set; }
        }

        public class CompanyInfoDto
        {
            public int Id { get; set; }
            public string ITN { get; set; }
        }

        [Test]
        public void Does_retain_null_properties()
        {
            var user = new User { FirstName = "Foo" };
            var userDto = user.ConvertTo<UserFields>();

            Assert.That(userDto.FirstName, Is.EqualTo("Foo"));
            Assert.That(userDto.LastName, Is.Null);
            Assert.That(userDto.Car, Is.Null);

            var customer = new Customer { CompanyInfo = null };
            var dto = customer.ConvertTo<CustomerDto>();

            Assert.That(dto.CompanyInfo, Is.Null);
        }

        [Test]
        public void Does_ignore_properties_without_attributes()
        {
            var model = new ModelWithIgnoredFields
            {
                Id = 1,
                Name = "Foo",
                Ignored = 2
            };

            var dto = new ModelWithIgnoredFields { Ignored = 10 }
                .PopulateFromPropertiesWithoutAttribute(model, typeof(ReadOnlyAttribute));

            Assert.That(dto.Id, Is.EqualTo(model.Id));
            Assert.That(dto.Name, Is.EqualTo(model.Name));
            Assert.That(dto.Ignored, Is.EqualTo(10));
        }

#if !NETCORE
        public class IgnoredModel
        {
            public int Id { get; set; }

            [IgnoreDataMember]
            public int DataMemberIgnoreId { get; set; }

            [JsonIgnore]
            public int JsonIgnoreId { get; set; }

            [ScriptIgnore]
            public int ScriptIgnoreId { get; set; }
        }

        //Matches JSON.NET's [JsonIgnore] by name
        public class JsonIgnoreAttribute : AttributeBase { }

        [Test]
        public void Can_change_ignored_properties()
        {
            JsConfig.IgnoreAttributesNamed = JsConfig.IgnoreAttributesNamed.NewArray(
                with: typeof(ScriptIgnoreAttribute).Name,
                without: typeof(JsonIgnoreAttribute).Name);

            var dto = new IgnoredModel { JsonIgnoreId = 1, ScriptIgnoreId = 2 };

            Assert.That(dto.ToJson(), Is.EqualTo("{\"Id\":0,\"JsonIgnoreId\":1}"));
        }
#endif

        [Test]
        public void Does_convert_to_ValueType()
        {
            Assert.That("1".ConvertTo(typeof(int)), Is.EqualTo(1));
            Assert.That("1".ConvertTo(typeof(long)), Is.EqualTo(1L));
            Assert.That("1.1".ConvertTo(typeof(float)), Is.EqualTo(1.1f));
            Assert.That("1.1".ConvertTo(typeof(double)), Is.EqualTo(1.1d));
            Assert.That("1.1".ConvertTo(typeof(decimal)), Is.EqualTo(1.1M));

            Assert.That("2001-01-01".ConvertTo<DateTime>(), Is.EqualTo(new DateTime(2001, 01, 01)));
            Assert.That("98ece8400be4452eb6ad7c3a4404f119".ConvertTo<Guid>(), Is.EqualTo(new Guid("98ece8400be4452eb6ad7c3a4404f119")));
        }

        [Test]
        public void Does_convert_from_ValueType_to_strings()
        {
            Assert.That(1.ConvertTo(typeof(string)), Is.EqualTo("1"));
            Assert.That(1L.ConvertTo(typeof(string)), Is.EqualTo("1"));
            Assert.That(1.1f.ConvertTo(typeof(string)), Is.EqualTo("1.1"));
            Assert.That(1.1d.ConvertTo(typeof(string)), Is.EqualTo("1.1"));
            Assert.That(1.1M.ConvertTo(typeof(string)), Is.EqualTo("1.1"));
            
            Assert.That(new DateTime(2001, 01, 01).ConvertTo<string>(), Is.EqualTo("2001-01-01"));
            Assert.That(new Guid("98ECE840-0BE4-452E-B6AD-7C3A4404F119").ConvertTo<string>(), Is.EqualTo("98ece8400be4452eb6ad7c3a4404f119"));
        }

        [Test]
        public void Can_convert_from_List_object()
        {
            var from = 3.Times(i => (object)new Car { Age = i, Name = "Name" + i });
            var to = (List<Car>)TranslateListWithElements.TryTranslateCollections(
                typeof(List<object>), typeof(List<Car>), from);

            Assert.That(to.Count, Is.EqualTo(3));
            Assert.That(to[0].Age, Is.EqualTo(0));
            Assert.That(to[0].Name, Is.EqualTo("Name0"));
        }

        [Test]
        public void Can_convert_from_List_SubType()
        {
            var from = 3.Times(i => new SubCar { Age = i, Name = "Name" + i });
            var to = (List<Car>)TranslateListWithElements.TryTranslateCollections(
                typeof(List<SubCar>), typeof(List<Car>), from);

            Assert.That(to.Count, Is.EqualTo(3));
            Assert.That(to[0].Age, Is.EqualTo(0));
            Assert.That(to[0].Name, Is.EqualTo("Name0"));
        }

        [Test]
        public void Can_create_Dictionary_default_value()
        {
            var obj = (Dictionary<string, ClassWithEnum>)AutoMappingUtils.CreateDefaultValue(typeof(Dictionary<string, ClassWithEnum>), new Dictionary<Type, int>());
            Assert.That(obj, Is.Not.Null);
        }

        [Test]
        public void Can_get_generic_types_from_Dictionary_or_KVPs()
        {
            typeof(Dictionary<string, object>).GetKeyValuePairsTypes(out var keyType, out var valueType);
            Assert.That(keyType, Is.EqualTo(typeof(string)));
            Assert.That(valueType, Is.EqualTo(typeof(object)));
            
            typeof(IEnumerable<KeyValuePair<string, object>>).GetKeyValuePairsTypes(out keyType, out valueType);
            Assert.That(keyType, Is.EqualTo(typeof(string)));
            Assert.That(valueType, Is.EqualTo(typeof(object)));
            
            typeof(List<KeyValuePair<string, object>>).GetKeyValuePairsTypes(out keyType, out valueType);
            Assert.That(keyType, Is.EqualTo(typeof(string)));
            Assert.That(valueType, Is.EqualTo(typeof(object)));
        }

        [Test]
        public void Can_convert_between_different_types_of_Dictionaries_and_KVP_values()
        {
            var genericMap = new Dictionary<string, object> {
                { "a", 1 }
            };
            var stringMap = new Dictionary<string, string> {
                {"a", "1"}
            };
            var intMap = new Dictionary<string, int> {
                {"a", 1}
            };
            var kvps = new List<KeyValuePair<string, object>> {
                new KeyValuePair<string, object>("a", 1)
            };
            var stringKvps = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("a", "1")
            };
            var objDict = new ObjectDictionary {
                { "a", 1 }
            };
            
            var genericMapStringValue = new Dictionary<string, object> {
                { "a", "1" }
            };
            var objDictStringValue = new ObjectDictionary {
                { "a", "1" }
            };
            var kvpsStringValue = new List<KeyValuePair<string, object>> {
                new KeyValuePair<string, object>("a", "1")
            };
            
            Assert.That(genericMap.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(genericMap));
            Assert.That(genericMap.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(genericMap.ConvertTo<Dictionary<string,int>>(), Is.EquivalentTo(intMap));
            Assert.That(genericMap.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(kvps));
            Assert.That(genericMap.ConvertTo<List<KeyValuePair<string, string>>>(), Is.EquivalentTo(stringKvps));
            Assert.That(genericMap.ConvertTo<ObjectDictionary>(), Is.EquivalentTo(objDict));
            
            Assert.That(stringMap.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(genericMapStringValue));
            Assert.That(stringMap.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(stringMap.ConvertTo<Dictionary<string,int>>(), Is.EquivalentTo(intMap));
            Assert.That(stringMap.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(kvpsStringValue));
            Assert.That(stringMap.ConvertTo<List<KeyValuePair<string, string>>>(), Is.EquivalentTo(stringKvps));
            Assert.That(stringMap.ConvertTo<ObjectDictionary>(), Is.EquivalentTo(objDictStringValue));
            
            Assert.That(intMap.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(genericMap));
            Assert.That(intMap.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(intMap.ConvertTo<Dictionary<string,int>>(), Is.EquivalentTo(intMap));
            Assert.That(intMap.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(kvps));
            Assert.That(intMap.ConvertTo<List<KeyValuePair<string, string>>>(), Is.EquivalentTo(stringKvps));
            Assert.That(intMap.ConvertTo<ObjectDictionary>(), Is.EquivalentTo(objDict));
            
            Assert.That(kvps.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(genericMap));
            Assert.That(kvps.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(kvps.ConvertTo<Dictionary<string,int>>(), Is.EquivalentTo(intMap));
            Assert.That(kvps.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(kvps));
            Assert.That(kvps.ConvertTo<List<KeyValuePair<string, string>>>(), Is.EquivalentTo(stringKvps));
            Assert.That(kvps.ConvertTo<ObjectDictionary>(), Is.EquivalentTo(objDict));
            
            Assert.That(stringKvps.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(genericMapStringValue));
            Assert.That(stringKvps.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(stringKvps.ConvertTo<Dictionary<string,int>>(), Is.EquivalentTo(intMap));
            Assert.That(stringKvps.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(kvpsStringValue));
            Assert.That(stringKvps.ConvertTo<List<KeyValuePair<string, string>>>(), Is.EquivalentTo(stringKvps));
            Assert.That(stringKvps.ConvertTo<ObjectDictionary>(), Is.EquivalentTo(objDictStringValue));
        }

        [Test]
        public void Can_convert_between_different_types_of_Dictionaries_and_KVP_keys()
        {
            var genericMap = new Dictionary<object, string> {
                { 1, "a" }
            };
            var stringMap = new Dictionary<string, string> {
                {"1","a"}
            };
            var intMap = new Dictionary<int, string> {
                {1,"a"}
            };
            var kvps = new List<KeyValuePair<object, string>> {
                new KeyValuePair<object,string>(1, "a")
            };
            var stringKvps = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("1","a")
            };
            
            var genericMapStringValue = new Dictionary<object,string> {
                { "1","a" }
            };
            var kvpsStringValue = new List<KeyValuePair<object,string>> {
                new KeyValuePair<object,string>("1","a")
            };
            
            Assert.That(genericMap.ConvertTo<Dictionary<object,string>>(), Is.EquivalentTo(genericMap));
            Assert.That(genericMap.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(genericMap.ConvertTo<Dictionary<int,string>>(), Is.EquivalentTo(intMap));
            Assert.That(genericMap.ConvertTo<List<KeyValuePair<object,string>>>(), Is.EquivalentTo(kvps));
            Assert.That(genericMap.ConvertTo<List<KeyValuePair<string,string>>>(), Is.EquivalentTo(stringKvps));
            
            Assert.That(stringMap.ConvertTo<Dictionary<object,string>>(), Is.EquivalentTo(genericMapStringValue));
            Assert.That(stringMap.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(stringMap.ConvertTo<Dictionary<int,string>>(), Is.EquivalentTo(intMap));
            Assert.That(stringMap.ConvertTo<List<KeyValuePair<object,string>>>(), Is.EquivalentTo(kvpsStringValue));
            Assert.That(stringMap.ConvertTo<List<KeyValuePair<string, string>>>(), Is.EquivalentTo(stringKvps));
            
            Assert.That(intMap.ConvertTo<Dictionary<object,string>>(), Is.EquivalentTo(genericMap));
            Assert.That(intMap.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(intMap.ConvertTo<Dictionary<int,string>>(), Is.EquivalentTo(intMap));
            Assert.That(intMap.ConvertTo<List<KeyValuePair<object,string>>>(), Is.EquivalentTo(kvps));
            Assert.That(intMap.ConvertTo<List<KeyValuePair<string, string>>>(), Is.EquivalentTo(stringKvps));
            
            Assert.That(kvps.ConvertTo<Dictionary<object,string>>(), Is.EquivalentTo(genericMap));
            Assert.That(kvps.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(kvps.ConvertTo<Dictionary<int,string>>(), Is.EquivalentTo(intMap));
            Assert.That(kvps.ConvertTo<List<KeyValuePair<object,string>>>(), Is.EquivalentTo(kvps));
            Assert.That(kvps.ConvertTo<List<KeyValuePair<string,string>>>(), Is.EquivalentTo(stringKvps));
            
            Assert.That(stringKvps.ConvertTo<Dictionary<object,string>>(), Is.EquivalentTo(genericMapStringValue));
            Assert.That(stringKvps.ConvertTo<Dictionary<string,string>>(), Is.EquivalentTo(stringMap));
            Assert.That(stringKvps.ConvertTo<Dictionary<int,string>>(), Is.EquivalentTo(intMap));
            Assert.That(stringKvps.ConvertTo<List<KeyValuePair<object,string>>>(), Is.EquivalentTo(kvpsStringValue));
            Assert.That(stringKvps.ConvertTo<List<KeyValuePair<string,string>>>(), Is.EquivalentTo(stringKvps));
        }

        class HasObject
        {
            public object Value { get; set; }
        }

        [Test]
        public void Can_convert_dictionary_to_HasObject()
        {
            var objDict = new ObjectDictionary {
                ["value"] = new ObjectDictionary {
                    ["a"] = 1
                }
            };

            var to = objDict.FromObjectDictionary<HasObject>();
            Assert.That(to.Value, Is.EqualTo(objDict["value"]));
            
            var kvps = new ObjectDictionary {
                ["value"] = new List<KeyValuePair<string,int>> {
                    new KeyValuePair<string, int>("a",1)
                }
            };
            
            to = objDict.FromObjectDictionary<HasObject>();
            Assert.That(to.Value, Is.EqualTo(objDict["value"]));
        }
        
    }

    public enum ClassWithEnumType
    {
        One = 1,
        Two = 2,
        Three = 3
    }

    public class ClassWithEnum
    {
        public ClassWithEnumType Type { get; set; }
    }    

    public class Test
    {
        public string Name { get; set; }
    }

    public class PropertyExpressionTests
    {
        [Test]
        public void Can_call_typed_setter_Expressions()
        {
            var nameProperty = typeof(Test).GetProperty("Name");
            var setMethod = nameProperty.GetSetMethod();

            var instance = Expression.Parameter(typeof(Test), "i");
            var argument = Expression.Parameter(typeof(string), "a");

            var setterCall = Expression.Call(instance, setMethod, argument);
            var fn = Expression.Lambda<Action<Test, string>>(setterCall, instance, argument).Compile();

            var test = new Test();
            fn(test, "Foo");
            Assert.That(test.Name, Is.EqualTo("Foo"));
        }

        [Test]
        public void Can_call_object_setter_Expressions()
        {
            var nameProperty = typeof(Test).GetProperty("Name");

            var instance = Expression.Parameter(typeof(object), "i");
            var argument = Expression.Parameter(typeof(object), "a");

            var instanceParam = Expression.Convert(instance, nameProperty.ReflectedType);
            var valueParam = Expression.Convert(argument, nameProperty.PropertyType);

            var setterCall = Expression.Call(instanceParam, nameProperty.GetSetMethod(nonPublic:true), valueParam);

            var fn = Expression.Lambda<Action<object, object>>(setterCall, instance, argument).Compile();

            var test = new Test();
            fn(test, "Foo");
            Assert.That(test.Name, Is.EqualTo("Foo"));
        }

        public class RawRequest : IRequiresRequestStream
        {
            public Stream RequestStream { get; set; }
        }

        [Test]
        public void Can_create_DTO_with_Stream()
        {
            var o = typeof(RawRequest).CreateInstance();
            var requestObj = AutoMappingUtils.PopulateWith(o);

            Assert.That(requestObj, Is.Not.Null);
        }

        public class ModelWithUri
        {
            public Uri Uri { get; set; }
        }
        public class ModelWithUriString
        {
            public string Uri { get; set; }
        }

        [Test]
        public void Does_map_Uri()
        {
            var dto = new ModelWithUri { Uri = new Uri("http://a.com") };

            var to = new ModelWithUri().PopulateWithNonDefaultValues(dto);
            Assert.That(to.Uri, Is.EqualTo(dto.Uri));

            var toString = new ModelWithUriString().PopulateWithNonDefaultValues(dto);
            Assert.That(toString.Uri, Is.EqualTo(dto.Uri.AbsoluteUri));
        }

        public class ViewModel
        {
            public string Public { get; set; }
            
            [IgnoreOnSelect]
            public string Private { get; set; }
        }
        
        [Test]
        public void Does_convert_same_model_without_Attributes()
        {
            var vm = new ViewModel().PopulateFromPropertiesWithoutAttribute(
                new ViewModel { Public = "Public", Private = "Private" },
                typeof(IgnoreOnSelectAttribute));
            
            Assert.That(vm.Public, Is.EqualTo("Public"));
            Assert.That(vm.Private, Is.Null);
        }

        [Test]
        public void Can_use_ToObjectDictionary_to_Remove_Property_with_Attribute()
        {
            var vm = new ViewModel {Public = "Public", Private = "Private"};
            var map = vm.ToObjectDictionary();
            vm.GetType().GetProperties()
                .Where(x => x.HasAttribute<IgnoreOnSelectAttribute>())
                .Each(x => map.Remove(x.Name));

            vm = map.FromObjectDictionary<ViewModel>();
            Assert.That(vm.Public, Is.EqualTo("Public"));
            Assert.That(vm.Private, Is.Null);
        }

        class DictionaryTest
        {
            public string A { get; set; }
            public int B { get; set; }
            public bool C { get; set; }
            public double D { get; set; }
        }

        [Test]
        public void Can_convert_anonymous_object_to_ObjectDictionary()
        {
            var newObj = new { A = "a", B = 1, C = true, D = 2.0 };
            var type = new DictionaryTest { A = "a", B = 1, C = true, D = 2.0 };
            var expected = new Dictionary<string, object> {
                ["A"] = "a",
                ["B"] = 1,
                ["C"] = true,
                ["D"] = 2.0
            };

            var to = newObj.ToObjectDictionary();
            Assert.That(to, Is.EquivalentTo(expected));
            to = type.ToObjectDictionary();
            Assert.That(to, Is.EquivalentTo(expected));
            to = expected.ToObjectDictionary();
            Assert.That(to, Is.EquivalentTo(expected));
        }

        [Test]
        public void Can_convert_string_to_collection()
        {
            Assert.That("a,b,c".ConvertTo<string[]>(), Is.EquivalentTo(new[]{ "a", "b", "c" }));
            Assert.That("1,2,3".ConvertTo<int[]>(), Is.EquivalentTo(new[]{ 1, 2, 3 }));
        }
        
        [Test]
        public void Translate_Between_Models_of_different_types_and_nullables()
        {
            var fromObj = ModelWithFieldsOfDifferentTypes.CreateConstant(1);

            var toObj = fromObj.ConvertTo<ModelWithFieldsOfDifferentTypesAsNullables>();

            Console.WriteLine(toObj.Dump());

            ModelWithFieldsOfDifferentTypesAsNullables.AssertIsEqual(fromObj, toObj);
        }

        [Test]
        public void Translate_Between_Models_of_nullables_and_different_types()
        {
            var fromObj = ModelWithFieldsOfDifferentTypesAsNullables.CreateConstant(1);

            var toObj = fromObj.ConvertTo<ModelWithFieldsOfDifferentTypes>();

            Console.WriteLine(toObj.Dump());

            ModelWithFieldsOfDifferentTypesAsNullables.AssertIsEqual(toObj, fromObj);
        }

        [Test]
        public void Can_convert_to_array_to_string()
        {
            Assert.That(new []{ new ViewModel { Public = "A"} }.ConvertTo<string>(), Is.Not.Empty);
            Assert.That(new []{ DayOfWeek.Monday }.ConvertTo<string>(), Is.Not.Empty);
        }
        
        [Test]
        public void To_works_with_ValueTypes()
        {
            Assert.That(1.ToString().ConvertTo<int>(), Is.EqualTo(1));
        }

        [Test]
        public void To_on_null_or_empty_string_returns_default_value_supplied()
        {
            const string nullString = null;
            Assert.That("".ConvertTo(1), Is.EqualTo(1));
            Assert.That("".ConvertTo(default(int)), Is.EqualTo(default(int)));
            Assert.That(nullString.ConvertTo(1), Is.EqualTo(1));
        }

        [Test]
        public void To_ValueType_on_null_or_empty_string_returns_default_value()
        {
            Assert.That("".ConvertTo<int>(), Is.EqualTo(default(int)));
        }

        struct A
        {
            public int Id { get; }
            public A(int id) => Id = id;
            public static implicit operator B(A from) => new B(from.Id);
        }

        struct B
        {
            public int Id { get; }
            public B(int id) => Id = id;
            public static implicit operator A(B from) => new A(from.Id);
        }

        [Test]
        public void Can_convert_between_structs_with_implicit_casts()
        {
            var a = new A(1);
            var b = a;
            Assert.That(b.Id, Is.EqualTo(a.Id));

            b = a.ConvertTo<B>();
            Assert.That(b.Id, Is.EqualTo(a.Id));
        }

        [Test]
        public void Can_convert_from_class_implicit_Casts()
        {
            var to = "test".ConvertTo<XName>();
            Assert.That(to.LocalName, Is.EqualTo("test"));
        }

        struct C
        {
            public int Id { get; }
            public C(int id) => Id = id;
            public static explicit operator C(D from) => new C(from.Id);
        }

        struct D
        {
            public int Id { get; }
            public D(int id) => Id = id;
            public static explicit operator D(C from) => new D(from.Id);
        }

        [Test]
        public void Can_convert_between_structs_with_explicit_casts()
        {
            var c = new C(1);
            var d = (D)c;
            Assert.That(d.Id, Is.EqualTo(c.Id));

            d = c.ConvertTo<D>();
            Assert.That(d.Id, Is.EqualTo(c.Id));
        }
        
        [Test]
        public void Can_Convert_from_ObjectDictionary_Containing_Another_Object_Dictionary()
        {
            var map = new Dictionary<string, object>
            {
                { "name", "Foo" },
                { "otherNames", new List<object>
                    {
                        new Dictionary<string, object> { { "name", "Fu" } },
                        new Dictionary<string, object> { { "name", "Fuey" } }
                    }
                }
            };

            var fromDict = map.ConvertTo<AutoMappingObjectDictionaryTests.PersonWithIdentities>();

            Assert.That(fromDict.Name, Is.EqualTo("Foo"));
            Assert.That(fromDict.OtherNames.Count, Is.EqualTo(2));
            Assert.That(fromDict.OtherNames.First().Name, Is.EqualTo("Fu"));
            Assert.That(fromDict.OtherNames.Last().Name, Is.EqualTo("Fuey"));

            var toDict = fromDict.ConvertTo<Dictionary<string,object>>();
            Assert.That(toDict["Name"], Is.EqualTo("Foo"));
            Assert.That(toDict["OtherNames"], Is.EqualTo(fromDict.OtherNames));

            var toObjDict = fromDict.ConvertTo<ObjectDictionary>();
            Assert.That(toObjDict["Name"], Is.EqualTo("Foo"));
            Assert.That(toObjDict["OtherNames"], Is.EqualTo(fromDict.OtherNames));
        }

        class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }

            protected bool Equals(Person other) => Id == other.Id && string.Equals(Name, other.Name);

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == this.GetType() && Equals((Person) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                }
            }
        }

        [Test]
        public void Can_convert_between_Poco_and_ObjectDictionary()
        {
            var person = new Person { Id = 1, Name = "foo" };

            Assert.That(person.ConvertTo<Dictionary<string, object>>(), Is.EqualTo(new Dictionary<string, object> {
                {"Id", 1},
                {"Name", "foo"},
            })); 
            Assert.That(new Dictionary<string, object> {
                {"Id", 1},
                {"Name", "foo"},
            }.ConvertTo<Person>(), Is.EqualTo(person));

            Assert.That(person.ConvertTo<ObjectDictionary>(), Is.EqualTo(new ObjectDictionary {
                {"Id", 1},
                {"Name", "foo"},
            })); 
            Assert.That(new ObjectDictionary {
                {"Id", 1},
                {"Name", "foo"},
            }.ConvertTo<Person>(), Is.EqualTo(person));

            Assert.That(person.ConvertTo<Dictionary<string, string>>(), Is.EqualTo(new Dictionary<string, string> {
                {"Id", "1"},
                {"Name", "foo"},
            })); 

            Assert.That(new Dictionary<string, string> {
                {"Id", "1"},
                {"Name", "foo"},
            }.ConvertTo<Person>(), Is.EqualTo(person)); 

            Assert.That(person.ConvertTo<StringDictionary>(), Is.EqualTo(new StringDictionary {
                {"Id", "1"},
                {"Name", "foo"},
            })); 
            Assert.That(new StringDictionary {
                {"Id", "1"},
                {"Name", "foo"},
            }.ConvertTo<Person>(), Is.EqualTo(person)); 
        }

        [Test]
        public void Can_ToObjectDictionary_KVPs()
        {
            var objKvp = new KeyValuePair<string, object>("A", 1);
            var strKvp = new KeyValuePair<string, string>("A", "1");
            var intKvp = new KeyValuePair<string, int>("A", 1);
            
            Assert.That(objKvp.ToObjectDictionary(), Is.EquivalentTo(new Dictionary<string, object> { {"Key", "A" }, { "Value", 1  } } ));
            Assert.That(strKvp.ToObjectDictionary(), Is.EquivalentTo(new Dictionary<string, object> { {"Key", "A" }, { "Value", "1"} } ));
            Assert.That(intKvp.ToObjectDictionary(), Is.EquivalentTo(new Dictionary<string, object> { {"Key", "A" }, { "Value", 1  } } ));
            
            var objKvp2 = new KeyValuePair<string, object>("B", 2);
            var strKvp2 = new KeyValuePair<string, string>("B", "2");
            var intKvp2 = new KeyValuePair<string, int>("B", 2);

            Assert.That(new[] { objKvp, objKvp2 }.ToObjectDictionary(), Is.EquivalentTo(new Dictionary<string, object> { {"A", 1}, {"B", 2} }));
            Assert.That(new[] { strKvp, strKvp2 }.ToObjectDictionary(), Is.EquivalentTo(new Dictionary<string, object> { {"A", "1"}, {"B", "2"} }));
            Assert.That(new[] { intKvp, intKvp2 }.ToObjectDictionary(), Is.EquivalentTo(new Dictionary<string, object> { {"A", 1}, {"B", 2} }));
        }

        [Test]
        public void Can_convert_KVPs_to_ObjectDictionary()
        {
            var objKvp = new KeyValuePair<string, object>("A", 1);
            var strKvp = new KeyValuePair<string, string>("A", "1");
            var intKvp = new KeyValuePair<string, int>("A", 1);
            
            Assert.That(objKvp.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(new Dictionary<string, object> { {"Key", "A" }, { "Value", 1  } } ));
            Assert.That(strKvp.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(new Dictionary<string, object> { {"Key", "A" }, { "Value", "1"} } ));
            Assert.That(intKvp.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(new Dictionary<string, object> { {"Key", "A" }, { "Value", 1  } } ));
            
            var objKvp2 = new KeyValuePair<string, object>("B", 2);
            var strKvp2 = new KeyValuePair<string, string>("B", "2");
            var intKvp2 = new KeyValuePair<string, int>("B", 2);

            Assert.That(new[] { objKvp, objKvp2 }.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(new Dictionary<string, object> { {"A", 1}, {"B", 2} }));
            Assert.That(new[] { strKvp, strKvp2 }.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(new Dictionary<string, object> { {"A", "1"}, {"B", "2"} }));
            Assert.That(new[] { intKvp, intKvp2 }.ConvertTo<Dictionary<string,object>>(), Is.EquivalentTo(new Dictionary<string, object> { {"A", 1}, {"B", 2} }));
        }

        [Test]
        public void Can_convert_between_KVP_Objects_and_IEnumerable()
        {
            var kvps = new List<KeyValuePair<string, object>> {
                new KeyValuePair<string, object>("A", 1),
                new KeyValuePair<string, object>("B", 2),
                new KeyValuePair<string, object>("C", 3),
            };
            
            Assert.That(kvps.ConvertTo<IEnumerable<object>>(), Is.EqualTo(kvps));
            Assert.That(kvps.ConvertTo<List<object>>(), Is.EqualTo(kvps));
            Assert.That(kvps.ConvertTo<object[]>(), Is.EqualTo(kvps));

            List<object> listObjs = kvps.Cast<object>().ToList();
            Assert.That(listObjs.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(listObjs));
            object[] arrayObjs = kvps.Cast<object>().ToArray();
            Assert.That(arrayObjs.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(arrayObjs));
        }

        [Test]
        public void Can_convert_between_KVP_Strings_and_IEnumerable()
        {
            var kvps = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("A", "1"),
                new KeyValuePair<string, string>("B", "2"),
                new KeyValuePair<string, string>("C", "3"),
            };
            
            Assert.That(kvps.ConvertTo<IEnumerable<object>>(), Is.EqualTo(kvps));
            Assert.That(kvps.ConvertTo<List<object>>(), Is.EqualTo(kvps));
            Assert.That(kvps.ConvertTo<object[]>(), Is.EqualTo(kvps));

            List<object> listObjs = kvps.Cast<object>().ToList();
            Assert.That(listObjs.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(listObjs));
            object[] arrayObjs = kvps.Cast<object>().ToArray();
            Assert.That(arrayObjs.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(arrayObjs));
        }

        [Test]
        public void Can_convert_between_KVP_ints_and_IEnumerable()
        {
            var kvps = new List<KeyValuePair<string, int>> {
                new KeyValuePair<string, int>("A", 1),
                new KeyValuePair<string, int>("B", 2),
                new KeyValuePair<string, int>("C", 3),
            };
            
            Assert.That(kvps.ConvertTo<IEnumerable<object>>(), Is.EqualTo(kvps));
            Assert.That(kvps.ConvertTo<List<object>>(), Is.EqualTo(kvps));
            Assert.That(kvps.ConvertTo<object[]>(), Is.EqualTo(kvps));

            List<object> listObjs = kvps.Cast<object>().ToList();
            Assert.That(listObjs.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(listObjs));
            object[] arrayObjs = kvps.Cast<object>().ToArray();
            Assert.That(arrayObjs.ConvertTo<List<KeyValuePair<string, object>>>(), Is.EquivalentTo(arrayObjs));
        }

        public enum TestEnum { A, B, C }

        [Test]
        public void Can_convert_between_list_of_value_types()
        {
            var enumArrays = new[]{ TestEnum.A, TestEnum.B, TestEnum.C };
            var strArrays = new List<string> {"A","B","C"};
            Assert.That(enumArrays.ConvertTo<List<TestEnum>>(), Is.EquivalentTo(enumArrays.ToList()));
            Assert.That(enumArrays.ConvertTo<List<string>>(), Is.EquivalentTo(strArrays));

            var strNums = new List<string> {"1","2","3"};
            var intNums = new List<int> {1,2,3};
            Assert.That(strNums.ConvertTo<List<int>>(), Is.EquivalentTo(intNums));
            Assert.That(intNums.ConvertTo<List<string>>(), Is.EquivalentTo(strNums));
        }
        
        public class TestClassA
        {
            public IList<string> IListStrings { get; set; }
            public ArrayOfString ListStrings { get; set; }
            public IList<TestEnum> ListEnums { get; set; }
        }
        public class TestClassB
        {
            public ArrayOfString IListStrings { get; set; }
            public IList<string> ListStrings { get; set; }
            public ArrayOfString ListEnums { get; set; }
        }
        public class TestClassC
        {
            public IList<string> ListStrings { get; protected set; }
        }

        [Test]
        public void Can_translate_generic_lists()
        {
            
            var values = new[] { "A", "B", "C" };
            var testA = new TestClassA
            {
                IListStrings = new List<string>(values),
                ListStrings = new ArrayOfString(values),
                ListEnums = new List<TestEnum> { TestEnum.A, TestEnum.B, TestEnum.C },
            };

            var fromTestA = testA.ConvertTo<TestClassB>();

            AssertAreEqual(testA, fromTestA);

            var userFileTypeValues = testA.ListEnums.Map(x => x.ToString());
            var testB = new TestClassB
            {
                IListStrings = new ArrayOfString(values),
                ListStrings = new List<string>(values),
                ListEnums = new ArrayOfString(userFileTypeValues),
            };

            var fromTestB = testB.ConvertTo<TestClassA>();
            AssertAreEqual(fromTestB, testB);
        }
        
        private static void AssertAreEqual(TestClassA testA, TestClassB testB)
        {
            Assert.That(testA, Is.Not.Null);
            Assert.That(testB, Is.Not.Null);

            Assert.That(testA.IListStrings, Is.Not.Null);
            Assert.That(testB.IListStrings, Is.Not.Null);
            Assert.That(testA.IListStrings,
                Is.EquivalentTo(new List<string>(testB.IListStrings)));

            Assert.That(testA.ListStrings, Is.Not.Null);
            Assert.That(testB.ListStrings, Is.Not.Null);
            Assert.That(testA.ListStrings, Is.EquivalentTo(testB.ListStrings));

            Assert.That(testA.ListEnums, Is.Not.Null);
            Assert.That(testB.ListEnums, Is.Not.Null);
            Assert.That(testA.ListEnums,
                Is.EquivalentTo(testB.ListEnums.ConvertAll(x => x.ToEnum<TestEnum>())));
        }

        class Company
        {
            public string Name { get; set; }
            public Address Address { get; set; }
        }

        class Address
        {
            public string City { get; set; }
            public string PostCode { get; set; }
        }

        [Test]
        public void Can_register_converters_to_PopulateWithNonDefaultValues()
        {
            Company Create()
            {
                return new Company {
                    Name = "Compnay1",
                    Address = new Address {
                        City = "NY",
                        PostCode = "1234",
                    }
                };
            }

            var defaults = Create();
            var original = Create();

            original.PopulateWithNonDefaultValues(new Company {
                Address = new Address {
                    PostCode = "4321"
                }
            });
            
            Assert.That(original.Name, Is.EqualTo(defaults.Name));
            Assert.That(original.Address.City, Is.Null);
            Assert.That(original.Address.PostCode, Is.EqualTo("4321"));
            
            original = Create();

            var updated = new Company {
                Address = new Address {
                    PostCode = "4321"
                }
            };
            original.Address.PopulateWithNonDefaultValues(updated.Address);
            updated.Address = null;
            original.PopulateWithNonDefaultValues(updated);
            
            Assert.That(original.Name, Is.EqualTo(defaults.Name));
            Assert.That(original.Address.City, Is.EqualTo(defaults.Address.City));
            Assert.That(original.Address.PostCode, Is.EqualTo("4321"));
        }
        
        public class NavItem : IMeta
        {
            public string Label { get; set; }
            public string Path { get; set; }
        
            /// <summary>
            /// Whether Active class should only be added when paths are exact match
            /// otherwise checks if ActivePath starts with Path
            /// </summary>
            public bool Exact { get; set; }

            public string Id { get; set; }    // Emit id="{Id}"
            public string Class { get; set; } // Override class="{Class}"
        
            public List<NavItem> Children { get; set; } = new List<NavItem>();
        
            public Dictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        }

        [Test]
        public void Does_convert_nested_collections()
        {
            var to = new[] {
                new Dictionary<string,object> {
                    ["id"] = "A1",
                    ["children"] = new List<object>
                    {
                        new Dictionary<string,object>
                        {
                            ["id"] = "B1",
                        },
                        new Dictionary<string,object>
                        {
                            ["id"] = "B2",
                            ["children"] = new List<object> {
                                new Dictionary<string,object>
                                {
                                    ["id"] = "C1",
                                },
                            }
                        },
                    }
                }
            };

            var navItems = to.ConvertTo<List<NavItem>>();
            Assert.That(navItems[0].Id, Is.EqualTo("A1"));
            Assert.That(navItems[0].Children[0].Id, Is.EqualTo("B1"));
            Assert.That(navItems[0].Children[1].Id, Is.EqualTo("B2"));
            Assert.That(navItems[0].Children[1].Children[0].Id, Is.EqualTo("C1"));
        }

        public class TestMethod
        {
            public void MyMethod()
            {
                MyProp = nameof(MyProp);
            }
            public string MyProp { get; set; }
        }
        
        public class TestMethod2
        {
            public void MyMethod(string myProp)
            {
                MyProp = nameof(MyMethod);
            }
            public string MyProp { get; set; }
        }
        
        [Test]
        public void Does_not_try_to_map_methods()
        {
            var from = new TestMethod();
            var to = new TestMethod2();
            from.MyProp = "Test";
            to.PopulateFromPropertiesWithoutAttribute(from, typeof(IgnoreDataMemberAttribute));
            Assert.That(from.MyProp, Is.EqualTo(to.MyProp));
        }

    }
}
