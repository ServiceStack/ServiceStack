using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Kingbase.Tests
{
    public class InsertTests : KingbaseTestBase
    {
        [Test]
        public Task Kingbase_Model_Insert()
        {
            var factory = BuildOrmLiteConnectionFactory();
            using var db = factory.OpenDbConnection();

            db.DropAndCreateTable<Person>();
            db.DropAndCreateTable<Address>();
            db.TableExists<Person>().Should().BeTrue();
            db.TableExists<Address>().Should().BeTrue();
            var now = DateTime.Now;
            var fixture = new Fixture();

            var address = fixture.Create<Address>();
            var id = db.Insert(address, selectIdentity: true);
            address.Id = (int)id;

            var person = fixture.Create<Person>();
            id = db.Insert(person, selectIdentity: true);
            person.Id = (int)id;
            return Task.CompletedTask;
        }
    }
}