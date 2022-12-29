using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class AutoMappingObjectDictionaryTests
    {
        [Test]
        public void Can_convert_Car_to_ObjectDictionary_WithMapper()
        {
            var dto = new DtoWithEnum { Name = "Dan", Color = Color.Blue};
            var map = dto.ToObjectDictionary((k,v) => k == nameof(Color) ? v.ToString().ToLower() : v);

            Assert.That(map["Color"], Is.EqualTo(Color.Blue.ToString().ToLower()));
            Assert.That(map["Name"], Is.EqualTo(dto.Name));
        }

        [Test]
        public void Can_convert_Car_to_ObjectDictionary()
        {
            var dto = new Car { Age = 10, Name = "ZCar" };
            var map = dto.ToObjectDictionary();

            Assert.That(map["Age"], Is.EqualTo(dto.Age));
            Assert.That(map["Name"], Is.EqualTo(dto.Name));

            var fromDict = (Car)map.FromObjectDictionary(typeof(Car));
            Assert.That(fromDict.Age, Is.EqualTo(dto.Age));
            Assert.That(fromDict.Name, Is.EqualTo(dto.Name));
        }

        [Test]
        public void Can_convert_Cart_to_ObjectDictionary()
        {
            var dto = new User
            {
                FirstName = "First",
                LastName = "Last",
                Car = new Car { Age = 10, Name = "ZCar" },
            };

            var map = dto.ToObjectDictionary();

            Assert.That(map["FirstName"], Is.EqualTo(dto.FirstName));
            Assert.That(map["LastName"], Is.EqualTo(dto.LastName));
            Assert.That(((Car)map["Car"]).Age, Is.EqualTo(dto.Car.Age));
            Assert.That(((Car)map["Car"]).Name, Is.EqualTo(dto.Car.Name));

            var fromDict = map.FromObjectDictionary<User>();
            Assert.That(fromDict.FirstName, Is.EqualTo(dto.FirstName));
            Assert.That(fromDict.LastName, Is.EqualTo(dto.LastName));
            Assert.That(fromDict.Car.Age, Is.EqualTo(dto.Car.Age));
            Assert.That(fromDict.Car.Name, Is.EqualTo(dto.Car.Name));
        }

        [Test]
        public void Can_Convert_from_ObjectDictionary_with_Different_Types()
        {
            var map = new Dictionary<string, object>
            {
                { "FirstName", 1 },
                { "LastName", true },
                { "Car",  new SubCar { Age = 10, Name = "SubCar", Custom = "Custom"} },
            };

            var fromDict = (User)map.FromObjectDictionary(typeof(User));
            Assert.That(fromDict.FirstName, Is.EqualTo("1"));
            Assert.That(fromDict.LastName, Is.EqualTo(bool.TrueString));
            Assert.That(fromDict.Car.Age, Is.EqualTo(10));
            Assert.That(fromDict.Car.Name, Is.EqualTo("SubCar"));
        }

        [Test]
        public void Can_Convert_from_ObjectDictionary_with_Different_Types_with_camelCase_names()
        {
            var map = new Dictionary<string, object>
            {
                { "firstName", 1 },
                { "lastName", true },
                { "car",  new SubCar { Age = 10, Name = "SubCar", Custom = "Custom"} },
            };

            var fromDict = (User)map.FromObjectDictionary(typeof(User));
            Assert.That(fromDict.FirstName, Is.EqualTo("1"));
            Assert.That(fromDict.LastName, Is.EqualTo(bool.TrueString));
            Assert.That(fromDict.Car.Age, Is.EqualTo(10));
            Assert.That(fromDict.Car.Name, Is.EqualTo("SubCar"));
        }

        [Test]
        public void Can_Convert_from_ObjectDictionary_with_Read_Only_Dictionary()
        {
            var map = new Dictionary<string, object>
            {
                { "FirstName", 1 },
                { "LastName", true },
                { "Car",  new SubCar { Age = 10, Name = "SubCar", Custom = "Custom"} },
            };

            var readOnlyMap = new ReadOnlyDictionary<string, object>(map);

            var fromDict = (User)readOnlyMap.FromObjectDictionary(typeof(User));
            Assert.That(fromDict.FirstName, Is.EqualTo("1"));
            Assert.That(fromDict.LastName, Is.EqualTo(bool.TrueString));
            Assert.That(fromDict.Car.Age, Is.EqualTo(10));
            Assert.That(fromDict.Car.Name, Is.EqualTo("SubCar"));
        }

        public class QueryCustomers : QueryDb<Customer>
        {
            public string CustomerId { get; set; }
            public string[] CountryIn { get; set; }
            public string[] CityIn { get; set; }
        }

        [Test]
        public void Can_convert_from_ObjectDictionary_into_AutoQuery_DTO()
        {
            var map = new Dictionary<string, object>
            {
                { "CustomerId", "CustomerId"},
                { "CountryIn", new[]{"UK", "Germany"}},
                { "CityIn", "London,Berlin"},
                { "take", 5 },
                { "Meta", "{foo:bar}" },
            };

            var request = map.FromObjectDictionary<QueryCustomers>();

            Assert.That(request.CustomerId, Is.EqualTo("CustomerId"));
            Assert.That(request.CountryIn, Is.EquivalentTo(new[]{"UK", "Germany" }));
            Assert.That(request.CityIn, Is.EquivalentTo(new[]{ "London", "Berlin" }));
            Assert.That(request.Take, Is.EqualTo(5));
            Assert.That(request.Meta, Is.EquivalentTo(new Dictionary<string, object> {{"foo", "bar"}}));
        }
        
        public class PersonWithIdentities
        {
            public string Name { get; set; }
            public List<OtherName> OtherNames { get;set; }
        }

        public class OtherName
        {
            public string Name { get; set; }
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

            var fromDict = map.FromObjectDictionary<PersonWithIdentities>();

            Assert.That(fromDict.Name, Is.EqualTo("Foo"));
            Assert.That(fromDict.OtherNames.Count, Is.EqualTo(2));
            Assert.That(fromDict.OtherNames.First().Name, Is.EqualTo("Fu"));
            Assert.That(fromDict.OtherNames.Last().Name, Is.EqualTo("Fuey"));

            var toDict = fromDict.ToObjectDictionary();
            Assert.That(toDict["Name"], Is.EqualTo("Foo"));
            Assert.That(toDict["OtherNames"], Is.EqualTo(fromDict.OtherNames));
        }

        public class Employee
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string DisplayName { get; set; }
        }

        [Test]
        public void Can_create_new_object_using_MergeIntoObjectDictionary()
        {
            var customer = new User { FirstName = "John", LastName = "Doe" };
            var map = customer.MergeIntoObjectDictionary(new { Initial = "Z" });
            map["DisplayName"] = map["FirstName"] + " " + map["Initial"] + " " + map["LastName"];
            var employee = map.FromObjectDictionary<Employee>();
            
            Assert.That(employee.DisplayName, Is.EqualTo("John Z Doe"));
        }

        [Test]
        public void Can_create_new_object_from_merged_objects()
        {
            var customer = new User { FirstName = "John", LastName = "Doe" };
            var map = MergeObjects(customer, new { Initial = "Z" });
            map["DisplayName"] = map["FirstName"] + " " + map["Initial"] + " " + map["LastName"];
            var employee = map.FromObjectDictionary<Employee>();

            Dictionary<string,object> MergeObjects(params object[] sources) {
                var to = new Dictionary<string, object>(); 
                sources.Each(x => x.ToObjectDictionary().Each(entry => to[entry.Key] = entry.Value));
                return to;
            }
            
            Assert.That(employee.DisplayName, Is.EqualTo("John Z Doe"));
        }

        [Test, TestCaseSource(nameof(TestDataFromObjectDictionaryWithNullableTypes))]
        public void Can_Convert_from_ObjectDictionary_with_Nullable_Properties(
            Dictionary<string, object> map,
            ModelWithFieldsOfNullableTypes expected)
        {
            var actual = map.FromObjectDictionary<ModelWithFieldsOfNullableTypes>();

            ModelWithFieldsOfNullableTypes.AssertIsEqual(actual, expected);
        }

        private static IEnumerable<TestCaseData> TestDataFromObjectDictionaryWithNullableTypes
        {
            get
            {
                var defaults = ModelWithFieldsOfNullableTypes.CreateConstant(1);

                yield return new TestCaseData(
                    new Dictionary<string, object>
                    {
                        { "Id", defaults.Id },
                        { "NId", defaults.NId },
                        { "NLongId", defaults.NLongId },
                        { "NGuid", defaults.NGuid },
                        { "NBool", defaults.NBool },
                        { "NDateTime", defaults.NDateTime },
                        { "NFloat", defaults.NFloat },
                        { "NDouble", defaults.NDouble },
                        { "NDecimal", defaults.NDecimal },
                        { "NTimeSpan", defaults.NTimeSpan }
                    },
                    defaults).SetName("All values populated");

                yield return new TestCaseData(
                    new Dictionary<string, object>
                    {
                        { "Id", defaults.Id.ToString() },
                        { "NId", defaults.NId.ToString() },
                        { "NLongId", defaults.NLongId.ToString() },
                        { "NGuid", defaults.NGuid.ToString() },
                        { "NBool", defaults.NBool.ToString() },
                        { "NDateTime", defaults.NDateTime?.ToString("o") },
                        { "NFloat", defaults.NFloat.ToString() },
                        { "NDouble", defaults.NDouble.ToString() },
                        { "NDecimal", defaults.NDecimal.ToString() },
                        { "NTimeSpan", defaults.NTimeSpan.ToString() }
                    },
                    defaults).SetName("All values populated as strings");

                yield return new TestCaseData(
                    new Dictionary<string, object>
                    {
                        { "Id", defaults.Id },
                        { "NId", null },
                        { "NLongId", null },
                        { "NGuid", null },
                        { "NBool", null },
                        { "NDateTime", null },
                        { "NFloat", null },
                        { "NDouble", null },
                        { "NDecimal", null },
                        { "NTimeSpan", null }
                    },
                    new ModelWithFieldsOfNullableTypes
                    {
                        Id = defaults.Id
                    }).SetName("Nullables set to null");

                yield return new TestCaseData(
                    new Dictionary<string, object>
                    {
                        { "Id", defaults.Id }
                    },
                    new ModelWithFieldsOfNullableTypes
                    {
                        Id = defaults.Id
                    }).SetName("Nullables unassigned");

                yield return new TestCaseData(
                    new Dictionary<string, object>
                    {
                        { "Id", defaults.Id },
                        { "NLongId", 2 },
                        { "NFloat", "3.1" },
                        { "NDecimal", 4.2d },
                        { "NTimeSpan", null }
                    },
                    new ModelWithFieldsOfNullableTypes
                    {
                        Id = defaults.Id,
                        NLongId = 2,
                        NFloat = 3.1f,
                        NDecimal = 4.2m
                    }).SetName("Mixed properties");

                yield return new TestCaseData(
                    new Dictionary<string, object>
                    {
                        { "Id", defaults.Id },
                        { "NMadeUp", 99.9 },
                        { "NLongId", 2 },
                        { "NFloat", "3.1" },
                        { "NRandom", "RANDOM" },
                        { "NDecimal", 4.2d },
                        { "NTimeSpan", null },
                        { "NNull", null }
                    },
                    new ModelWithFieldsOfNullableTypes
                    {
                        Id = defaults.Id,
                        NLongId = 2,
                        NFloat = 3.1f,
                        NDecimal = 4.2m
                    }).SetName("Mixed properties with some foreign key/values");
            }
        }

        [Test]
        public void Can_Convert_from_ObjectDictionary_with_Nullable_Collection_Properties()
        {
            var map = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Users", new[] { new User { FirstName = "Foo", LastName = "Bar", Car = new Car { Name = "Jag", Age = 25 }}}},
                { "Cars", new List<Car> { new Car { Name = "Toyota", Age = 2 }, new Car { Name = "Lexus", Age = 1 }}},
                { "Colors", null }
            };

            var actual = map.FromObjectDictionary<ModelWithCollectionsOfNullableTypes>();

            Assert.That(actual.Id, Is.EqualTo(1));
            Assert.That(actual.Users, Is.Not.Null);
            Assert.That(actual.Users.Count(), Is.EqualTo(1));
            var user = actual.Users.Single();
            Assert.That(user.FirstName, Is.EqualTo("Foo"));
            Assert.That(user.LastName, Is.EqualTo("Bar"));
            Assert.That(user.Car, Is.Not.Null);
            Assert.That(user.Car.Name, Is.EqualTo("Jag"));
            Assert.That(user.Car.Age, Is.EqualTo(25));
            Assert.That(actual.Cars, Is.Not.Null);
            Assert.That(actual.Cars.Count, Is.EqualTo(2));
            var firstCar = actual.Cars.First();
            Assert.That(firstCar.Name, Is.EqualTo("Toyota"));
            Assert.That(firstCar.Age, Is.EqualTo(2));
            var secondCar = actual.Cars.Last();
            Assert.That(secondCar.Name, Is.EqualTo("Lexus"));
            Assert.That(secondCar.Age, Is.EqualTo(1));
            Assert.That(actual.Colors, Is.Null);
        }

        public class ModelWithCollectionsOfNullableTypes
        {
            public int Id { get; set; }
            public IEnumerable<User> Users { get; set; }
            public Car[] Cars { get; set; }
            public IList<Color> Colors { get; set; }
        }

        public class ModelWithTimeSpan
        {
            public TimeSpan Time { get; set; }
        }

        public class ModelWithLong
        {
            public long Time { get; set; }
        }

        [Test]
        public void FromObjectDictionary_does_Convert_long_to_TimeSpan()
        {
            var time = new TimeSpan(1,1,1,1);
            var map = new Dictionary<string, object> {
                [nameof(ModelWithTimeSpan.Time)] = time.Ticks
            };

            var dto = map.FromObjectDictionary<ModelWithTimeSpan>();
            Assert.That(dto.Time, Is.EqualTo(time));
            
            map = new Dictionary<string, object> {
                [nameof(ModelWithTimeSpan.Time)] = time
            };

            var dtoLong = map.FromObjectDictionary<ModelWithLong>();
            Assert.That(dtoLong.Time, Is.EqualTo(time.Ticks));
        }
        
        public enum CefLogSeverity
        {
            Default,
            Verbose,
            Debug = Verbose,
            Info,
            Warning,
            Error,
            ErrorReport,
            Disable = 99,
        }
        
        public class CefSettings
        {
            public CefLogSeverity LogSeverity { get; set; }
        }

        public class CefConfig
        {
            public string WindowTitle { get; set; }
            public string Icon { get; set; }
            public string CefPath { get; set; }
            public string[] Args { get; set; }
            public CefSettings CefSettings { get; set; }
            public string StartUrl { get; set; }
            public int? X { get; set; }
            public int? Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool CenterToScreen { get; set; }
            public bool HideConsoleWindow { get; set; }
        }

        [Test]
        public void Can_use_PopulateInstance_to_populate_enum()
        {
            var map = new Dictionary<string, object> {
                ["LogSeverity"] = "Verbose"
            };
            
            var config = new CefConfig { CefSettings = new CefSettings { LogSeverity = CefLogSeverity.Info } };
            map.PopulateInstance(config.CefSettings);
            
            Assert.That(config.CefSettings.LogSeverity, Is.EqualTo(CefLogSeverity.Verbose));
        }
        
        [Test]
        public void Can_convert_Dictionary_FromObjectDictionary()
        {
            var dict = new Dictionary<string,object>();
            var to = dict.FromObjectDictionary<Dictionary<string, object>>();
            Assert.That(to == dict);
        }
        
        [Test]
        public void Can_convert_inner_dictionary()
        {
            var map = new Dictionary<string, object>
            {
                { "FirstName", "Foo" },
                { "LastName", "Bar" },
                { "Car", new Dictionary<string, object>
                {
                    { "Name", "Tesla" },
                    { "Age", 2 }
                }}
            };

            var user = map.FromObjectDictionary<User>();

            Assert.That(user.FirstName, Is.EqualTo("Foo"));
            Assert.That(user.LastName, Is.EqualTo("Bar"));
            Assert.That(user.Car, Is.Not.Null);
            Assert.That(user.Car.Name, Is.EqualTo("Tesla"));
            Assert.That(user.Car.Age, Is.EqualTo(2));
        }

        [Test]
        public void Can_convert_inner_collection_of_dictionaries()
        {
            var map = new Dictionary<string, object>
            {
                { "Name", "Tesla" },
                { "Age", "2" },
                { "Specs", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        {"Item", "Model"},
                        {"Value", "S"}
                    },
                    new Dictionary<string, object>
                    {
                        {"Item", "Engine"},
                        {"Value", "Electric"}
                    },
                    new Dictionary<string, object>
                    {
                        {"Item", "Color"},
                        {"Value", "Red"}
                    },
                    new Dictionary<string, object>
                    {
                        {"Item", "PowerKW"},
                        {"Value", 285}
                    },
                    new Dictionary<string, object>
                    {
                        {"Item", "TorqueNm"},
                        {"Value", 430}
                    },
                }}
            };

            var carWithSpecs = map.FromObjectDictionary<CarWithSpecs>();

            Assert.That(carWithSpecs.Name, Is.EqualTo("Tesla"));
            Assert.That(carWithSpecs.Age, Is.EqualTo(2));
            Assert.That(carWithSpecs.Specs.Count, Is.EqualTo(5));
            var model = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "Model");
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Value, Is.EqualTo("S"));
            var engine = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "Engine");
            Assert.That(engine, Is.Not.Null);
            Assert.That(engine.Value, Is.EqualTo("Electric"));
            var color = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "Color");
            Assert.That(color, Is.Not.Null);
            Assert.That(color.Value, Is.EqualTo("Red"));
            var power = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "PowerKW");
            Assert.That(power, Is.Not.Null);
            Assert.That(power.Value, Is.EqualTo("285"));
            var torque = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "TorqueNm");
            Assert.That(torque, Is.Not.Null);
            Assert.That(torque.Value, Is.EqualTo("430"));
        }

        [Test]
        public void Can_convert_inner_array_of_dictionaries()
        {
            var map = new Dictionary<string, object>
            {
                { "Name", "Tesla" },
                { "Age", "2" },
                { "Specs", new[]
                {
                    new Dictionary<string, object>
                    {
                        {"Item", "Model"},
                        {"Value", "S"}
                    },
                    new Dictionary<string, object>
                    {
                        {"Item", "Engine"},
                        {"Value", "Electric"}
                    },
                    new Dictionary<string, object>
                    {
                        {"Item", "Color"},
                        {"Value", "Red"}
                    },
                    new Dictionary<string, object>
                    {
                        {"Item", "PowerKW"},
                        {"Value", 285}
                    },
                    new Dictionary<string, object>
                    {
                        {"Item", "TorqueNm"},
                        {"Value", 430}
                    },
                }}
            };

            var carWithSpecs = map.FromObjectDictionary<CarWithSpecs>();

            Assert.That(carWithSpecs.Name, Is.EqualTo("Tesla"));
            Assert.That(carWithSpecs.Age, Is.EqualTo(2));
            Assert.That(carWithSpecs.Specs.Count, Is.EqualTo(5));
            var model = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "Model");
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Value, Is.EqualTo("S"));
            var engine = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "Engine");
            Assert.That(engine, Is.Not.Null);
            Assert.That(engine.Value, Is.EqualTo("Electric"));
            var color = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "Color");
            Assert.That(color, Is.Not.Null);
            Assert.That(color.Value, Is.EqualTo("Red"));
            var power = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "PowerKW");
            Assert.That(power, Is.Not.Null);
            Assert.That(power.Value, Is.EqualTo("285"));
            var torque = carWithSpecs.Specs.SingleOrDefault(s => s.Item == "TorqueNm");
            Assert.That(torque, Is.Not.Null);
            Assert.That(torque.Value, Is.EqualTo("430"));
        }
    }


}