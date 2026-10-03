#nullable enable

using System;

namespace ServiceStack;

/// <summary>
/// Whether the queries of an AutoQuery API, or of every AutoQuery API of a table, read from the read replica of their
/// connection, instead of AutoQueryFeature.UseReadReplica, e.g. [ReadReplica] on a report's Request DTO, or
/// [ReadReplica(false)] on an API that has to read what was just written
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class ReadReplicaAttribute(bool enabled = true) : AttributeBase
{
    public bool Enabled { get; set; } = enabled;
}
