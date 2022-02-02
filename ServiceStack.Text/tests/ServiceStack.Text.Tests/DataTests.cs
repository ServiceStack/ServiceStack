using System;
using System.Runtime.Serialization;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class DataStressTests
        : TestBase
    {
        public class TestClass
        {
            public string Value { get; set; }

            public bool Equals(TestClass other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(other.Value, Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(TestClass)) return false;
                return Equals((TestClass)obj);
            }

            public override int GetHashCode()
            {
                return (Value != null ? Value.GetHashCode() : 0);
            }
        }

        [Test]
        public void serialize_Customer_BOLID()
        {
            var customer = NorthwindFactory.Customer(
                "BOLID", "Bólido Comidas preparadas", "Martín Sommer", "Owner", "C/ Araquil, 67",
                "Madrid", null, "28023", "Spain", "(91) 555 22 82", "(91) 555 91 99", null);

            var model = new TestClass
            {
                Value = TypeSerializer.SerializeToString(customer)
            };

            var toModel = Serialize(model);
            Console.WriteLine("toModel.Value: " + toModel.Value);

            var toCustomer = TypeSerializer.DeserializeFromString<Customer>(toModel.Value);
            Console.WriteLine("customer.Address: " + customer.Address);
            Console.WriteLine("toCustomer.Address: " + toCustomer.Address);
        }

        [DataContract]
        public class GetValuesResponse
        {
            public GetValuesResponse()
            {
                this.ResponseStatus = new ResponseStatus();

                this.Values = new ArrayOfString();
            }

            [DataMember]
            public ArrayOfString Values { get; set; }

            [DataMember]
            public ResponseStatus ResponseStatus { get; set; }
        }

        [Test]
        public void serialize_GetValuesResponse()
        {
            const string responseJsv = "{Values:[\"{Id:1,LastName:Davolio,FirstName:Nancy,Title:Sales Representative,TitleOfCourtesy:Ms.,BirthDate:1948-12-08,HireDate:1992-05-01,Address:507 - 20th Ave. E. Apt. 2A,City:Seattle,Region:WA,PostalCode:98122,Country:USA,HomePhone:(206) 555-9857,Extension:5467,Notes:Education includes a BA in psychology from Colorado State University in 1970.  She also completed 'The Art of the Cold Call.'  Nancy is a member of Toastmasters International.,ReportsTo:2,PhotoPath:http://accweb/emmployees/davolio.bmp}\",\"{Id:2,LastName:Fuller,FirstName:Andrew,Title:\"\"Vice President, Sales\"\",TitleOfCourtesy:Dr.,BirthDate:1952-02-19,HireDate:1992-08-14,Address:908 W. Capital Way,City:Tacoma,Region:WA,PostalCode:98401,Country:USA,HomePhone:(206) 555-9482,Extension:3457,Notes:\"\"Andrew received his BTS commercial in 1974 and a Ph.D. in international marketing from the University of Dallas in 1981.  He is fluent in French and Italian and reads German.  He joined the company as a sales representative, was promoted to sales manager in January 1992 and to vice president of sales in March 1993.  Andrew is a member of the Sales Management Roundtable, the Seattle Chamber of Commerce, and the Pacific Rim Importers Association.\"\",PhotoPath:http://accweb/emmployees/fuller.bmp}\",\"{Id:3,LastName:Leverling,FirstName:Janet,Title:Sales Representative,TitleOfCourtesy:Ms.,BirthDate:1963-08-30,HireDate:1992-04-01,Address:722 Moss Bay Blvd.,City:Kirkland,Region:WA,PostalCode:98033,Country:USA,HomePhone:(206) 555-3412,Extension:3355,Notes:Janet has a BS degree in chemistry from Boston College (1984).  She has also completed a certificate program in food retailing management.  Janet was hired as a sales associate in 1991 and promoted to sales representative in February 1992.,ReportsTo:2,PhotoPath:http://accweb/emmployees/leverling.bmp}\",\"{Id:4,LastName:Peacock,FirstName:Margaret,Title:Sales Representative,TitleOfCourtesy:Mrs.,BirthDate:1937-09-19,HireDate:1993-05-03,Address:4110 Old Redmond Rd.,City:Redmond,Region:WA,PostalCode:98052,Country:USA,HomePhone:(206) 555-8122,Extension:5176,Notes:Margaret holds a BA in English literature from Concordia College (1958) and an MA from the American Institute of Culinary Arts (1966).  She was assigned to the London office temporarily from July through November 1992.,ReportsTo:2,PhotoPath:http://accweb/emmployees/peacock.bmp}\",\"{Id:5,LastName:Buchanan,FirstName:Steven,Title:Sales Manager,TitleOfCourtesy:Mr.,BirthDate:1955-03-04,HireDate:1993-10-17,Address:14 Garrett Hill,City:London,PostalCode:SW1 8JR,Country:UK,HomePhone:(71) 555-4848,Extension:3453,Notes:\"\"Steven Buchanan graduated from St. Andrews University, Scotland, with a BSC degree in 1976.  Upon joining the company as a sales representative in 1992, he spent 6 months in an orientation program at the Seattle office and then returned to his permanent post in London.  He was promoted to sales manager in March 1993.  Mr. Buchanan has completed the courses 'Successful Telemarketing' and 'International Sales Management.'  He is fluent in French.\"\",ReportsTo:2,PhotoPath:http://accweb/emmployees/buchanan.bmp}\",\"{Id:6,LastName:Suyama,FirstName:Michael,Title:Sales Representative,TitleOfCourtesy:Mr.,BirthDate:1963-07-02,HireDate:1993-10-17,Address:Coventry House Miner Rd.,City:London,PostalCode:EC2 7JR,Country:UK,HomePhone:(71) 555-7773,Extension:428,Notes:\"\"Michael is a graduate of Sussex University (MA, economics, 1983) and the University of California at Los Angeles (MBA, marketing, 1986).  He has also taken the courses 'Multi-Cultural Selling' and 'Time Management for the Sales Professional.'  He is fluent in Japanese and can read and write French, Portuguese, and Spanish.\"\",ReportsTo:5,PhotoPath:http://accweb/emmployees/davolio.bmp}\",\"{Id:7,LastName:King,FirstName:Robert,Title:Sales Representative,TitleOfCourtesy:Mr.,BirthDate:1960-05-29,HireDate:1994-01-02,Address:Edgeham Hollow Winchester Way,City:London,PostalCode:RG1 9SP,Country:UK,HomePhone:(71) 555-5598,Extension:465,Notes:\"\"Robert King served in the Peace Corps and traveled extensively before completing his degree in English at the University of Michigan in 1992, the year he joined the company.  After completing a course entitled 'Selling in Europe,' he was transferred to the London office in March 1993.\"\",ReportsTo:5,PhotoPath:http://accweb/emmployees/davolio.bmp}\",\"{Id:8,LastName:Callahan,FirstName:Laura,Title:Inside Sales Coordinator,TitleOfCourtesy:Ms.,BirthDate:1958-01-09,HireDate:1994-03-05,Address:4726 - 11th Ave. N.E.,City:Seattle,Region:WA,PostalCode:98105,Country:USA,HomePhone:(206) 555-1189,Extension:2344,Notes:Laura received a BA in psychology from the University of Washington.  She has also completed a course in business French.  She reads and writes French.,ReportsTo:2,PhotoPath:http://accweb/emmployees/davolio.bmp}\",\"{Id:9,LastName:Dodsworth,FirstName:Anne,Title:Sales Representative,TitleOfCourtesy:Ms.,BirthDate:1966-01-27,HireDate:1994-11-15,Address:7 Houndstooth Rd.,City:London,PostalCode:WG2 7LT,Country:UK,HomePhone:(71) 555-4444,Extension:452,Notes:Anne has a BA degree in English from St. Lawrence College.  She is fluent in French and German.,ReportsTo:5,PhotoPath:http://accweb/emmployees/davolio.bmp}\"],ResponseStatus:{Errors:[]}}";

            var response = TypeSerializer.DeserializeFromString<GetValuesResponse>(responseJsv);

            Assert.That(response.Values, Has.Count.EqualTo(9));
        }

        [Test]
        public void Does_Combine_Byte()
        {
            var wordBytes = "HELLO".ToUtf8Bytes().Combine(" ".ToUtf8Bytes(), "WORLD".ToUtf8Bytes());
            var word = wordBytes.FromUtf8Bytes();
            Assert.That(word, Is.EqualTo("HELLO WORLD"));
        }
    }
}