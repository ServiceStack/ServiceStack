using ServiceStack.DataAnnotations;

namespace ServiceStack.AI;

[UniqueConstraint(nameof(User), nameof(RequestId))]
public class ChatProjectCreation
{
    [PrimaryKey, StringLength(36)] public string Id { get; set; } = null!;
    [Index, StringLength(200)] public string User { get; set; } = null!;
    [StringLength(36)] public string RequestId { get; set; } = null!;
    [StringLength(64)] public string Fingerprint { get; set; } = null!;
    [StringLength(StringLengthAttribute.MaxText)] public string Payload { get; set; } = "{}";
    public string Name { get; set; } = null!;
    [StringLength(StringLengthAttribute.MaxText)] public string Destination { get; set; } = null!;
    public string State { get; set; } = "queued";
    public string Message { get; set; } = "Preparing your project…";
    public int? Percent { get; set; }
    public long Revision { get; set; } = 1;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string? Owner { get; set; }
    public DateTime? Lease { get; set; }
    public bool Cancelled { get; set; }
    public bool Prepared { get; set; }
    public int? Child { get; set; }
    public string Temporary { get; set; } = null!;
    [StringLength(StringLengthAttribute.MaxText)] public string? Result { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Error { get; set; }
}

/// <summary>Portable unique live name/destination reservations instead of partial SQLite indexes.</summary>
public class ChatProjectCreationReservation
{
    [PrimaryKey, StringLength(64)] public string Id { get; set; } = null!;
    [Index, StringLength(36)] public string OperationId { get; set; } = null!;
    [Index, StringLength(200)] public string User { get; set; } = null!;
}
