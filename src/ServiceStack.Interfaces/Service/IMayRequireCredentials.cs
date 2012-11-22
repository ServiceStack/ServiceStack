namespace ServiceStack.Service
{
    public interface IMayRequireCredentials
    {
        void SetCredentials(string userName, string password);
    }
}