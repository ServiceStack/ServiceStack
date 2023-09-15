namespace ServiceStack.GoogleCloud;

public class GoogleCloudConfig
{
    public static void AssertValidCredentials()
    {
        var googleCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (string.IsNullOrEmpty(googleCredentials))
            throw new Exception("GOOGLE_APPLICATION_CREDENTIALS Environment Variable not set");
        if (!File.Exists(googleCredentials))
            throw new Exception($"GOOGLE_APPLICATION_CREDENTIALS '{googleCredentials}' does not exist");
    }
}
