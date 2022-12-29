using ServiceStack.IO;

namespace ServiceStack.Redis;

public interface IRedisEndpoint : IEndpoint
{
    bool Ssl { get; }
    int ConnectTimeout { get; }
    int SendTimeout { get; }
    int ReceiveTimeout { get; }
    int RetryTimeout { get; }
    int IdleTimeOutSecs { get; }
    long Db { get; }
    string Client { get; }
    /// <summary>
    /// ACL Username
    /// </summary>
    string Username { get; }
    string Password { get; }
}