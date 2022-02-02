using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoCreateTables : DynamoTestBase
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Does_create_table_using_dynamodb_attributes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithDynamoAttributes>();

            var table = DynamoMetadata.GetTable<TableWithDynamoAttributes>();

            Assert.That(table.HashKey.Name, Is.EqualTo("D"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("C"));
            Assert.That(table.Fields.Length, Is.EqualTo(5));
        }

        [Test]
        public void Does_create_table_using_id_convention()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithIdConvention>();

            var table = DynamoMetadata.GetTable<TableWithIdConvention>();

            Assert.That(table.HashKey.Name, Is.EqualTo("Id"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("RangeKey"));
            Assert.That(table.Fields.Length, Is.EqualTo(3));
        }

        [Test]
        public void Does_create_table_using_convention_names()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithConventionNames>();

            var table = DynamoMetadata.GetTable<TableWithConventionNames>();

            Assert.That(table.HashKey.Name, Is.EqualTo("HashKey"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("RangeKey"));
            Assert.That(table.Fields.Length, Is.EqualTo(3));
        }

        [Test]
        public void Does_create_table_using_composite_index()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithCompositeKey>();

            var table = DynamoMetadata.GetTable<TableWithCompositeKey>();

            Assert.That(table.HashKey.Name, Is.EqualTo("D"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("C"));
            Assert.That(table.Fields.Length, Is.EqualTo(5));
        }

        [Test]
        public void Does_create_table_and_index_using_Interface_attrs()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithTypedGlobalIndex>();

            var table = DynamoMetadata.GetTable<TableWithTypedGlobalIndex>();

            Assert.That(table.HashKey.Name, Is.EqualTo("D"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("C"));
            Assert.That(table.Fields.Length, Is.EqualTo(5));

            Assert.That(table.GlobalIndexes.Count, Is.EqualTo(1));
            Assert.That(table.GlobalIndexes[0].HashKey.Name, Is.EqualTo("B"));
            Assert.That(table.GlobalIndexes[0].RangeKey.Name, Is.EqualTo("D"));
        }

        [Test]
        public void Does_create_table_with_ProvisionedThroughput()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithProvision>();

            var table = DynamoMetadata.GetTable<TableWithProvision>();
            Assert.That(table.HashKey.Name, Is.EqualTo("Id"));
            Assert.That(table.ReadCapacityUnits, Is.EqualTo(100));
            Assert.That(table.WriteCapacityUnits, Is.EqualTo(50));
        }

        [Test]
        public void Does_create_table_with_GlobalIndex_with_ProvisionedThroughput()
        {
            var db = (PocoDynamo)CreatePocoDynamo();
            db.RegisterTable<TableWithGlobalIndexProvision>();

            var table = DynamoMetadata.GetTable<TableWithGlobalIndexProvision>();
            Assert.That(table.HashKey.Name, Is.EqualTo("Id"));
            Assert.That(table.ReadCapacityUnits, Is.Null);
            Assert.That(table.WriteCapacityUnits, Is.Null);
            Assert.That(table.GlobalIndexes[0].ReadCapacityUnits, Is.EqualTo(100));
            Assert.That(table.GlobalIndexes[0].WriteCapacityUnits, Is.EqualTo(50));
        }

        [Test]
        public void Can_put_UserAuth()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<UserAuth>();
            db.GetTableMetadata<UserAuth>().LocalIndexes.Clear();
            db.InitSchema();

            db.PutItem(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                UserName = "mythz",
            });
        }

        [Test]
        public void Can_put_CustomUserAuth()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<CustomUserAuth>();
            db.GetTableMetadata<CustomUserAuth>().LocalIndexes.Clear();
            db.InitSchema();

            db.PutItem(new CustomUserAuth
            {
                Custom = "CustomUserAuth",
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                UserName = "demis.bellot@gmail.com",
            });
        }

        [Test]
        public void Does_create_Collection_Table()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Collection>();
            db.InitSchema();

            var table = db.GetTableMetadata<Collection>();
            Assert.That(table.GetField("Id").DbType, Is.EqualTo(DynamoType.Number));
            Assert.That(table.GetField("Title").DbType, Is.EqualTo(DynamoType.String));
            Assert.That(table.GetField("ArrayInts").DbType, Is.EqualTo(DynamoType.List));
            Assert.That(table.GetField("SetStrings").DbType, Is.EqualTo(DynamoType.StringSet));
            Assert.That(table.GetField("ArrayStrings").DbType, Is.EqualTo(DynamoType.List));
            Assert.That(table.GetField("ListInts").DbType, Is.EqualTo(DynamoType.List));
            Assert.That(table.GetField("ListStrings").DbType, Is.EqualTo(DynamoType.List));
            Assert.That(table.GetField("SetInts").DbType, Is.EqualTo(DynamoType.NumberSet));
            Assert.That(table.GetField("DictionaryInts").DbType, Is.EqualTo(DynamoType.Map));
            Assert.That(table.GetField("DictionaryStrings").DbType, Is.EqualTo(DynamoType.Map));
            Assert.That(table.GetField("PocoLookup").DbType, Is.EqualTo(DynamoType.Map));
            Assert.That(table.GetField("PocoLookupMap").DbType, Is.EqualTo(DynamoType.Map));
        }

        [Test]
        public void Can_put_empty_Collection()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Collection>();
            db.InitSchema();

            db.PutItem(new Collection
            {
                ArrayInts = new int[0],
                SetStrings = new HashSet<string>(),
                ArrayStrings = new string[0],
                ListInts = new List<int>(),
                ListStrings = new List<string>(),
                SetInts = new HashSet<int>(),
                DictionaryInts = new Dictionary<int, int>(),
                DictionaryStrings = new Dictionary<string, string>(),
                PocoLookup = new Dictionary<string, List<Poco>>(),
                PocoLookupMap = new Dictionary<string, List<Dictionary<string, Poco>>>(),
            });
        }

        [Test]
        public void Can_Create_and_put_populated_AllTypes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<AllTypes>();
            db.InitSchema();

            var dto = new AllTypes
            {
                Id = 1,
                NullableId = 2,
                Byte = 3,
                Short = 4,
                Int = 5,
                Long = 6,
                UShort = 7,
                UInt = 8,
                ULong = 9,
                Float = 1.1f,
                Double = 2.2,
                Decimal = 3.3M,
                String = "String",
                DateTime = new DateTime(2001, 01, 01),
                TimeSpan = new TimeSpan(1, 1, 1, 1, 1),
                DateTimeOffset = new DateTimeOffset(new DateTime(2001, 01, 01)),
                Guid = new Guid("DC8837C3-84FB-401B-AB59-CE799FF99142"),
                Char = 'A',
                NullableDateTime = new DateTime(2001, 01, 01),
                NullableTimeSpan = new TimeSpan(1, 1, 1, 1, 1),
                StringList = new[] { "A", "B", "C" }.ToList(),
                StringArray = new[] { "D", "E", "F" },
                StringMap = new Dictionary<string, string>
                {
                    {"A","1"},
                    {"B","2"},
                    {"C","3"},
                },
                IntStringMap = new Dictionary<int, string>
                {
                    { 1, "A" },
                    { 2, "B" },
                    { 3, "C" },
                },
                SubType = new SubType
                {
                    Id = 1,
                    Name = "Name"
                }
            };

            db.PutItem(dto);

            var row = db.GetItem<AllTypes>(1);

            Assert.That(dto, Is.EqualTo(row));
        }

        private static IPocoDynamo RegisterTypeGenerically<T>()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable(typeof(AllTypes<T>));
            db.InitSchema();

            return db;
        }

        [Test]
        public void Can_Create_and_put_populated_AllTypes_WithGenericTypeDefinition()
        {
            var db = RegisterTypeGenerically<Poco>();
            db = RegisterTypeGenerically<Node>();
            db = RegisterTypeGenerically<Country>();

            var dto = new AllTypes<Poco>
            {
                Id = 1,
                NullableId = 2,
                Byte = 3,
                Short = 4,
                Int = 5,
                Long = 6,
                UShort = 7,
                UInt = 8,
                ULong = 9,
                Float = 1.1f,
                Double = 2.2,
                Decimal = 3.3M,
                String = "String",
                DateTime = new DateTime(2001, 01, 01),
                TimeSpan = new TimeSpan(1, 1, 1, 1, 1),
                DateTimeOffset = new DateTimeOffset(new DateTime(2001, 01, 01)),
                Guid = new Guid("DC8837C3-84FB-401B-AB59-CE799FF99142"),
                Char = 'A',
                NullableDateTime = new DateTime(2001, 01, 01),
                NullableTimeSpan = new TimeSpan(1, 1, 1, 1, 1),
                StringList = new[] { "A", "B", "C" }.ToList(),
                StringArray = new[] { "D", "E", "F" },
                StringMap = new Dictionary<string, string>
                {
                    {"A","1"},
                    {"B","2"},
                    {"C","3"},
                },
                IntStringMap = new Dictionary<int, string>
                {
                    { 1, "A" },
                    { 2, "B" },
                    { 3, "C" },
                },
                SubType = new SubType
                {
                    Id = 1,
                    Name = "Name"
                },
                GenericType = new Poco()
                {
                    Id = 1,
                    Title = "GenericType"
                }
            };

            db.PutItem(dto);

            var row = db.GetItem<AllTypes<Poco>>(1);

            Assert.That(dto, Is.EqualTo(row));

            // This works!
            var results = db.FromScan<AllTypes<Poco>>(x => x.Id == 1).Exec();
            Assert.That(dto, Is.EqualTo(results.First()));

            // This works too!
            results = db.FromQuery<AllTypes<Poco>>(x => x.Id == 1).Exec();
            Assert.That(dto, Is.EqualTo(results.First()));

            // TODO: this fails. It loks like you can't access the members of subtypes due to the lambda expression failing to compile (see PocoDynamoExpression.cs)
            //            results = db.FromQuery<AllTypes<Poco>>().Filter(x => x.GenericType.Title == "GenericType").Exec();
            //            Assert.That(dto, Is.EqualTo(results.First()));

            // TODO: this fails. It loks like you can't access the members of subtypes due to the lambda expression failing to compile (see PocoDynamoExpression.cs)
            //            results = db.FromQuery<AllTypes<Poco>>().Filter(x => x.SubType.Name == "Name").Exec();
            //            Assert.That(dto, Is.EqualTo(results.First()));
        }

        [Test]
        public void Can_Create_and_put_empty_AllTypes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<AllTypes>();
            db.InitSchema();

            var dto = new AllTypes { Id = 1 };

            db.PutItem(dto);

            var row = db.GetItem<AllTypes>(1);

            Assert.That(dto, Is.EqualTo(row));
        }

        public class TableWithIgnoredFields
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            [IgnoreDataMember]
            public string LastName { get; set; }

            public string DisplayName
            {
                get { return FirstName + " " + LastName; }
            }

            [DataAnnotations.Ignore]
            public int IsIgnored { get; set; }
        }

        [Test]
        public void Does_ignore_fields()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithIgnoredFields>();
            db.InitSchema();

            db.PutItem(new TableWithIgnoredFields
            {
                Id = 1,
                FirstName = "Foo",
                LastName = "Bar",
                IsIgnored = 10,
            });

            var row = db.GetItem<TableWithIgnoredFields>(1);

            Assert.That(row.DisplayName, Is.EqualTo("Foo Bar"));
            Assert.That(row.IsIgnored, Is.EqualTo(0));

            var table = DynamoMetadata.GetTable<TableWithIgnoredFields>();
            var request = new GetItemRequest
            {
                TableName = table.Name,
                Key = db.Converters.ToAttributeKeyValue(db, table.HashKey, 1),
            };

            var raw = db.DynamoDb.GetItem(request);

            Assert.That(raw.Item.ContainsKey("Id"));
            Assert.That(raw.Item.ContainsKey("FirstName"));
            Assert.That(raw.Item.ContainsKey("LastName"));
            Assert.That(!raw.Item.ContainsKey("DisplayName"));
            Assert.That(!raw.Item.ContainsKey("IsIgnored"));

            var json = row.ToJson();
            Assert.That(json, Is.Not.Contains("LastName"));
        }
    }

}