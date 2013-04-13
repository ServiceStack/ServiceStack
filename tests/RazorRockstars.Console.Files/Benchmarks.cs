using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Razor2;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Formats;

namespace RazorRockstars.Console.Files
{
    [Explicit("Ignore benchmarks")]
    [TestFixture]
    public class Benchmarks
    {
        AppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            appHost = new AppHost();
            appHost.Init();
            appHost.Start("http://*:1337/");
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Benchmark_Razor_vs_Markdown()
        {
            var iterations = 10000;
            var razorFormat = RazorFormat.Instance;
            var markdownFmt = MarkdownFormat.Instance;
            var dto = new RockstarsResponse { Results = Rockstar.SeedData.ToList() };

            "Warm up MVC Razor...".Print();
            var mockReq = new MockHttpRequest { OperationName = "RockstarsRazor" };
            var mockRes = new MockHttpResponse();
            razorFormat.ProcessRequest(mockReq, mockRes, dto);
            mockRes.ReadAsString().Print();

            "Warm up Markdown Razor...".Print();
            mockReq = new MockHttpRequest { OperationName = "RockstarsMark" };
            mockRes = new MockHttpResponse();
            markdownFmt.ProcessRequest(mockReq, mockRes, dto);
            mockRes.ReadAsString().Print();

            "\n\nRunning for {0} times...".Fmt(iterations).Print();
            CompareRuns(iterations,
                "MVC Razor", () => {
                    mockReq = new MockHttpRequest { OperationName = "RockstarsRazor" };
                    mockRes = new MockHttpResponse();
                    razorFormat.ProcessRequest(mockReq, mockRes, dto);
                },
                "Markdown Razor", () => {
                    mockReq = new MockHttpRequest { OperationName = "RockstarsMark" };
                    mockRes = new MockHttpResponse();
                    markdownFmt.ProcessRequest(mockReq, mockRes, dto);
                });
        }

        protected void CompareRuns(int iterations, string run1Name, Action run1Action, string run2Name, Action run2Action)
        {
            var run1 = RunAction(run1Action, iterations, run1Name);
            var run2 = RunAction(run2Action, iterations, run2Name);

            var runDiff = run1 - run2;
            var run1IsSlower = runDiff > 0;
            var slowerRun = run1IsSlower ? run1Name : run2Name;
            var fasterRun = run1IsSlower ? run2Name : run1Name;
            var runDiffTime = run1IsSlower ? runDiff : runDiff * -1;
            var runDiffAvg = run1IsSlower ? run1 / run2 : run2 / run1;

            "{0} was {1}ms or {2} times slower than {3}".Fmt(
            slowerRun, runDiffTime, Math.Round(runDiffAvg, 2), fasterRun).Print();
        }

        protected decimal RunAction(Action action, int iterations, string actionName)
        {
            actionName = actionName ?? action.GetType().Name;
            var ticksTaken = Measure(action, iterations);
            var timeSpan = TimeSpan.FromSeconds(ticksTaken * 1d / Stopwatch.Frequency);

            "{0} took {1}ms ({2} ticks), avg: {3} ticks".Fmt(
            actionName, timeSpan.TotalMilliseconds, ticksTaken, (ticksTaken / iterations)).Print();

            return ticksTaken;
        }

        protected long Measure(Action action, decimal iterations)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var begin = Stopwatch.GetTimestamp();

            for (var i = 0; i < iterations; i++)
            {
                action();
            }

            var end = Stopwatch.GetTimestamp();

            return (end - begin);
        }

        protected void WarmUp(params Action[] actions)
        {
            foreach (var action in actions)
            {
                action();
                GC.Collect();
            }
        }

    }
}
