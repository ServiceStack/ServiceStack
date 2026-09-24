#nullable enable

using ServiceStack.Web;

namespace ServiceStack.Jobs;

/// <summary>
/// A Background Job execution recorded by ProfilingFeature.
/// Defined for every Target Framework so ProfilingFeature can handle it, while the Jobs providers
/// that emit it are .NET 8+ only.
/// </summary>
public class JobDiagnosticEvent : DiagnosticEvent
{
    public override string Source => "ServiceStack.Jobs";
    public BackgroundJobBase Job { get; set; } = null!;
    public IRequest? Request { get; set; }
}
