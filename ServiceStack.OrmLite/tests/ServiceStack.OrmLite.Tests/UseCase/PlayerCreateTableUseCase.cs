using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.UseCase
{
    public class Player
    {
        public int Id { get; set; }                     // 'Id' is PrimaryKey by convention

        [Required]
        public string FirstName { get; set; }           // Creates NOT NULL Column

        [Alias("Surname")]                              // Maps to [Surname] RDBMS column
        public string LastName { get; set; }

        [Index(Unique = true)]                          // Creates Unique Index
        public string Email { get; set; }

        public List<Phone> PhoneNumbers { get; set; }   // Complex Types blobbed by default

        [Reference]
        public List<GameItem> GameItems { get; set; }   // 1:M Reference Type saved separately

        [Reference]
        public Profile Profile { get; set; }            // 1:1 Reference Type saved separately
        public int ProfileId { get; set; }              // 1:1 Self Ref Id on Parent Table

        [ForeignKey(typeof(Level), OnDelete="CASCADE")] // Creates ON DELETE CASCADE Constraint
        public Guid SavedLevelId { get; set; }          // Creates Foreign Key Reference

        public ulong RowVersion { get; set; }           // Optimistic Concurrency Updates
    }

    public class Phone                                  // Blobbed Type only
    {
        public PhoneKind Kind { get; set; }

        public string Number { get; set; }

        public string Ext { get; set; }
    }

    public enum PhoneKind
    {
        Home,
        Mobile,
        Work,
    }

    [Alias("PlayerProfile")]                            // Maps to [PlayerProfile] RDBMS Table
    [CompositeIndex(nameof(Username), nameof(Region))]  // Creates Composite Index
    public class Profile
    {
        [AutoIncrement]                                 // Auto Insert Id assigned by RDBMS
        public int Id { get; set; }

        public PlayerRole Role { get; set; }            // Native support for Enums

        public Region Region { get; set; }

        public string Username { get; set; }

        public long HighScore { get; set; }

        [Default(1)]                                    // Created in RDBMS with DEFAULT (1)
        public long GamesPlayed { get; set; }

        [CheckConstraint("Energy BETWEEN 0 AND 100")]   // Creates RDBMS Check Constraint
        public short Energy { get; set; }

        public string ProfileUrl { get; set; }

        public Dictionary<string, string> Meta { get; set; }
    }

    public enum PlayerRole                              // Enums saved as strings by default
    {
        Leader,
        Player,
        NonPlayer,
    }

    [EnumAsInt]                                         // Enum Saved as int
    public enum Region
    {
        Africa = 1,
        Americas = 2,
        Asia = 3,
        Australasia = 4,
        Europe = 5,
    }

    public class GameItem
    {
        [PrimaryKey]                                    // Specify field to use as Primary Key
        [StringLength(50)]                              // Creates VARCHAR COLUMN
        public string Name { get; set; }

        public int PlayerId { get; set; }               // Foreign Table Reference Id

        [StringLength(StringLengthAttribute.MaxText)]   // Creates "TEXT" RDBMS Column 
        public string Description { get; set; }

        [Default(OrmLiteVariables.SystemUtc)]           // Populated with UTC Date by RDBMS
        public DateTime DateAdded { get; set; }
    }

    public class Level
    {
        public Guid Id { get; set; }                    // Unique Identifer/GUID Primary Key

        public byte[] Data { get; set; }                // Saved as BLOB/Binary where possible
    }


    [TestFixtureOrmLite]
    public class PlayerCreateTableUseCase : OrmLiteProvidersTestBase
    {
        public PlayerCreateTableUseCase(DialectContext context) : base(context) {}

        [Test]
        public void Can_Create_Player_Tables_and_Save_Data()
        {
//            OrmLiteConfig.BeforeExecFilter = cmd => cmd.GetDebugString().Print();

            using (var db = OpenDbConnection())
            {
                if (db.TableExists<Level>())
                    db.DeleteAll<Level>();   // Delete ForeignKey data if exists

                //DROP and CREATE Foreign Key Tables in dependent order
                db.DropTable<Player>();
                db.DropTable<Level>();
                db.CreateTable<Level>();
                db.CreateTable<Player>();

                //DROP and CREATE tables without Foreign Keys in any order
                db.DropAndCreateTable<Profile>();
                db.DropAndCreateTable<GameItem>();

                var savedLevel = new Level
                {
                    Id = Guid.NewGuid(),
                    Data = new byte[]{ 1, 2, 3, 4, 5 },
                };
                db.Insert(savedLevel);

                var player = new Player
                {
                    Id = 1,
                    FirstName = "North",
                    LastName = "West",
                    Email = "north@west.com",
                    PhoneNumbers = new List<Phone>
                    {
                        new Phone { Kind = PhoneKind.Mobile, Number = "123-555-5555"},
                        new Phone { Kind = PhoneKind.Home,   Number = "555-555-5555", Ext = "123"},
                    },
                    GameItems = new List<GameItem>
                    {
                        new GameItem { Name = "WAND", Description = "Golden Wand of Odyssey"},
                        new GameItem { Name = "STAFF", Description = "Staff of the Magi"},
                    },
                    Profile = new Profile
                    {
                        Username = "north",
                        Role = PlayerRole.Leader,
                        Region = Region.Australasia,
                        HighScore = 100,
                        GamesPlayed = 10,
                        ProfileUrl = "https://www.gravatar.com/avatar/205e460b479e2e5b48aec07710c08d50.jpg",
                        Meta = new Dictionary<string, string>
                        {
                            {"Quote", "I am gamer"}
                        },
                    },
                    SavedLevelId = savedLevel.Id,
                };
                db.Save(player, references: true);

                var dbPlayer = db.LoadSingleById<Player>(player.Id);

                dbPlayer.PrintDump();
            }
        }
    }
}