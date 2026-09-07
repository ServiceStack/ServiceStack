#nullable enable
namespace ServiceStack;

public interface IHasSchema : IRequiresSchema
{
    /// <summary>
    /// Unified API to drop any Tables, Data Structure Schema 
    /// or perform any other tasks dependencies require to run at Startup.
    /// </summary>
    void DropSchema();
}