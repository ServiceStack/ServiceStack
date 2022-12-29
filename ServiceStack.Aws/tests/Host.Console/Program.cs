using System.Diagnostics;
using System.Threading;
using Amazon.DynamoDBv2;
using Funq;
using ServiceStack;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Text;
using Todos;

namespace Host.Console
{
    public class AppHost : AppSelfHostBase
    {
        public AppHost() 
            : base("AWS Demo", typeof(MyServices).Assembly) {}

        public override void Configure(Container container)
        {
            JsConfig.EmitCamelCaseNames = true;

            var dynamoClient = new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig {
                ServiceURL = "http://localhost:8000",
            });

            container.Register<IPocoDynamo>(c => new PocoDynamo(dynamoClient));

            var db = container.Resolve<IPocoDynamo>();
            db.RegisterTable<Todo>();
            db.InitSchema();
        }
    }

    // Create your ServiceStack rest-ful web service implementation. 
    public class MyServices : Service
    {
    }

    class Program
    {
        static void Main(string[] args)
        {
            new AppHost()
                .Init()
                .Start("http://*:2000/");

            Process.Start("http://127.0.0.1:2000/todo.html");

            "Listening on http://127.0.0.1:2000/".Print();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
