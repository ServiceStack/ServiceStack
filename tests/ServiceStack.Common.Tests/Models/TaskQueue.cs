using System;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Models
{
    public class TaskQueue
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TaskQueue));

        public const string TaskLoad = "Load";
        public const string TaskIndex = "Index";

        public const string StatusPending = "Pending";
        public const string StatusStarted = "Started";
        public const string StatusCompleted = "Completed";
        public const string StatusFailed = "Failed";

        public const int PriorityLow = 0;
        public const int PriorityMedium = 1;
        public const int PriorityHigh = 2;

        public int Id { get; set; }

        public Guid? UserId { get; set; }

        public string Task { get; set; }

        public string ContentUrn { get; set; }

        public string Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public int Priority { get; set; }

        public int NoOfAttempts { get; set; }

        public string ErrorMessage { get; set; }

        public static TaskQueue Create(int id)
        {
            return new TaskQueue
            {
                ContentUrn = "urn:track:" + id,
                CreatedDate = DateTime.Now,
                Task = TaskLoad,
                Status = StatusPending,
                NoOfAttempts = 0,
            };
        }

        public static void AssertIsEqual(TaskQueue actual, TaskQueue expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.UserId, Is.EqualTo(expected.UserId));
            Assert.That(actual.ContentUrn, Is.EqualTo(expected.ContentUrn));
            Assert.That(actual.Status, Is.EqualTo(expected.Status));
            try
            {
                Assert.That(actual.CreatedDate, Is.EqualTo(expected.CreatedDate));
            }
            catch (Exception ex)
            {
                Log.Error("Trouble with DateTime precisions, trying Assert again with rounding to seconds", ex);
                Assert.That(actual.CreatedDate.RoundToSecond(), Is.EqualTo(expected.CreatedDate.RoundToSecond()));
            }
            Assert.That(actual.Priority, Is.EqualTo(expected.Priority));
            Assert.That(actual.NoOfAttempts, Is.EqualTo(expected.NoOfAttempts));
            Assert.That(actual.ErrorMessage, Is.EqualTo(expected.ErrorMessage));
        }

    }
}