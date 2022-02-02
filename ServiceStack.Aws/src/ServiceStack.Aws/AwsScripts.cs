using Amazon;
using Amazon.S3;
using ServiceStack.IO;
using ServiceStack.Script;

namespace ServiceStack.Aws
{

    public class AwsScriptPlugin : IScriptPlugin
    {
        public void Register(ScriptContext context)
        {
            context.ScriptMethods.Add(new AwsScripts());
        }
    }
    
    public class AwsScripts : ScriptMethods
    {
        public AmazonS3Client amazonS3Client(string awsAccessKeyId, string awsSecretAccessKey, string region) =>
            new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.GetBySystemName(region));
        
        public S3VirtualFiles vfsS3(AmazonS3Client s3Client, string bucketName) => 
            new S3VirtualFiles(s3Client, bucketName);

        public S3VirtualFiles vfsS3(string awsAccessKeyId, string awsSecretAccessKey, string region, string bucketName) => 
            new S3VirtualFiles(amazonS3Client(awsAccessKeyId, awsSecretAccessKey, region), bucketName);
    }
    
}