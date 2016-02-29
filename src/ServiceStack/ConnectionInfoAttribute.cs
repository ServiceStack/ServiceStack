using ServiceStack.Web;

namespace ServiceStack
{
    public class ConnectionInfoAttribute : RequestFilterAttribute
    {
        public string NamedConnection { get; set; }
        public string ConnectionString { get; set; }
        public string ProviderName { get; set; }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            req.Items[Keywords.DbInfo] = new ConnectionInfo
            {
                ConnectionString = ConnectionString,
                NamedConnection = NamedConnection,
                ProviderName = ProviderName,
            };            
        }
    }
}