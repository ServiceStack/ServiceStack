namespace ServiceStack
{
    public enum RequestFilterPriority : int
    {
        Authenticate = -100,
        RequiredRole = -90,
        RequiredPermission = -80,
    }
}