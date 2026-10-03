#nullable enable

namespace ServiceStack.Data;

/// <summary>
/// A DB connection that records whether it has run a statement that writes, e.g. OrmLiteConnection
/// </summary>
public interface IHasDbWrites
{
    bool HasWrites { get; }
}
