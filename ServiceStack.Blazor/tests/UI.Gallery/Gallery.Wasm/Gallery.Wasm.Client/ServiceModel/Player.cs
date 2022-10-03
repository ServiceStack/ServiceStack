using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.ServiceModel;

[Tag("Game")]
[AutoApply(Behavior.AuditQuery)]
public class QueryPlayer : QueryDb<Player>
{
    
}

[Tag("Game")]
[AutoApply(Behavior.AuditCreate)]
public class CreatePlayer : ICreateDb<Player>, IReturn<IdResponse>
{
    [ValidateNotEmpty]
    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public List<Phone>? PhoneNumbers { get; set; }

    [ValidateNotNull]
    public int? ProfileId { get; set; }

    public Guid? SavedLevelId { get; set; }
}

[Tag("Game")]
[AutoApply(Behavior.AuditModify)]
public class UpdatePlayer : IPatchDb<Player>, IReturn<IdResponse>
{
    public int Id { get; set; }

    [ValidateNotEmpty]
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public List<Phone>? PhoneNumbers { get; set; }

    public int? ProfileId { get; set; }

    public Guid? SavedLevelId { get; set; }
}

[Tag("Game")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeletePlayer : IDeleteDb<Player>, IReturnVoid
{
    public int Id { get; set; }
}

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='M6 9h2v2h2v2H8v2H6v-2H4v-2h2V9m12.5 0a1.5 1.5 0 0 1 1.5 1.5a1.5 1.5 0 0 1-1.5 1.5a1.5 1.5 0 0 1-1.5-1.5A1.5 1.5 0 0 1 18.5 9m-3 3a1.5 1.5 0 0 1 1.5 1.5a1.5 1.5 0 0 1-1.5 1.5a1.5 1.5 0 0 1-1.5-1.5a1.5 1.5 0 0 1 1.5-1.5M17 5a7 7 0 0 1 7 7a7 7 0 0 1-7 7c-1.96 0-3.73-.8-5-2.1A6.96 6.96 0 0 1 7 19a7 7 0 0 1-7-7a7 7 0 0 1 7-7h10M7 7a5 5 0 0 0-5 5a5 5 0 0 0 5 5c1.64 0 3.09-.79 4-2h2c.91 1.21 2.36 2 4 2a5 5 0 0 0 5-5a5 5 0 0 0-5-5H7Z'/></svg>")]
public class Player : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }                     // 'Id' is PrimaryKey by convention

    [Required]
    public string FirstName { get; set; }           // Creates NOT NULL Column

    [Alias("Surname")]                              // Maps to [Surname] RDBMS column
    public string LastName { get; set; }

    [Format(FormatMethods.LinkEmail)]
    [Index(Unique = true)]                          // Creates Unique Index
    public string Email { get; set; }

    public List<Phone> PhoneNumbers { get; set; }   // Complex Types blobbed by default

    [Reference]
    public List<PlayerGameItem> GameItems { get; set; }   // 1:M Reference Type saved separately

    [Reference]
    [Format(FormatMethods.Hidden)]
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

[Tag("Game")]
[AutoApply(Behavior.AuditQuery)]
public class QueryProfile : QueryDb<Profile> {}

[Tag("Game")]
[AutoApply(Behavior.AuditCreate)]
public class CreateProfile : ICreateDb<Profile>, IReturn<IdResponse>
{
    public PlayerRole Role { get; set; }            // Native support for Enums
    public PlayerRegion Region { get; set; }
    [ValidateNotEmpty] 
    public string Username { get; set; } = string.Empty;
    public long HighScore { get; set; }

    public long GamesPlayed { get; set; }

    [ValidateInclusiveBetween(0,100)]
    public int Energy { get; set; }

    [Input(Type = "file"), UploadTo("profiles")]
    public string? ProfileUrl { get; set; }
    
    [Input(Type = "file"), UploadTo("files")]
    public string? CoverUrl { get; set; }
}


[Tag("Game")]
[AutoApply(Behavior.AuditModify)]
public class UpdateProfile : IPatchDb<Profile>, IReturn<IdResponse>
{
    public int Id { get; set; }
    public PlayerRole? Role { get; set; }
    public PlayerRegion? Region { get; set; }
    public string? Username { get; set; }
    public long? HighScore { get; set; }

    public long? GamesPlayed { get; set; }

    [ValidateInclusiveBetween(0,100)]
    public int? Energy { get; set; }

    [Input(Type = "file"), UploadTo("profiles")]
    public string? ProfileUrl { get; set; }
    
    [Input(Type = "file"), UploadTo("files")]
    public string? CoverUrl { get; set; }
}

[Tag("Game")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteProfile : IDeleteDb<Profile>, IReturnVoid
{
    public int Id { get; set; }
}

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' aria-hidden='true' role='img' width='1em' height='1em' preserveAspectRatio='xMidYMid meet' viewBox='0 0 16 16'><path fill='currentColor' d='M13.5 0h-12C.675 0 0 .675 0 1.5v13c0 .825.675 1.5 1.5 1.5h12c.825 0 1.5-.675 1.5-1.5v-13c0-.825-.675-1.5-1.5-1.5zM13 14H2V2h11v12zM4 9h7v1H4zm0 2h7v1H4zm1-6.5a1.5 1.5 0 1 1 3.001.001A1.5 1.5 0 0 1 5 4.5zM7.5 6h-2C4.675 6 4 6.45 4 7v1h5V7c0-.55-.675-1-1.5-1z'/></svg>")]
[CompositeIndex(nameof(Username), nameof(Region))]  // Creates Composite Index
public class Profile : AuditBase
{
    [AutoIncrement]                                 // Auto Insert Id assigned by RDBMS
    public int Id { get; set; }

    public PlayerRole Role { get; set; }            // Native support for Enums
    public PlayerRegion Region { get; set; }
    public string? Username { get; set; }
    public long HighScore { get; set; }

    [Default(1)]                                    // Created in RDBMS with DEFAULT (1)
    public long GamesPlayed { get; set; }

    [CheckConstraint("Energy BETWEEN 0 AND 100")]   // Creates RDBMS Check Constraint
    public int Energy { get; set; }

    [Format(FormatMethods.Icon)]
    public string? ProfileUrl { get; set; }

    [Format(FormatMethods.Attachment)]
    public string? CoverUrl { get; set; }
    
    public Dictionary<string, string>? Meta { get; set; }
}

public enum PlayerRole                              // Enums saved as strings by default
{
    Leader,
    Player,
    NonPlayer,
}

[EnumAsInt]                                         // Enum Saved as int
public enum PlayerRegion
{
    Africa = 1,
    Americas = 2,
    Asia = 3,
    Australasia = 4,
    Europe = 5,
}


[Tag("Game")]
[AutoApply(Behavior.AuditQuery)]
public class QueryGameItem : QueryDb<GameItem>
{
    public string Name { get; set; }
}

[Tag("Game")]
[AutoApply(Behavior.AuditCreate)]
public class CreateGameItem : ICreateDb<GameItem>, IReturn<IdResponse>
{
    [ValidateNotEmpty]
    public string Name { get; set; } = string.Empty;

    [ValidateNotEmpty]
    public string Description { get; set; } = string.Empty;
    
    [ValidateNotEmpty]
    [Input(Type = "file"), UploadTo("game_items")]
    public string ImageUrl { get; set; }
}

[Tag("Game")]
[AutoApply(Behavior.AuditModify)]
public class UpdateGameItem : IPatchDb<GameItem>, IReturn<IdResponse>
{
    [ValidateNotEmpty]
    public string Name { get; set; } = string.Empty;

    [ValidateNotEmpty]
    public string? Description { get; set; }
    
    [Input(Type = "file"), UploadTo("game_items")]
    public string? ImageUrl { get; set; }
}

[Tag("Game")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteGameItem : IDeleteDb<GameItem>, IReturnVoid
{
    [ValidateNotEmpty]
    public string Name { get; set; } = string.Empty;
}

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='m6.816 15.126l4.703 2.715v-5.433L6.814 9.695v5.432zm-2.025 1.168l6.73 3.882v3.82l-10.04-5.79V6.616l3.31 1.91v7.769zM12 6.145L7.298 8.863L12 11.579l4.704-2.717L12 6.146zm0-2.332l5.659 3.274l3.31-1.91L12 0L1.975 5.79L5.28 7.695zm7.207 12.48v-3.947l-2.023 1.167v1.614l-4.703 2.715v.005v-5.436L22.518 6.62v11.587L12.48 24v-3.817l6.727-3.887z'/></svg>")]
public class GameItem : AuditBase
{
    [PrimaryKey]                                    // Specify field to use as Primary Key
    [StringLength(50)]                              // Creates VARCHAR COLUMN
    public string Name { get; set; }
    
    [Format(FormatMethods.IconRounded)]
    public string ImageUrl { get; set; }

    [StringLength(StringLengthAttribute.MaxText)]   // Creates "TEXT" RDBMS Column 
    public string? Description { get; set; }

    [Default(OrmLiteVariables.SystemUtc)]           // Populated with UTC Date by RDBMS
    public DateTime DateAdded { get; set; }
}

[Tag("Game")]
[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 36 36'><path fill='currentColor' d='M17 12h-2.85a6.25 6.25 0 0 0-6.21 5H2v2h5.93a6.22 6.22 0 0 0 6.22 5H17Z' class='clr-i-solid clr-i-solid-path-1'/><path fill='currentColor' d='M28.23 17A6.25 6.25 0 0 0 22 12h-3v12h3a6.22 6.22 0 0 0 6.22-5H34v-2Z' class='clr-i-solid clr-i-solid-path-2'/><path fill='none' d='M0 0h36v36H0z'/></svg>")]
public class QueryPlayerGameItem : QueryDb<PlayerGameItem>
{
    public int? Id { get; set; }
    public int? PlayerId { get; set; }
    public string? GameItemName { get; set; }
}

[Tag("Game")]
public class PlayerGameItem
{
    [AutoIncrement]
    public int Id { get; set; }
    [References(typeof(Player))]
    public int PlayerId { get; set; }               // Foreign Table Reference Id
    [References(typeof(GameItem))]
    public string GameItemName { get; set; }
}

[Tag("Game")]
[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><path fill='currentColor' d='M18 5H2C.9 5 0 5.9 0 7v6c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm0 8H2V7h16v6zM7 8H3v4h4V8zm5 0H8v4h4V8zm5 0h-4v4h4V8z'/></svg>")]
public class QueryLevel : QueryDb<Level>
{
    public Guid? Id { get; set; }                    // Unique Identifier/GUID Primary Key
}

public class Level
{
    public Guid Id { get; set; }                    // Unique Identifier/GUID Primary Key
    public byte[] Data { get; set; } = Array.Empty<byte>(); // Saved as BLOB/Binary where possible
}
