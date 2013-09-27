namespace ServiceStack.Web
{
    public class Endpoint
    {
        public string Host { get; private set; }
        public int Port { get; private set; }

        public Endpoint(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}