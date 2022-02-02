using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    public class XmlTest
    {
        public int Id { get; set; }
        public XmlValue Xml { get; set; } 
    }
    
    public class XmlTests : OrmLiteTestBase
    {
        [Test]
        public void Can_use_xml_in_postgresql()
        {
            OrmLiteUtils.PrintSql();
            var dbFactory = new OrmLiteConnectionFactory("Server=192.168.1.8;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200", PostgreSqlDialect.Provider);
            using (var db = dbFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<XmlTest>();

                db.Insert(new XmlTest { Id = 1, Xml = @"<attendee>
    <bio>
        <name>John Doe</name>
        <birthYear>1986</birthYear>
    </bio>
    <languages>
        <lang level=""5"">php</lang>
        <lang level=""4"">python</lang>
        <lang level=""2"">java</lang>
    </languages>
</attendee>" });
                
                db.Insert(new XmlTest { Id = 2, Xml = @"<attendee>
    <bio>
        <name>Tom Smith</name>
        <birthYear>1978</birthYear>
    </bio>
    <languages>
        <lang level=""5"">python</lang>
        <lang level=""3"">java</lang>
        <lang level=""1"">ruby</lang>
    </languages>
</attendee>" });


                var results = db.Column<string>(@"SELECT
                    (xpath('//bio/name/text()', Xml)::text[])[1]
                    FROM xml_test 
                    WHERE cast(xpath('//bio[birthYear>1980]', Xml) as text[]) != '{}'");
                
                Assert.That(results.Count, Is.EqualTo(1));
                Assert.That(results[0], Is.EqualTo("John Doe"));
            }
        }
    }
}