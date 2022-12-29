using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql.Tests.Issues
{
    [TestFixture, Explicit]
    public class CustomMappingIssue
         : OrmLiteTestBase
    {
        private string CreateChecksTableSql = @"
CREATE TABLE checks (
  check_id bigint(11) NOT NULL AUTO_INCREMENT,
  room_state_id bigint(11) NOT NULL DEFAULT 0,
  entry_stamp datetime DEFAULT NULL,
  student_id int(11) NOT NULL DEFAULT 0,
  reason varchar(60) DEFAULT NULL,
  comments text DEFAULT NULL,
  check_type_id bigint(20) NOT NULL DEFAULT 0,
  check_dt datetime DEFAULT NULL,
  grace tinyint(4) UNSIGNED NOT NULL DEFAULT 0,
  curfew_dt datetime NOT NULL DEFAULT '0000-00-00 00:00:00',
  excused_by int(11) NOT NULL DEFAULT 0,
  status char(1) NOT NULL DEFAULT '',
  points int(11) NOT NULL DEFAULT 0,
  check_class char(2) NOT NULL DEFAULT '',
  leave_id int(11) NOT NULL DEFAULT 0,
  night_of date NOT NULL DEFAULT '0000-00-00',
  violation_dt datetime NOT NULL DEFAULT '0000-00-00 00:00:00',
  excused_reason varchar(255) NOT NULL DEFAULT '',
  excused_dt datetime DEFAULT '0000-00-00 00:00:00',
  imported tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (check_id),
  INDEX check_class (check_class),
  INDEX check_dt (check_dt),
  INDEX check_type_id (check_type_id),
  INDEX excused_by (excused_by),
  INDEX leave_id (leave_id),
  INDEX night_of (night_of),
  INDEX status (status),
  INDEX student_id (student_id)
)";

        [Alias("checks")]
        public class Check
        {
            [AutoIncrement]
            [PrimaryKey]
            public long check_id { get; set; }
            public long room_state_id { get; set; }
            public DateTime? entry_stamp { get; set; }
            public int student_id { get; set; }
            public string reason { get; set; }
            public string comments { get; set; }
            public long check_type_id { get; set; }
            public DateTime? check_dt { get; set; }
            public byte grace { get; set; }
            public DateTime curfew_dt { get; set; }
            public int excused_by { get; set; }
            public string status { get; set; }
            public int points { get; set; }
            public string check_class { get; set; }
            public int leave_id { get; set; }
            public DateTime night_of { get; set; }
            public DateTime violation_dt { get; set; }
            public string excused_reason { get; set; }
            public DateTime? excused_dt { get; set; }
            public bool imported { get; set; } //tinyint(1) returned as `bool`
        }

        [Test]
        public void Does_map_to_checks_table()
        {
            LogManager.LogFactory = new ConsoleLogFactory();

            using (var db = OpenDbConnection())
            {
                db.DropTable<Check>();

                db.ExecuteSql(CreateChecksTableSql);

                db.Insert(new Check
                {
                    room_state_id = 1,
                    student_id = 2,
                    reason = "reason",
                    grace = 1,
                    imported = false,
                    status = "s",
                    check_class = "ch",
                    excused_reason = "excused_reason",
                });

                db.Select<Check>().PrintDump();
            }
        }
    }
}