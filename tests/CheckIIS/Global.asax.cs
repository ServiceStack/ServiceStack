using System;
using System.Threading;
using ServiceStack;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Hosting;

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

            TestCase6();
        }

        private void TestCase1()
        {
            HostingEnvironment.QueueBackgroundWorkItem(LongMessage_SWithSleep2Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwaySleep2Async);
        }

        private void TestCase2()
        {
            HostingEnvironment.QueueBackgroundWorkItem(LongMessage_SWithSleep2Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayDelay2Async);
        }

        private void TestCase3()
        {
            HostingEnvironment.QueueBackgroundWorkItem(LongMessage_SWithSleep200Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayDelay200Async);
        }

        private void TestCase4()
        {
            HostingEnvironment.QueueBackgroundWorkItem(LongMessage_SWithSleep2Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayDelay1Async);
        }

        private void TestCase5()
        {
            HostingEnvironment.QueueBackgroundWorkItem(LongMessage_SWithSleep2Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayDelay1Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayDelay2Async);
        }

        private void TestCase6()
        {
            HostingEnvironment.QueueBackgroundWorkItem(LongMessage_SWithSleep2Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayDelay1Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayDelay2Async);
            HostingEnvironment.QueueBackgroundWorkItem(GiftAwayDelay1Async);
        }

        public async Task GiftAwayDelay200Async(CancellationToken cancellationToken)
        {
            await GiftAwayAsync(true, 200, cancellationToken);
        }
        public async Task GiftAwayDelay2Async(CancellationToken cancellationToken)
        {
            await GiftAwayAsync(true, 2, cancellationToken);
        }
        public async Task GiftAwayDelay1Async(CancellationToken cancellationToken)
        {
            await GiftAwayAsync(true, 1, cancellationToken);
        }
        public async Task GiftAwaySleep2Async(CancellationToken cancellationToken)
        {
            await GiftAwayAsync(false, 2, cancellationToken);
        }
        public async Task GiftAwaySleep200Async(CancellationToken cancellationToken)
        {
            await GiftAwayAsync(false, 200, cancellationToken);
        }
        public async Task GiftAwaySleep1Async(CancellationToken cancellationToken)
        {
            await GiftAwayAsync(false, 1, cancellationToken);
        }
        public async Task GiftAwayAsync(bool delay, int waitInMs, CancellationToken cancellationToken)
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
                if (delay)
                {
                    await Task.Delay(waitInMs);
                }
                else
                {
                    Thread.Sleep(waitInMs);
                }
                await serverEvents.NotifyAllAsync("cmd.bombard", msg);
            }

        }

        public async Task LongMessage_SWithSleep1Async(CancellationToken cancellationToken)
        {
            await LongMessage_SAsync(false, 1, cancellationToken);
        }
        public async Task LongMessage_SWithSleep2Async(CancellationToken cancellationToken)
        {
            await LongMessage_SAsync(false, 2, cancellationToken);
        }
        public async Task LongMessage_SWithSleep200Async(CancellationToken cancellationToken)
        {
            await LongMessage_SAsync(false, 200, cancellationToken);
        }
        public async Task LongMessage_SAsync(bool delay, int waitInMs, CancellationToken cancellationToken)
        {
            const int msgSize = 420 * 1024;
            var serverEvents = HostContext.TryResolve<IServerEvents>();
            var stuff = new LongMessage
            {
                Msg = $"size{msgSize}-{new string('S', msgSize)}"
            };

            while (true)
            {
                if (delay)
                {
                    await Task.Delay(waitInMs);
                }
                else
                {
                    Thread.Sleep(waitInMs);
                }
                await serverEvents.NotifyAllAsync("cmd.MS", stuff, cancellationToken);
            }
        }

    }
}