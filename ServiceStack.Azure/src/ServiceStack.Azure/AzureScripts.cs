using ServiceStack.Azure.Storage;
using ServiceStack.Script;
// ReSharper disable InconsistentNaming

namespace ServiceStack.Azure;

public class AzureScriptPlugin : IScriptPlugin
{
    public void Register(ScriptContext context)
    {
        context.ScriptMethods.Add(new AzureScripts());
    }
}
    
public class AzureScripts : ScriptMethods
{
    public AzureBlobVirtualFiles vfsAzureBlob(string connectionString, string containerName) =>
        new(connectionString, containerName);

    public AzureAppendBlobVirtualFiles vfsAzureAppendBlob(string connectionString, string containerName) =>
        new(connectionString, containerName);
}