using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.UseCase
{
    [Ignore("Robots Shard Use Case")]
    [TestFixture]
    public class ShardingUseCase
    {
        public class MasterRecord
        {
            public Guid Id { get; set; }
            public int RobotId { get; set; }
            public string RobotName { get; set; }
            public DateTime? LastActivated { get; set; }
        }

        public class Robot
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActivated { get; set; }
            public long CellCount { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        [Test]
        public void Shard_1000_Robots_over_10_shards()
        {
            const int NoOfShards = 10;
            const int NoOfRobots = 1000;

            var dbFactory = new OrmLiteConnectionFactory(
                "~/App_Data/robots-master.sqlite".MapAbsolutePath(), SqliteDialect.Provider);
            
            //var dbFactory = new OrmLiteConnectionFactory(
            //    "Data Source=localhost;Initial Catalog=RobotsMaster;Integrated Security=SSPI", 
            //    SqlServerDialect.Provider);
            
            //Create Master Table in Master DB
            using (var db = dbFactory.Open())
                db.CreateTable<MasterRecord>();

            NoOfShards.Times(i => {
                var shardId = "robots-shard" + i;
                dbFactory.RegisterConnection(shardId, "~/App_Data/{0}.sqlite".Fmt(shardId).MapAbsolutePath(), SqliteDialect.Provider);

                //Create Robot table in Shard
                using (var db = dbFactory.Open(shardId))
                    db.CreateTable<Robot>(); 
            });

            var newRobots = NoOfRobots.Times(i => //Create 1000 Robots
                new Robot { Id = i, Name = "R2D" + i, CreatedDate = DateTime.UtcNow, CellCount = DateTime.UtcNow.ToUnixTimeMs() % 100000 });

            foreach (var newRobot in newRobots)
            {
                using (IDbConnection db = dbFactory.Open()) //Open Connection to Master DB
                {
                    db.Insert(new MasterRecord { Id = Guid.NewGuid(), RobotId = newRobot.Id, RobotName = newRobot.Name });
                    using (IDbConnection robotShard = dbFactory.OpenDbConnection("robots-shard" + newRobot.Id % NoOfShards)) //Shard DB
                    {
                        robotShard.Insert(newRobot);
                    }
                }
            }

        }
    }
}