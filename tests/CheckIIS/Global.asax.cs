using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Optimization;
using System.Web.Routing;
using ServiceStack;

namespace CheckIIS
{
    public class SimpleMessage
    {
        public string MyMsg { get; set; }
        public IList<string> Gifts { get; set; }
    }

    public class LongMessage
    {
        public string Msg { get; set; }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
            RunServerEventsLoadTest();

            // Code that runs on application startup
            // RouteConfig.RegisterRoutes(RouteTable.Routes);
            // BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        void RunServerEventsLoadTest()
        {
            HostingEnvironment.QueueBackgroundWorkItem(LongMessage_SWithSleepAsync);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayAsync);
        }

        public async Task GiftAwayAsync(CancellationToken token)
        {
            var serverEvents = HostContext.TryResolve<IServerEvents>();

            var myGifts = new List<string>
            {
                "On the first day of Christmas my true love gave to me: a Partridge in a Pear Tree.",
                "On the second day of Christmas my true love gave to me: two Turtle Doves, and a Partridge in a Pear Tree.",
                "On the third day of Christmas my true love gave to me: three French Hens, two Turtle Doves, and a Partridge in a Pear Tree.",
                "On the fourth day of Christmas my true love gave to me: four Calling Birds, three French Hens, two Turtle Doves, and a Partridge in a Pear Tree.",
                "On the fifth day of Christmas my true love gave to me: five Gold Rings, four Calling Birds, three French Hens, two Turtle Doves, and a Partridge in a Pear Tree."
            };
            
            var msg = new SimpleMessage
            {
                MyMsg = "song",
                Gifts = myGifts
            };

            while (true)
            {
                Thread.Sleep(2);
                await serverEvents.NotifyAllAsync("cmd.bombard", msg, token);
            }
        }

        public async Task LongMessage_SWithSleepAsync(CancellationToken token)
        {
            const int msgSize = 420 * 1024;
            var serverEvents = HostContext.TryResolve<IServerEvents>();
            var stuff = new LongMessage
            {
                Msg = $"size{msgSize}-{new string('S', msgSize)}"
            };

            while (true)
            {
                Thread.Sleep(2);
                await serverEvents.NotifyAllAsync("cmd.MS", stuff, token);
            }
        }
    }
    
}