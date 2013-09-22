namespace ServiceStack.Web
{
    public class EndPoint
    {
        public string Host { get; private set; }
        public int Port { get; private set; }

        public EndPoint(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}