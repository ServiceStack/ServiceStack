using System.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp;

public static class ConfigureDbPlayers
{
    public static void SeedPlayers(this IDbConnection db, string by="admin@email.com")
    {
        if (db.TableExists<Level>())
            db.DeleteAll<Level>();  // Delete ForeignKey data if exists

        //DROP and CREATE ForeignKey Tables in dependent order
        db.DropTable<PlayerGameItem>();
        db.DropTable<Player>();
        db.DropTable<Level>();
        db.CreateTable<Level>();
        db.CreateTable<Player>();

        //DROP and CREATE tables without Foreign Keys in any order
        db.DropAndCreateTable<Profile>();
        db.DropAndCreateTable<GameItem>();
        db.CreateTable<PlayerGameItem>();

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
        db.Insert(savedLevel1, savedLevel2);

        var gameItem1 = new GameItem { Name = "WAND",  Description = "Golden Wand of Odyssey" }.WithAudit(by);
        var gameItem2 = new GameItem { Name = "STAFF", Description = "Staff of the Magi" }.WithAudit(by);
        var gameItem3 = new GameItem { Name = "SWORD", Description = "Sword of Damocles" }.WithAudit(by);
        db.Insert(gameItem1, gameItem2, gameItem3);
            
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
            GameItems = new List<PlayerGameItem>
            {
                new () { GameItemName = gameItem1.Name },
                new () { GameItemName = gameItem1.Name },
            },
            Profile = new Profile
            {
                Username = "north",
                Role = PlayerRole.Leader,
                Region = PlayerRegion.Australasia,
                HighScore = 100,
                GamesPlayed = 10,
                ProfileUrl = "https://images.unsplash.com/photo-1463453091185-61582044d556?ixlib=rb-=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=facearea&facepad=8&w=1024&h=1024&q=80",
                CoverUrl = "files/cover.docx",
                Meta = new Dictionary<string, string>
                {
                    {"Quote", "I am gamer"}
                },
            }.WithAudit(by),
            SavedLevelId = savedLevel1.Id,
        }.WithAudit(by);
        db.Save(player1, references: true);
            
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
            GameItems = new List<PlayerGameItem>
            {
                new () { GameItemName = gameItem2.Name },
                new () { GameItemName = gameItem3.Name },
            },
            Profile = new Profile
            {
                Username = "south",
                Role = PlayerRole.Player,
                Region = PlayerRegion.Americas,
                HighScore = 50,
                GamesPlayed = 20,
                ProfileUrl = "https://images.unsplash.com/photo-1504703395950-b89145a5425b?ixlib=rb-=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=facearea&facepad=8&w=1024&h=1024&q=80",
                CoverUrl = "files/profile.jpg",
                Meta = new Dictionary<string, string>
                {
                    {"Quote", "I am, game"}
                },
            }.WithAudit(by),
            SavedLevelId = savedLevel2.Id,
        }.WithAudit(by);
        db.Save(player2, references: true);
    }
}