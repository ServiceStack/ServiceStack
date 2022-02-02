namespace ServiceStack.Redis
{
    public class RedisRetryableException
        : RedisException
    {
        public RedisRetryableException(string message)
            : base(message)
        {
        }

        public RedisRetryableException(string message, string code) : base(message)
        {
            Code = code;
        }

        public string Code { get; private set; }
    }
}