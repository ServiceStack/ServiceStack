using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Serialization;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    [TestFixture]
    public class JsonDataContractSerializerTests : OrmLiteTestBase
    {
        [Test]
        public void Can_save_and_load_complex_type()
        {
            SqlServerDialect.Provider.StringSerializer = new JsonDataContractSerializer();

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Group>();

                var group = new Group
                {
                    Name = "Group Name",
                    ShortName = "GN",
                    GroupTypeId = 1,
                    BirthDay = new DateTime(1980,01,09),
                    ContactDetails = new ContactDetails {
                        Address = "Address",
                        CompanyName = "Company",
                        FirstName = "First",
                        LastName = "Last",
                        Title = "Title",
                    }
                };

                db.Save(group);

                var result = db.SingleById<Group>(group.GroupId);

                result.PrintDump();

                var results = db.Select<Group>("DATEPART(d,BirthDay) < 10");
                results.PrintDump();
            }            
        }
    }

    public class Group
    {
        [Alias("GroupID")]
        [AutoIncrement]
        [PrimaryKey]
        public int GroupId { get; set; }

        [Alias("Name")]
        public string Name { get; set; }

        [Alias("ShortName")]
        public string ShortName { get; set; }

        [Alias("GroupTypeID")]
        public int GroupTypeId { get; set; }

        [Alias("ContactDetails")]
        public ContactDetails ContactDetails { get; set; }

        public DateTime BirthDay { get; set; }
    }

    [DataContract]
    [Serializable]
    public class ContactDetails
    {
        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public string CompanyName { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Address { get; set; }
    }
}