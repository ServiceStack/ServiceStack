using System;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class ActionExecTests
    {
        [Test]
        public void Can_run_blocking_actions_in_parallel()
        {
            using var started = new CountdownEvent(2);
            var completed = 0;

            Action waitForBothActions = () =>
            {
                started.Signal();
                if (started.Wait(TimeSpan.FromSeconds(10)))
                    Interlocked.Increment(ref completed);
            };

            var actions = new[] { waitForBothActions, waitForBothActions };
            actions.ExecAllAndWait(timeout: TimeSpan.FromSeconds(30));

            Assert.That(completed, Is.EqualTo(actions.Length), "Both actions must start before either can complete.");
        }
    }
}