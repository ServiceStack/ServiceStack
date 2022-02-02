using ServiceStack.Azure.Storage;
using ServiceStack.Script;

namespace ServiceStack.Azure
{
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
            new AzureBlobVirtualFiles(connectionString, containerName);

        public AzureAppendBlobVirtualFiles vfsAzureAppendBlob(string connectionString, string containerName) => 
            new AzureAppendBlobVirtualFiles(connectionString, containerName);
    }
}
