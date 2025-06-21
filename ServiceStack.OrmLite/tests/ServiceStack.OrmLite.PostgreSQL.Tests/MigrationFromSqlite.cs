#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL.Tests;

[Explicit("Integration Test")]
public class MigrationFromSqlite
{
    private static OrmLiteConnectionFactory ConfigureDb()
    {
        var dialect = PostgreSqlConfiguration.Configure(PostgreSqlDialect.Create());
        var dbFactory = new OrmLiteConnectionFactory(Environment.GetEnvironmentVariable("PGSQL_CONNECTION"), dialect);

        var sqliteDialect = SqliteConfiguration.Configure(SqliteDialect.Create());
        dbFactory.RegisterConnection("app.db", "DataSource=/home/mythz/src/ServiceStack/ComfyGateway/App_Data/app.db;Cache=Shared", sqliteDialect);
        return dbFactory;
    }

    [Test]
    public void Can_Reset_API_Keys()
    {
        var dbFactory = ConfigureDb();
        using var db = dbFactory.Open();
        using var dbSqlite = dbFactory.Open("app.db");
        
        BulkInsertConfig? config = new BulkInsertConfig { Mode = BulkInsertMode.Sql };
        // config = null;
        
        db.DeleteAll<ApiKey>();
        db.ResetSequence<ApiKey>(x => x.Id);
        db.BulkInsert(dbSqlite.Select<ApiKey>(), config);
    }

    [Test]
    public void Can_BulkInsert_DeletedRows()
    {
        var dbFactory = ConfigureDb();
        using var db = dbFactory.Open();
        
        int[] artifactIds = [
            5374, 5373, 5372, 5371, 5370, 5368, 5367, 5366, 5365, 5364, 5363, 
            5361, 5360, 5359, 5358, 5357, 5356, 5355, 5354, 5353, 5352, 5351,
        ];
        
        db.DeleteAll<DeletedRow>();
        db.ResetSequence<DeletedRow>(x => x.Id);
        db.BulkInsert(artifactIds.Select(x => new DeletedRow { Table = Table.Artifact, Key = $"{x}" }));
        
        var rows = db.Select<DeletedRow>();
        Assert.That(rows.Count, Is.EqualTo(artifactIds.Length));

        db.DeleteAll<DeletedRow>();
        db.ResetSequence<DeletedRow>(x => x.Id);
        db.BulkInsert(artifactIds.Select(x => new DeletedRow { Table = Table.Artifact, Key = $"{x}" }), 
            new() { Mode = BulkInsertMode.Sql});
        
        rows = db.Select<DeletedRow>();
        Assert.That(rows.Count, Is.EqualTo(artifactIds.Length));
    }
    
    [Test]
    public void Migrate_from_SQLite()
    {
        var dbFactory = ConfigureDb();
        using var db = dbFactory.Open();
        using var dbSqlite = dbFactory.Open("app.db");
        
        BulkInsertConfig? config = new BulkInsertConfig { Mode = BulkInsertMode.Sql };
        config = null;

        // db.DeleteAll<Workflow>();
        // db.ResetSequence<Workflow>(x => x.Id);
        // db.Insert(dbSqlite.Select<Workflow>(x => x.Id == 1).First());
        
        // db.Delete<ApiKeysFeature.ApiKey>();
        // db.BulkInsert(dbSqlite.Select<ApiKeysFeature.ApiKey>());
        db.DeleteAll<ArtifactReaction>();
        db.ResetSequence<ArtifactReaction>(x => x.Id);
        db.DeleteAll<ArtifactCategory>();
        db.ResetSequence<ArtifactCategory>(x => x.Id);
        db.DeleteAll<ArtifactTag>();
        db.ResetSequence<ArtifactTag>(x => x.Id);
        db.DeleteAll<Artifact>();
        db.ResetSequence<Artifact>(x => x.Id);
        db.DeleteAll<Tag>();
        db.ResetSequence<Tag>(x => x.Id);
        db.DeleteAll<Category>();
        db.ResetSequence<Category>(x => x.Id);
        db.DeleteAll<CommentReport>();
        db.ResetSequence<CommentReport>(x => x.Id);
        db.DeleteAll<CommentVote>();
        db.ResetSequence<CommentVote>(x => x.Id);
        db.DeleteAll<Comment>();
        db.ResetSequence<Comment>(x => x.Id);
        db.DeleteAll<HiddenArtifact>();
        db.ResetSequence<HiddenArtifact>(x => x.Id);
        db.DeleteAll<ModerationQueue>();
        db.ResetSequence<ModerationQueue>(x => x.Id);
        db.DeleteAll<ThreadVote>();
        db.ResetSequence<ThreadVote>(x => x.Id);
        db.DeleteAll<Thread>();
        db.ResetSequence<Thread>(x => x.Id);
        db.DeleteAll<WorkflowGeneration>();
        db.ResetSequence<WorkflowGeneration>(x => x.Id);
        db.DeleteAll<WorkflowVersion>();
        db.ResetSequence<WorkflowVersion>(x => x.Id);
        db.DeleteAll<Workflow>();
        db.ResetSequence<Workflow>(x => x.Id);

        var artifacts = dbSqlite.Select<Artifact>().OrderBy(x => x.Id);
        db.BulkInsert(artifacts, config);
        db.BulkInsert(dbSqlite.Select<ArtifactCategory>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<ArtifactReaction>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<ArtifactTag>().OrderBy(x => x.Id), config);
        // db.BulkInsert(dbSqlite.Select<Asset>());
        db.BulkInsert(dbSqlite.Select<Category>().OrderBy(x => x.Id), config);
        // db.BulkInsert(dbSqlite.Select<ComfyAgent>());
        db.BulkInsert(dbSqlite.Select<Comment>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<CommentReport>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<CommentVote>().OrderBy(x => x.Id), config);
        // db.BulkInsert(dbSqlite.Select<DeletedRow>());
        db.BulkInsert(dbSqlite.Select<HiddenArtifact>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<ModerationQueue>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<Tag>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<Thread>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<ThreadVote>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<Workflow>().OrderBy(x => x.Id), config);
        db.BulkInsert(dbSqlite.Select<WorkflowGeneration>().OrderBy(x => x.CreatedDate), config);
        db.BulkInsert(dbSqlite.Select<WorkflowVersion>().OrderBy(x => x.Id), config);
    }
}

public class ApiKey : IApiKey
{
    [AutoIncrement]
    public int Id { get; set; }
        
    /// <summary>
    /// The API Key
    /// </summary>
    [Index(Unique = true)]
    public string Key { get; set; }

    /// <summary>
    /// Name for the API Key
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// User Primary Key
    /// </summary>
    public string? UserId { get; set; }
    [DataAnnotations.Ignore]
    public string? UserAuthId => UserId;

    /// <summary>
    /// Name of the User or Worker using the API Key
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// What to show the User after they've created the API Key
    /// </summary>
    public string VisibleKey { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public DateTime? CancelledDate { get; set; }

    public DateTime? LastUsedDate { get; set; }

    public List<string> Scopes { get; set; } = [];

    public List<string> Features { get; set; } = [];

    /// <summary>
    /// Restricted to only access specific APIs
    /// </summary>
    public List<string> RestrictTo { get; set; } = [];

    public string? Environment { get; set; }

    public string? Notes { get; set; }

    //Custom Reference Data
    public int? RefId { get; set; }
    public string? RefIdStr { get; set; }
        
    public bool HasScope(string scope) => Scopes.Contains(scope);
    public bool HasFeature(string feature) => Features.Contains(feature);
    public bool CanAccess(Type requestType) => RestrictTo.IsEmpty() || RestrictTo.Contains(requestType.Name);

    public Dictionary<string, string>? Meta { get; set; }
}

public class ComfyAgent
{
    [AutoIncrement] public int Id { get; set; }
    [Index]
    public string DeviceId { get; set; }
    public int Version { get; set; }
    [Index]
    public string UserId { get; set; }
    public string? UserName { get; set; }
    public string ApiKey { get; set; }
    public List<string>? Gpus { get; set; }
    public List<string> Workflows { get; set; }
    public List<string> Nodes { get; set; }
    public List<string> Checkpoints { get; set; }
    public List<string> Unets { get; set; }
    public List<string> Vaes { get; set; }
    public List<string> Loras { get; set; }
    public List<string> Clips { get; set; }
    public List<string> ClipVisions { get; set; }
    public List<string> Upscalers { get; set; }
    public List<string> ControlNets { get; set; }
    public List<string> Embeddings { get; set; }
    public List<string> Stylers { get; set; }
    public List<string> Gligens { get; set; }
    public List<string> PhotoMakers { get; set; }
    public bool Enabled { get; set; }
    public DateTime? OfflineDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? LastIp { get; set; }
    
    public int Credits { get; set; }
    public int WorkflowsExecuted { get; set; }
    public int ImagesGenerated { get; set; }
    public int AudiosGenerated { get; set; }
    public int VideosGenerated { get; set; }
    public int TextsGenerated { get; set; }
    
    public int QueueCount { get; set; }
    public List<string>? LanguageModels { get; set; }
    
    [PgSqlJsonB]
    public List<string>? RequirePip { get; set; }
    [PgSqlJsonB]
    public List<string>? RequireNodes { get; set; }
    [PgSqlJsonB]
    public List<string>? RequireModels { get; set; }
}

public class DeletedRow
{
    [AutoIncrement]
    public int Id { get; set; }
    public Table Table { get; set; }
    public string Key { get; set; }
}

[Flags]
public enum Table
{
    Artifact = 1,
    ArtifactTag = 2,
    ArtifactCategory = 3,
    ArtifactReaction = 4,
    HiddenArtifact = 5,
    Thread = 6,
    Comment = 7,
    Workflow = 8,
    WorkflowGeneration = 9,
    WorkflowVersion = 10,
}

//jq -r 'to_entries[] | .value.input.required // {} | to_entries[] | .value[0] | if type == "array" then "ENUM" else . end' files/object_info.json | sort | uniq
public enum ComfyInputType
{
    Unknown,
    Audio,
    Boolean,
    Clip,
    ClipVision,
    ClipVisionOutput,
    Combo,
    Conditioning,
    ControlNet,
    Enum,
    FasterWhisperModel,
    Filepath,
    Fl2Model,
    Float,
    Floats,
    Gligen,
    Guider,
    Hooks,
    Image,
    Int,
    Latent,
    LatentOperation,
    Load3D,
    Load3DAnimation,
    Mask,
    Mesh,
    Model,
    Noise,
    Photomaker,
    Sampler,
    Sigmas,
    String,
    StyleModel,
    Subtitle,
    TranscriptionPipeline,
    Transcriptions,
    UpscaleModel,
    VAE,
    VHSAudio,
    Voxel,
    WavBytes,
    WavBytesBatch,
    Webcam,
}

public class ComfyInputDefinition
{
    public string ClassType { get; set; }
    public int NodeId { get; set; }
    public int ValueIndex { get; set; }
    public string Name { get; set; }
    public string Label { get; set; }
    public ComfyInputType Type { get; set; }
    public string? Tooltip { get; set; }
    public object? Default { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public decimal? Step { get; set; }
    public decimal? Round { get; set; }
    public bool? Multiline { get; set; }
    public bool? DynamicPrompts { get; set; }
    public bool? ControlAfterGenerate { get; set; }
    public string[]? EnumValues { get; set; }
    public Dictionary<string, object>? ComboValues { get; set; }
}

public class Workflow : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Category { get; set; }
    public string Base { get; set; }
    [Unique]
    public string Name { get; set; }
    [Index(Unique = true)]
    public string Slug { get; set; }
    [Unique]
    public string Path { get; set; }
    public string Description { get; set; } // Markdown
    public int? PinVersionId { get; set; }
}

public class WorkflowVersion : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    [ForeignKey(typeof(Workflow))]
    public int ParentId { get; set; } //ComfyWorkflow.Id
    public string Version { get; set; }  //v1
    public Dictionary<string,object?> Workflow { get; set; }
    public WorkflowInfo Info { get; set; }
    public List<string> Nodes { get; set; }
    public List<string> Assets { get; set; }
}

public class WorkflowInfo
{
    [References(typeof(WorkflowVersion))]
    public int Id { get; set; }
    [References(typeof(Workflow))]
    public int ParentId { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public ComfyWorkflowType Type { get; set; }
    public ComfyPrimarySource Input { get; set; }
    public ComfyPrimarySource Output { get; set; }
    public List<ComfyInputDefinition> Inputs { get; set; } = [];
}

public enum ComfyWorkflowType
{
    TextToImage,
    ImageToImage,
    ImageToText,
    TextToAudio,
    TextToVideo,
    TextTo3D,
    AudioToText,
    VideoToText,
    ImageToVideo,
}

public enum ComfyPrimarySource
{
    Text,
    Image,
    Video,
    Audio,
}

[DataContract]
public class ApiNode
{
    [DataMember(Name="inputs")]
    public Dictionary<string, object> Inputs { get; set; } = new(); // InputName -> value or [source_node_id, output_index]
    [DataMember(Name="class_type")] public string ClassType { get; set; } = "";
}

[DataContract]
public class ApiPrompt
{
    // Key is the workflow node ID (string)
    [DataMember(Name="prompt")]
    public Dictionary<string, ApiNode> Prompt { get; set; } = new();

    // Other optional properties like extra_data, client_id can be added here
    [DataMember(Name="extra_data")]
    public Dictionary<string, object?>? ExtraData { get; set; }

    [DataMember(Name="client_id")]
    public string? ClientId { get; set; }
}

public class WorkflowGeneration : AuditBase
{
    public string Id { get; set; } // ClientId
    public string? UserId { get; set; }
    public int? ThreadId { get; set; } 
    public int WorkflowId { get; set; }
    public int? VersionId { get; set; }
    public AssetType? Output { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// Defines the base model's weights and architecture.
    /// The base model checkpoint file that determines the core capabilities and style
    /// of image generation. It provides the foundation AI model (like SD 1.5, SDXL, etc.)
    /// </summary>
    public string? Checkpoint { get; set; }
    /// <summary>
    /// LORAs adds specialized capabilities or styles on top of the checkpoint.
    /// A Low-Rank Adaptation file that fine-tunes the base model for specific styles,
    /// subjects, or concepts without changing the entire model
    /// </summary>
    public string? Lora { get; set; }
    /// <summary>
    /// Embeddings enable consistent generation of specific concepts or elements.
    /// A Textual Inversion embedding that teaches the model specific concepts or subjects
    /// using a small set of vectors referenced by a trigger word in prompts
    /// </summary>
    public string? Embedding { get; set; }
    /// <summary>
    /// VAEs converts between the model's latent representation and the actual image.
    /// The Variational Autoencoder component responsible for encoding/decoding between 
    /// latent space and pixel space. It determines the final image quality and detail.
    /// </summary>
    public string? Vae { get; set; }
    /// <summary>
    /// Control Nets provide precise control over composition, structure, and layout of generated images.
    /// They're a specialized model that allows for conditional control over the image generation process
    /// by accepting additional inputs like depth maps, edge detection, pose estimation, etc.
    /// </summary>
    public string? ControlNet { get; set; }
    /// <summary>
    /// Upscalers improves final image quality by intelligently scaling low-resolution outputs to higher resolutions.
    /// Upscale models are specifically designed to increase the resolution of generated images
    /// while preserving or enhancing details and reducing artifacts
    /// </summary>
    public string? Upscaler { get; set; }
    /// <summary>
    /// Image preview to use for this generation
    /// </summary>
    public string? PosterImage { get; set; }
    public Dictionary<string,object?>? Args { get; set; }
    [IgnoreDataMember]
    public Dictionary<string,object?> Workflow { get; set; }
    [IgnoreDataMember]
    public ApiPrompt ApiPrompt { get; set; }
    public HashSet<string> RequiredNodes { get; set; }
    public HashSet<string> RequiredAssets { get; set; }
    public string? DeviceId { get; set; }
    public string? PromptId { get; set; }
    [IgnoreDataMember]
    public Dictionary<string,object?>? Status { get; set; }
    [IgnoreDataMember]
    public Dictionary<string,object?>? Outputs { get; set; }
    public WorkflowResult? Result { get; set; }
    public ResponseStatus? Error { get; set; }
    public int Credits { get; set; }
    public string? StatusUpdate { get; set; }
    public string? PublishedBy { get; set; }
    [Index]
    public DateTime? PublishedDate { get; set; }
}

public class WorkflowResult
{
    public string? ClientId { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<ComfyTextOutput>? Texts { get; set; }
    public List<ComfyAssetOutput>? Assets { get; set; }
}
public class ComfyTextOutput
{
    public string NodeId { get; set; }
    public string? Text { get; set; }
}

public interface IAssetMetadata
{
    public Ratings? Ratings { get; set; }
    public Dictionary<string, double>? Tags { get; set; }
    public Dictionary<string, double>? Categories { get; set; }
    public List<ObjectDetection>? Objects { get; set; }
}

public class ComfyAssetOutput : IAssetMetadata
{
    public string NodeId { get; set; }
    public string Url { get; set; }
    public AssetType Type { get; set; }
    public string FileName { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Length { get; set; }
    public Rating? Rating { get; set; }
    public Ratings? Ratings { get; set; }
    public Dictionary<string, double>? Tags { get; set; }
    public Dictionary<string, double>? Categories { get; set; }
    public List<ObjectDetection>? Objects { get; set; }
    public string? Phash { get; set; }
    public string? Color { get; set; }
}

public class Artifact : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string GenerationId { get; set; }
    public AssetType Type { get; set; }
    [IgnoreDataMember] // Not important
    public string FileName { get; set; }
    public string Url { get; set; }
    public long Length { get; set; }
    [IgnoreDataMember] // In URL
    public string? Hash { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? VersionId { get; set; }
    public int? WorkflowId { get; set; }
    public int? ThreadId { get; set; }
    public int? Credits { get; set; }
    [Index]
    public Rating? Rating { get; set; }
    public Ratings? Ratings { get; set; }
    // Tag => Score
    public Dictionary<string,double>? Tags { get; set; }
    // Category => Score
    public Dictionary<string,double>? Categories { get; set; }
    // CodePoint => Count
    public Dictionary<long, long> Reactions { get; set; } = new();
    [IgnoreDataMember] // In URL
    public List<ObjectDetection>? Objects { get; set; }
    public string? Phash { get; set; }
    public string? Color { get; set; }
    public string? Caption { get; set; }
    public string? Description { get; set; }
    public string? PublishedBy { get; set; }
    [Index]
    public DateTime? PublishedDate { get; set; }
}

public class Ratings
{
    [DataMember(Name="predicted_rating")]
    public string PredictedRating { get; set; }
    public double Confidence { get; set; }
    [DataMember(Name="all_scores")]
    public Dictionary<string, double> AllScores { get; set; }
}

public class ObjectDetection
{
    public string? Model { get; set; }
    public string Class { get; set; }
    public double Score { get; set; }
    public int[] Box { get; set; }
}

public enum AssetType
{
    Image,
    Video,
    Audio,
    Animation,
    Text,
    Binary,
}

//[Flags] store as strings but allow bitwise operations
public enum Rating
{
    PG    = 1 << 0,
    PG13  = 1 << 1,
    M     = 1 << 2,
    R     = 1 << 3,
    X     = 1 << 4,
    XXX   = 1 << 5,
}

public class ModerationQueue : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public int ArtifactId { get; set; }
    public Rating? Rating { get; set; }
    public bool? Hide { get; set; }
    public int? PoorQuality { get; set; }
    public ReportType? ReportType { get; set; }
    public ReportTag? ReportTag { get; set; }
    public string? ReportComment { get; set; }
    public ModerationDecision? Decision { get; set; }
    public string? Notes { get; set; }
}

public class HiddenArtifact
{
    [AutoIncrement]
    public int Id { get; set; }
    public int ArtifactId { get; set; }
    public string? UserId { get; set; }
    public string? Reason { get; set; }
    public DateTime? CreatedDate { get; set; }
}
public class ArtifactTag
{
    [AutoIncrement]
    public int Id { get; set; }
    [Index]
    public int TagId { get; set; }
    public int ArtifactId { get; set; }
    public int Score { get; set; }
    public string? UserId { get; set; }
}

public class Tag
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }

    [IgnoreDataMember]
    public DateTime CreatedDate { get; set; }
    [IgnoreDataMember]
    public string? CreatedBy { get; set; }
}

public class Category
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
}
public class ArtifactCategory
{
    [AutoIncrement]
    public int Id { get; set; }
    [Index]
    public int CategoryId { get; set; }
    public int ArtifactId { get; set; }
    public int Score { get; set; }
}

public class ArtifactReaction
{
    [AutoIncrement]
    public int Id { get; set; }
    public int ArtifactId { get; set; }
    public int CodePoint { get; set; }
    public string UserId { get; set; }
}

public enum ReportType
{
    NeedsReview,
    MatureContent,
    TOSViolation,
}

public enum ReportTag
{
    //AdultContent
    Nudity,
    ExplicitNudity,
    SexualActs,
    AdultProducts,
    
    //SuggestiveContent
    Underwear,
    Swimwear,
    PartialNudity,
    SexyAttire,
    SexualThemes,
    
    //Violence
    IntenseGore,
    GraphicViolence,
    WeaponRelatedViolence,
    SelfHarm,
    Death,
    
    //Disturbing
    EmaciatedFigures,
    DeceasedBodies,
    Hanging,
    Explosions,
    VisuallyDisturbing,
    OffensiveGestures,
    
    //Hate
    HateSymbols,
    NaziRelatedContent,
    RacistContent,
    ReligiousHate,
    HomophobicContent,
    TransphobicContent,
    SexistContent,
    ExtremistContent,
    
    //TOSViolations
    DepictionOfRealPersonContent,
    FalseImpersonation,
    IllegalContent,
    DepictionOfMinor,
    ChildAbuse,
    Spam,
    ProhibitedPrompts,
    
    //NeedsModeratorReview
    PotentialSecurityConcern,
    ContentShouldBeReviewed,
    IncorrectOrMisleadingContent,
    OtherConcern,
}

[AutoPopulate(nameof(ExternalRef), Eval = "nguid")]
public class Thread : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    [Index(Unique = true)]
    public string Url { get; set; }
    public string Description { get; set; }
    public string? ExternalRef { get; set; }
    [Default(0)]
    public int ViewCount { get; set; }
    [Default(1)]
    public long LikesCount { get; set; }
    [Default(0)]
    public long CommentsCount { get; set; }
    public Dictionary<string,object?>? Args { get; set; }
    public long? RefId { get; set; }
    public string RefIdStr { get; set; }
    public DateTime? ClosedDate { get; set; }
}

public class Comment : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public int? ReplyId { get; set; }
    public string Content { get; set; }
    [Default(0)]
    public int UpVotes { get; set; }
    [Default(0)]
    public int DownVotes { get; set; }
    public int Votes { get; set; }
    public string? FlagReason { get; set; }
    public string? Notes { get; set; }
    [References(typeof(AppUser))]
    public string UserId { get; set; }
}

[UniqueConstraint(nameof(ThreadId), nameof(UserId), nameof(Code))]
public class ThreadVote
{
    [AutoIncrement]
    public long Id { get; set; }

    [References(typeof(Thread))]
    public int ThreadId { get; set; }
    [References(typeof(AppUser))]
    public string UserId { get; set; }
    public int Vote { get; set; } // -1 / 1
    /// <summary>
    /// CodePoints for emojis
    /// ['👍','❤','😂','😢'].map(e => e.codePointAt(0)) == [128077, 10084, 128514, 128546]
    /// [128077, 10084, 128514, 128546].map(i => String.fromCodePoint(i))
    /// </summary>
    public long Code { get; set; }
    public DateTime CreatedDate { get; set; }
}

[UniqueConstraint(nameof(CommentId), nameof(UserId), nameof(Code))]
public class CommentVote
{
    [AutoIncrement]
    public long Id { get; set; }

    [Ref(None = true)]
    [References(typeof(Comment))]
    public int CommentId { get; set; }
    [References(typeof(AppUser))]
    public string UserId { get; set; }
    public int Vote { get; set; } // -1 / 1
    /// <summary>
    /// CodePoints for emojis
    /// ['👍','❤','😂','😢'].map(e => e.codePointAt(0)) == [128077, 10084, 128514, 128546]
    /// [128077, 10084, 128514, 128546].map(i => String.fromCodePoint(i))
    /// </summary>
    public long Code { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CommentReport
{
    [AutoIncrement]
    public long Id { get; set; }

    [References(typeof(Comment))]
    public int CommentId { get; set; }
    
    [Reference]
    public Comment Comment { get; set; }
    
    [References(typeof(AppUser))]
    public string UserId { get; set; }
    
    public PostReport PostReport { get; set; }
    public string Description { get; set; }

    public DateTime CreatedDate { get; set; }
    public ModerationDecision Moderation { get; set; }
    public string? Notes { get; set; }
}

public enum PostReport
{
    Offensive,
    Spam,
    Nudity,
    Illegal,
    Other,
}

public enum ModerationDecision
{
    [DataAnnotations.Description("Ignore")]
    None,
    [DataAnnotations.Description("Approve")]
    Approve,
    [DataAnnotations.Description("Deny")]
    Deny,
    [DataAnnotations.Description("Flag")]
    Flag,
    [DataAnnotations.Description("Delete")]
    Delete,
    [DataAnnotations.Description("Ban User for a day")]
    Ban1Day,
    [DataAnnotations.Description("Ban User for a week")]
    Ban1Week,
    [DataAnnotations.Description("Ban User for a month")]
    Ban1Month,
    [DataAnnotations.Description("Permanently Ban User")]
    PermanentBan,
}

public class CommentResult
{
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public int? ReplyId { get; set; }
    public string Content { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public int Votes { get; set; }
    public string? FlagReason { get; set; }
    public string? Notes { get; set; }
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    public string? Handle { get; set; }
    public string? ProfileUrl { get; set; }
    public string? Avatar { get; set; } //overrides ProfileUrl
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

[Alias("AspNetUsers")]
public class AppUser 
{
    [Alias("Id")]
    public string Id { get; set; }
    public string? UserName { get; set; }
    public virtual string? NormalizedUserName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public virtual int AccessFailedCount { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfileUrl { get; set; }
    [Input(Type = "file"), UploadTo("avatars")]
    public string? Avatar { get; set; } //overrides ProfileUrl
    public string? Handle { get; set; }
    public int? RefId { get; set; }
    public string RefIdStr { get; set; } = Guid.NewGuid().ToString();
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public string? LastLoginIp { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LockedDate { get; set; }
    public DateTime? BanUntilDate { get; set; }
    public string? FacebookUserId { get; set; }
    public string? GoogleUserId { get; set; }
    public string? GoogleProfilePageUrl { get; set; }
    public string? MicrosoftUserId { get; set; }
}