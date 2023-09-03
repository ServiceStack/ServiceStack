using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

public interface IRequiresSchemaAsync
{
    /// <summary>
    /// Unified API to create any missing Tables, Data Structure Schema 
    /// or perform any other tasks dependencies require to run at Startup.
    /// </summary>
    Task InitSchemaAsync(CancellationToken token=default);
}