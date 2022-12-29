using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
#if NETCORE
    [Ignore("Fix Northwind.dll")]
#endif
    public class CsvSerializerTests
    {
        static CsvSerializerTests()
        {
            NorthwindData.LoadData(false);
        }

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            JsConfig.SkipDateTimeConversion = true;
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            JsConfig.Reset();
        }

        public void Serialize<T>(T data)
        {
            //TODO: implement serializer and test properly
            var csv = CsvSerializer.SerializeToString(data);
            csv.Print();
        }

        public object SerializeAndDeserialize<T>(T data)
        {
            var csv = CsvSerializer.SerializeToString(data);
            csv.Print();

            var dto = CsvSerializer.DeserializeFromString<T>(csv);
            AssertEqual(dto, data);

            using (var reader = new StringReader(csv))
            {
                dto = CsvSerializer.DeserializeFromReader<T>(reader);
                AssertEqual(dto, data);
            }

            using (var ms = new MemoryStream(csv.ToUtf8Bytes()))
            {
                dto = CsvSerializer.DeserializeFromStream<T>(ms);
                AssertEqual(dto, data);
            }

            using (var ms = new MemoryStream(csv.ToUtf8Bytes()))
            {
                dto = (T)CsvSerializer.DeserializeFromStream(typeof(T), ms);
                AssertEqual(dto, data);
            }

            return dto;
        }

        private static void AssertEqual<T>(T dto, T data)
        {
            var dataArray = data is IEnumerable ? (data as IEnumerable).Map(x => x).ToArray() : null;
            var dtoArray = dto is IEnumerable ? (dto as IEnumerable).Map(x => x).ToArray() : null;

            if (dataArray != null && dtoArray != null)
                Assert.That(dtoArray, Is.EquivalentTo(dataArray));
            else
                Assert.That(dto, Is.EqualTo(data));
        }

        [Test]
        public void Does_parse_new_lines()
        {
            Assert.That(CsvReader.ParseLines("A,B\nC,D"), Is.EquivalentTo(new[] { "A,B", "C,D" }));
            Assert.That(CsvReader.ParseLines("A,B\nC,D\n"), Is.EquivalentTo(new[] { "A,B", "C,D" }));
            Assert.That(CsvReader.ParseLines("A,B\nC,D\n\n"), Is.EquivalentTo(new[] { "A,B", "C,D" }));

            Assert.That(CsvReader.ParseLines("A,B\r\nC,D"), Is.EquivalentTo(new[] { "A,B", "C,D" }));
            Assert.That(CsvReader.ParseLines("A,B\r\nC,D\r\n"), Is.EquivalentTo(new[] { "A,B", "C,D" }));
            Assert.That(CsvReader.ParseLines("A,B\r\nC,D\r\n\r\n"), Is.EquivalentTo(new[] { "A,B", "C,D" }));

            Assert.That(CsvReader.ParseLines("\"A,B\"\n\"C,D\""), Is.EquivalentTo(new[] { "\"A,B\"", "\"C,D\"" }));
            Assert.That(CsvReader.ParseLines("\"A,B\",B\nC,\"C,D\""), Is.EquivalentTo(new[] { "\"A,B\",B", "C,\"C,D\"" }));

            Assert.That(CsvReader.ParseLines("\"A\nB\",B\nC,\"C\r\nD\""), Is.EquivalentTo(new[] { "\"A\nB\",B", "C,\"C\r\nD\"" }));
        }

        [Test]
        public void Does_parse_fields()
        {
            Assert.That(CsvReader.ParseFields("A,B"), Is.EquivalentTo(new[] { "A", "B" }));
            Assert.That(CsvReader.ParseFields("\"A\",B"), Is.EquivalentTo(new[] { "A", "B" }));
            Assert.That(CsvReader.ParseFields("\"A\",\"B,C\""), Is.EquivalentTo(new[] { "A", "B,C" }));
            Assert.That(CsvReader.ParseFields("\"A\nB\",\"B,\r\nC\""), Is.EquivalentTo(new[] { "A\nB", "B,\r\nC" }));
            Assert.That(CsvReader.ParseFields("\"A\"\",B\""), Is.EquivalentTo(new[] { "A\",B" }));

            Assert.That(CsvReader.ParseFields(",A,B"), Is.EquivalentTo(new[] { null, "A", "B" }));
            Assert.That(CsvReader.ParseFields("A,,B"), Is.EquivalentTo(new[] { "A", null, "B" }));
            Assert.That(CsvReader.ParseFields("A,B,"), Is.EquivalentTo(new[] { "A", "B", null }));

            Assert.That(CsvReader.ParseFields("\"\",A,B"), Is.EquivalentTo(new[] { "", "A", "B" }));
            Assert.That(CsvReader.ParseFields("A,\"\",B"), Is.EquivalentTo(new[] { "A", "", "B" }));
            Assert.That(CsvReader.ParseFields("A,B,\"\""), Is.EquivalentTo(new[] { "A", "B", "" }));
        }

        [Test]
        public void Does_parse_fields_with_unmatchedJsMark()
        {
            Assert.That(CsvReader.ParseFields("{A,B"), Is.EqualTo(new[] { "{A", "B" }));
            Assert.That(CsvReader.ParseFields("{A},B"), Is.EqualTo(new[] { "{A}", "B" }));
            Assert.That(CsvReader.ParseFields("[A,B"), Is.EqualTo(new[] { "[A", "B" }));
            Assert.That(CsvReader.ParseFields("[A],B"), Is.EqualTo(new[] { "[A]", "B" }));
            Assert.That(CsvReader.ParseFields("[{A],B"), Is.EqualTo(new[] { "[{A]", "B" }));
            Assert.That(CsvReader.ParseFields("[A},B"), Is.EqualTo(new[] { "[A}", "B" }));
            Assert.That(CsvReader.ParseFields("[[A],B"), Is.EqualTo(new[] { "[[A]", "B" }));
            Assert.That(CsvReader.ParseFields("A],B"), Is.EqualTo(new[] { "A]", "B" }));
            Assert.That(CsvReader.ParseFields("A},B"), Is.EqualTo(new[] { "A}", "B" }));
        }

        [Test]
        public void Can_Serialize_Movie()
        {
            Serialize(MoviesData.Movies[0]);
        }

        [Test]
        public void Can_Serialize_Movies()
        {
            SerializeAndDeserialize(MoviesData.Movies);
        }

        [Test]
        public void Can_Serialize_inherited_Movies()
        {
            SerializeAndDeserialize(new Movies(MoviesData.Movies));
        }

        [Test]
        public void Does_Serialize_back_into_Array()
        {
            var dto = SerializeAndDeserialize(MoviesData.Movies.ToArray());
            Assert.That(dto.GetType().IsArray);
        }

        public class SubMovie
        {
            public DateTime ReleaseDate { get; set; }
            public string Title { get; set; }
            public decimal Rating { get; set; }
            public string ImdbId { get; set; }
        }

        [Test]
        public void Does_serialize_partial_DTO_in_order_of_Headers()
        {
            var subMovies = MoviesData.Movies.Map(x => x.ConvertTo<SubMovie>());
            var csv = CsvSerializer.SerializeToString(subMovies);

            csv.Print();
            Assert.That(csv, Does.StartWith("ReleaseDate,Title,Rating,ImdbId\r\n"));

            var movies = csv.FromCsv<List<Movie>>();

            Assert.That(movies.Count, Is.EqualTo(subMovies.Count));
            for (int i = 0; i < subMovies.Count; i++)
            {
                var actual = movies[i];
                var expected = MoviesData.Movies[i];

                Assert.That(actual.Id, Is.EqualTo(0));
                Assert.That(actual.ReleaseDate, Is.EqualTo(expected.ReleaseDate));
                Assert.That(actual.Title, Is.EqualTo(expected.Title));
                Assert.That(actual.Rating, Is.EqualTo(expected.Rating));
                Assert.That(actual.ImdbId, Is.EqualTo(expected.ImdbId));
            }
        }

        [Test]
        public void Can_Serialize_MovieResponse_Dto()
        {
            SerializeAndDeserialize(new MovieResponse { Movie = MoviesData.Movies[0] });
        }

        [Test]
        public void Can_Serialize_MoviesResponse_Dto()
        {
            SerializeAndDeserialize(new MoviesResponse { Movies = MoviesData.Movies });
        }

        [Test]
        public void Can_Serialize_MoviesResponse2_Dto()
        {
            SerializeAndDeserialize(new MoviesResponse2 { Movies = MoviesData.Movies });
        }

        [Test]
        public void Can_Deserialize_into_String_Dictionary()
        {
            var csv = MoviesData.Movies.ToCsv();

            var dynamicMap = csv.FromCsv<List<Dictionary<string, string>>>();
            Assert.That(dynamicMap.Count, Is.EqualTo(MoviesData.Movies.Count));

            dynamicMap.PrintDump();

            var movie = MoviesData.Movies[0];
            var map = dynamicMap[0];

            Assert.That(map["Id"], Is.EqualTo(movie.Id.ToString()));
            Assert.That(map["ImdbId"], Is.EqualTo(movie.ImdbId));
            Assert.That(map["Title"], Is.EqualTo(movie.Title));
            Assert.That(map["Rating"], Is.EqualTo(movie.Rating.ToString(CultureInfo.InvariantCulture)));
            Assert.That(map["Director"], Is.EqualTo(movie.Director));
            Assert.That(map["ReleaseDate"], Is.EqualTo(movie.ReleaseDate.ToJsv()));
            Assert.That(map["TagLine"], Is.EqualTo(movie.TagLine));
            Assert.That(map["Genres"], Is.EqualTo(movie.Genres.ToJsv()));
        }

        [Test]
        public void Can_deserialize_into_String_List()
        {
            var csv = MoviesData.Movies.ToCsv();

            var dynamicList = csv.FromCsv<List<List<string>>>();
            Assert.That(dynamicList.Count - 1, Is.EqualTo(MoviesData.Movies.Count));

            dynamicList.PrintDump();

            var movie = MoviesData.Movies[0];
            var headers = dynamicList[0];
            var first = dynamicList[1];

            Assert.That(headers[0], Is.EqualTo("Id"));
            Assert.That(headers[1], Is.EqualTo("ImdbId"));
            Assert.That(headers[2], Is.EqualTo("Title"));
            Assert.That(headers[3], Is.EqualTo("Rating"));
            Assert.That(headers[4], Is.EqualTo("Director"));
            Assert.That(headers[5], Is.EqualTo("ReleaseDate"));
            Assert.That(headers[6], Is.EqualTo("TagLine"));
            Assert.That(headers[7], Is.EqualTo("Genres"));

            Assert.That(first[0], Is.EqualTo(movie.Id.ToString()));
            Assert.That(first[1], Is.EqualTo(movie.ImdbId));
            Assert.That(first[2], Is.EqualTo(movie.Title));
            Assert.That(first[3], Is.EqualTo(movie.Rating.ToString(CultureInfo.InvariantCulture)));
            Assert.That(first[4], Is.EqualTo(movie.Director));
            Assert.That(first[5], Is.EqualTo(movie.ReleaseDate.ToJsv()));
            Assert.That(first[6], Is.EqualTo(movie.TagLine));
            Assert.That(first[7], Is.EqualTo(movie.Genres.ToJsv()));
        }

        [Test]
        public void Can_Serialize_using_custom_CSV_ItemString()
        {
            CsvConfig.ItemSeperatorString = ";";
            var csv = NorthwindData.OrderDetails[0].ToCsv();

            Assert.That(csv, Is.EqualTo(
                "Id;OrderId;ProductId;UnitPrice;Quantity;Discount\r\n10248/11;10248;11;14;12;0\r\n"));

            var row = csv.FromCsv<OrderDetail>();

            Assert.That(row, Is.EqualTo(NorthwindData.OrderDetails[0]));

            CsvConfig.Reset();
        }

        [Test]
        public void Can_serialize_ObjectDictionary_list()
        {
            var rows = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Id", 1 },
                    { "CustomerId", "ALFKI" },
                },
                new Dictionary<string, object>
                {
                    { "Id", 2 },
                    { "CustomerId", "ANATR" },
                },
            };

            Assert.That(rows.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,CustomerId\n1,ALFKI\n2,ANATR").Or.EqualTo("CustomerId,Id\nALFKI,1\nANATR,2"));
        }

        [Test]
        public void Can_serialize_StringDictionary_list()
        {
            var rows = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "Id", "1" },
                    { "CustomerId", "ALFKI" },
                },
                new Dictionary<string, string>
                {
                    { "Id", "2" },
                    { "CustomerId", "ANATR" },
                },
            };

            Assert.That(rows.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,CustomerId\n1,ALFKI\n2,ANATR").Or.EqualTo("CustomerId,Id\nALFKI,1\nANATR,2"));
        }

        [Test]
        public void Can_serialize_single_ObjectDictionary_or_ObjectKvps()
        {
            var row = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "CustomerId", "ALFKI" },
            };

            Assert.That(row.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,CustomerId\n1,ALFKI").Or.EqualTo("CustomerId,Id\nALFKI,1"));

            var kvps = new[]
            {
                new KeyValuePair<string, object>("Id", 1),
                new KeyValuePair<string, object>("CustomerId", "ALFKI"),
            };

            Assert.That(kvps.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,CustomerId\n1,ALFKI").Or.EqualTo("CustomerId,Id\nALFKI,1"));
        }

        [Test]
        public void Can_serialize_single_ObjectDictionary_or_ObjectKvps_WithEmptyString()
        {
            var row = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "CustomerId", "" },
            };

            Assert.That(row.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,CustomerId\n1,").Or.EqualTo("CustomerId,Id\n,1"));

            var kvps = new[]
            {
                new KeyValuePair<string, object>("Id", 1),
                new KeyValuePair<string, object>("CustomerId", ""),
            };

            Assert.That(kvps.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,CustomerId\n1,").Or.EqualTo("CustomerId,Id\n,1"));
        }
        
        [Test]
        public void Can_serialize_single_StringDictionary_or_StringKvps()
        {
            var row = new Dictionary<string, string>
            {
                { "Id", "1" },
                { "CustomerId", "ALFKI" },
            };

            Assert.That(row.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,CustomerId\n1,ALFKI").Or.EqualTo("CustomerId,Id\nALFKI,1"));

            var kvps = new[]
            {
                new KeyValuePair<string, string>("Id", "1"),
                new KeyValuePair<string, string>("CustomerId", "ALFKI"),
            };

            Assert.That(kvps.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,CustomerId\n1,ALFKI").Or.EqualTo("CustomerId,Id\nALFKI,1"));
        }

        [Test]
        public void Can_serialize_fields_with_double_quotes()
        {
            var person = new Person { Id = 1, Name = "\"Mr. Lee\"" };
            Assert.That(person.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,Name\n1,\"\"\"Mr. Lee\"\"\""));
            var fromCsv = person.ToCsv().FromCsv<Person>();
            Assert.That(fromCsv, Is.EqualTo(person));
            
            person = new Person { Id = 1, Name = "\"Anon\" Review" };
            Assert.That(person.ToCsv().NormalizeNewLines(), Is.EqualTo("Id,Name\n1,\"\"\"Anon\"\" Review\""));
            fromCsv = person.ToCsv().FromCsv<Person>();
            Assert.That(fromCsv, Is.EqualTo(person));
        }
        
        public Order Clone(Order o) => new Order {
            Id = o.Id,
            CustomerId = o.CustomerId,
            EmployeeId = o.EmployeeId,
            OrderDate = o.OrderDate,
            RequiredDate = o.RequiredDate,
            ShippedDate = o.ShippedDate,
            ShipVia = o.ShipVia,
            Freight = o.Freight,
            ShipName = o.ShipName,
            ShipAddress = o.ShipAddress,
            ShipCity = o.ShipCity,
            ShipRegion = o.ShipRegion,
            ShipPostalCode = o.ShipPostalCode,
            ShipCountry = o.ShipCountry,
        };

        [Test]
        public void Can_only_serialize_NonDefaultValues()
        {
            using var scope = JsConfig.With(new Config {
                ExcludeDefaultValues = true
            });

            var orders = NorthwindData.Orders.Take(5).Map(Clone);
            orders.ForEach(x => {
                //non-default min values
                x.RequiredDate = DateTime.MinValue;
                x.ShipVia = 0;
                
                //default values
                x.ShippedDate = null;
                x.EmployeeId = default;
                x.Freight = default;
                x.ShipPostalCode = null;
                x.ShipCountry = null;
            });

            var csv = orders.ToCsv();
            // csv.Print();
            var headers = csv.LeftPart('\r');
            headers.Print();
            Assert.That(headers, Is.EquivalentTo(
                "Id,CustomerId,OrderDate,RequiredDate,ShipVia,ShipName,ShipAddress,ShipCity,ShipRegion"));
        }

        [Test]
        public void serialize_Category()
        {
            SerializeAndDeserialize(NorthwindData.Categories[0]);
        }

        [Test]
        public void serialize_Categories()
        {
            SerializeAndDeserialize(NorthwindData.Categories);
        }

        [Test]
        public void serialize_Customer()
        {
            SerializeAndDeserialize(NorthwindData.Customers[0]);
        }

        [Test]
        public void serialize_Customers()
        {
            SerializeAndDeserialize(NorthwindData.Customers);
        }

        [Test]
        public void serialize_Employee()
        {
            SerializeAndDeserialize(NorthwindData.Employees[0]);
        }

        [Test]
        public void serialize_Employees()
        {
            SerializeAndDeserialize(NorthwindData.Employees);
        }

        [Test]
        public void serialize_EmployeeTerritory()
        {
            SerializeAndDeserialize(NorthwindData.EmployeeTerritories[0]);
        }

        [Test]
        public void serialize_EmployeeTerritories()
        {
            SerializeAndDeserialize(NorthwindData.EmployeeTerritories);
        }

        [Test]
        public void serialize_OrderDetail()
        {
            SerializeAndDeserialize(NorthwindData.OrderDetails[0]);
        }

        [Test]
        public void serialize_OrderDetails()
        {
            SerializeAndDeserialize(NorthwindData.OrderDetails);
        }

        [Test]
        public void serialize_Order()
        {
            SerializeAndDeserialize(NorthwindData.Orders[0]);
        }

        [Test]
        public void serialize_Orders()
        {
            Serialize(NorthwindData.Orders);
        }

        [Test]
        public void serialize_Product()
        {
            SerializeAndDeserialize(NorthwindData.Products[0]);
        }

        [Test]
        public void serialize_Products()
        {
            SerializeAndDeserialize(NorthwindData.Products);
        }

        [Test]
        public void serialize_Region()
        {
            SerializeAndDeserialize(NorthwindData.Regions[0]);
        }

        [Test]
        public void serialize_Regions()
        {
            SerializeAndDeserialize(NorthwindData.Regions);
        }

        [Test]
        public void serialize_Shipper()
        {
            SerializeAndDeserialize(NorthwindData.Shippers[0]);
        }

        [Test]
        public void serialize_Shippers()
        {
            SerializeAndDeserialize(NorthwindData.Shippers);
        }

        [Test]
        public void serialize_Supplier()
        {
            SerializeAndDeserialize(NorthwindData.Suppliers[0]);
        }

        [Test]
        public void serialize_Suppliers()
        {
            SerializeAndDeserialize(NorthwindData.Suppliers);
        }

        [Test]
        public void serialize_Territory()
        {
            SerializeAndDeserialize(NorthwindData.Territories[0]);
        }

        [Test]
        public void serialize_Territories()
        {
            SerializeAndDeserialize(NorthwindData.Territories);
        }
    }
}