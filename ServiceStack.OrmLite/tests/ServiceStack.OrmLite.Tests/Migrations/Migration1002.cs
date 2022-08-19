#nullable enable
using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Migrations;

[Notes("Add Player Feature")]
public class Migration1002 : MigrationBase
{
    public class Player : AuditBase
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
    public class Profile : AuditBase
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
        public Guid Id { get; set; }                    // Unique Identifier/GUID Primary Key
        public byte[] Data { get; set; }                // Saved as BLOB/Binary where possible
    }
    private string by = "admin@email.com";

    public override void Up()
    {
        //CREATE ForeignKey Tables in dependent order
        Db.CreateTable<Level>();
        Db.CreateTable<Player>();

        //CREATE tables without Foreign Keys in any order
        Db.CreateTable<Profile>();
        Db.CreateTable<GameItem>();

        var savedLevel1 = new Level
        {
            Id = Guid.NewGuid(),
            Data = new byte[]{ 1, 2, 3, 4, 5 },
        };
        var savedLevel2 = new Level
        {
            Id = Guid.NewGuid(),
            Data = new byte[]{ 6, 7, 8, 9, 10 },
        };
        Db.Insert(savedLevel1, savedLevel2);

        var player1 = new Player
        {
            Id = 1,
            FirstName = "North",
            LastName = "West",
            Email = "north@west.com",
            PhoneNumbers = new List<Phone>
            {
                new() { Kind = PhoneKind.Mobile, Number = "123-555-5555" },
                new() { Kind = PhoneKind.Home,   Number = "555-555-5555", Ext = "123" },
            },
            GameItems = new List<GameItem>
            {
                new() { Name = "WAND", Description = "Golden Wand of Odyssey"},
                new() { Name = "STAFF", Description = "Staff of the Magi"},
            },
            Profile = new Profile
            {
                Username = "north",
                Role = PlayerRole.Leader,
                Region = Region.Australasia,
                HighScore = 100,
                GamesPlayed = 10,
                ProfileUrl = "https://images.unsplash.com/photo-1463453091185-61582044d556?ixlib=rb-=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=facearea&facepad=8&w=1024&h=1024&q=80",
                Meta = new Dictionary<string, string>
                {
                    {"Quote", "I am gamer"}
                },
            }.WithAudit(by),
            SavedLevelId = savedLevel1.Id,
        }.WithAudit(by);
        Db.Save(player1, references: true);
            
        var player2 = new Player
        {
            Id = 2,
            FirstName = "South",
            LastName = "East",
            Email = "south@east.com",
            PhoneNumbers = new List<Phone>
            {
                new() { Kind = PhoneKind.Mobile, Number = "456-666-6666" },
                new() { Kind = PhoneKind.Work,   Number = "666-666-6666", Ext = "456" },
            },
            GameItems = new List<GameItem>
            {
                new() { Name = "SWORD", Description = "Sword of Damocles" },
            },
            Profile = new Profile
            {
                Username = "south",
                Role = PlayerRole.Player,
                Region = Region.Americas,
                HighScore = 50,
                GamesPlayed = 20,
                ProfileUrl = "https://images.unsplash.com/photo-1504703395950-b89145a5425b?ixlib=rb-=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=facearea&facepad=8&w=1024&h=1024&q=80",
                Meta = new Dictionary<string, string>
                {
                    {"Quote", "I am, game"}
                },
            }.WithAudit(by),
            SavedLevelId = savedLevel2.Id,
        }.WithAudit(by);
        Db.Save(player2, references: true);        
    }
    
    public override void Down()
    {
        // Clear FK Data
        Db.DeleteAll<Level>();

        // DROP ForeignKey Tables in dependent order
        Db.DropTable<Level>();
        Db.DropTable<Player>();

        // DROP tables without Foreign Keys in any order
        Db.DropTable<Profile>();
        Db.DropTable<GameItem>();
    }
}

