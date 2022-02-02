using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    public class CustomSelectModel
    {
        [PrimaryKey]
        public Guid DeviceId { get; set; }

        [CustomSelect("NULL")]
        public Guid SiteId { get; set; }

        [CustomSelect("NULL")]
        public Guid GroupId { get; set; }
    }
    
    public class CustomSelectTests : OrmLiteTestBase
    {
        [Test]
        public void Does_IgnoreOnInsert()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<CustomSelectModel>();
                
            var row = new CustomSelectModel
            {
                DeviceId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                SiteId = Guid.NewGuid(),
            };
                
            db.Insert(row);

            db.Update(row);

            db.Save(row);
        }
    }
}