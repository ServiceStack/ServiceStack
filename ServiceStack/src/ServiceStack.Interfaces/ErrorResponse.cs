#nullable enable

using System.Runtime.Serialization;

namespace ServiceStack;

/// <summary>
/// Generic ResponseStatus for when Response Type can't be inferred.
/// In schema-less formats like JSON, JSV it has the same shape as a typed Response DTO
/// </summary>
[DataContract]
public class ErrorResponse : IHasResponseStatus
{
    [DataMember(Order = 1)]
    public ResponseStatus? ResponseStatus { get; set; }
}