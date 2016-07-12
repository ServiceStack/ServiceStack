#if !NETCORE
using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class ActionExecTests
    {
        [Test]
        public void Can_run_blocking_options_in_parallel()
        {
            var sw = Stopwatch.StartNew();

            int i = 0;

            Action incrAndBlock = () => { Interlocked.Increment(ref i); Thread.Sleep(100); };

            var actions = new[]
            {
                incrAndBlock,
                incrAndBlock,
                incrAndBlock,
                incrAndBlock,
                incrAndBlock,
                incrAndBlock,
            };

            actions.ExecAllAndWait(timeout:TimeSpan.FromSeconds(30));

            "Took {0}ms".Print(sw.ElapsedMilliseconds);
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(400));
            Assert.That(i, Is.EqualTo(actions.Length));
        }
    }
}
#endif