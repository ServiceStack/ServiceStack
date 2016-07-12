#if !NETCORE_SUPPORT
using System;
using System.Xml.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Xlinq
{
    [TestFixture]
    public class XlinqExtensionsTests
    {
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
          + "<Envelope xmlns=\"http://schemas.xmlsoap.org/soap/envelope/\">"
          + "  <Body>"
          + "    <Element1>"
          + "      <Element2 day=\"2009-10-18\">"
          + "        <Element3 name=\"Joe\">"
          + "          <Element4 time=\"1\">"
          + "            <Element5 amount=\"0\" price=\"16.58\"/>"
          + "          </Element4>"
          + "        </Element3>"
          + "        <Element3 name=\"Fred\">"
          + "          <Element4 time=\"5\">"
          + "            <Element5 amount=\"0\" price=\"15.41\"/>"
          + "          </Element4>"
          + "        </Element3>"
          + "      </Element2>"
          + "    </Element1>"
          + "  </Body>"
          + "</Envelope>";

        public class XmlData : IHasId<int>
        {
            [AutoIncrement]
            public int Id { get; set; }
            public string Day { get; set; }
            public string Name { get; set; }
            public int Time { get; set; }
            public int Amount { get; set; }
            public decimal Price { get; set; }
        }

        [Test]
        public void Insert_data_from_xml_into_db()
        {
            //OrmLiteConfig.DialectProvider = SqlServerOrmLiteDialectProvider.Instance;
            OrmLiteConfig.DialectProvider = SqliteOrmLiteDialectProvider.Instance;

            var element2 = XElement.Parse(xml).AnyElement("Body").AnyElement("Element1").AnyElement("Element2");

            using (var db = ":memory:".OpenDbConnection())
            {
                db.CreateTable<XmlData>(true);
                foreach (var element3 in element2.AllElements("Element3"))
                {
                    var xmlData = new XmlData
                    {
                        Day = element2.AnyAttribute("day").Value,
                        Name = element3.AnyAttribute("name").Value,
                        Time = int.Parse(element3.FirstElement().AnyAttribute("time").Value),
                        Amount = int.Parse(element3.FirstElement().FirstElement().AnyAttribute("amount").Value),
                        Price = decimal.Parse(element3.FirstElement().FirstElement().AnyAttribute("price").Value),
                    };
                    db.Insert(xmlData);
                }
                db.Select<XmlData>().ForEach(x => Console.WriteLine(TypeSerializer.SerializeToString(x)));
            }
        }

    }
}
#endif
