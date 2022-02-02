using System;
using ServiceStack.Logging;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models
{
	
	public class TaskQueue
	{
		private static readonly ILog Log;
	
		public const int PriorityHigh = 2;
	
		public const int PriorityLow = 0;
	
		public const int PriorityMedium = 1;
	
		public const string StatusCompleted = "Completed";
	
		public const string StatusFailed = "Failed";
	
		public const string StatusPending = "Pending";
	
		public const string StatusStarted = "Started";
	
		public const string TaskIndex = "Index";
	
		public const string TaskLoad = "Load";
	
		public string ContentUrn
		{
			get;
			set;
		}
	
		public DateTime CreatedDate
		{
			get;
			set;
		}
	
		public string ErrorMessage
		{
			get;
			set;
		}

		[Sequence("TaskQueue_Id_GEN")]
		public int Id
		{
			get;
			set;
		}
	
		public int NoOfAttempts
		{
			get;
			set;
		}
	
		public int Priority
		{
			get;
			set;
		}
	
		public string Status
		{
			get;
			set;
		}
	
		public string Task
		{
			get;
			set;
		}
	
		public Guid? UserId
		{
			get;
			set;
		}
	
		static TaskQueue()
		{
			TaskQueue.Log = LogManager.GetLogger(typeof(TaskQueue));
		}
	
		public TaskQueue()
		{
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
			catch (Exception exception)
			{
				TaskQueue.Log.Error("Trouble with DateTime precisions, trying Assert again with rounding to seconds", exception);
				Assert.That(DateTimeExtensions.RoundToSecond(actual.CreatedDate),
						Is.EqualTo(DateTimeExtensions.RoundToSecond(expected.CreatedDate)));
			}
			Assert.That(actual.Priority, Is.EqualTo(expected.Priority));
			Assert.That(actual.NoOfAttempts, Is.EqualTo(expected.NoOfAttempts));
			Assert.That(actual.ErrorMessage, Is.EqualTo(expected.ErrorMessage));
		}
	
		public static TaskQueue Create(int id)
		{
		    var taskQueue = new TaskQueue
		    {
		        ContentUrn = string.Concat("urn:track:", id),
		        CreatedDate = DateTime.Now,
		        Task = "Load",
		        Status = "Pending",
		        NoOfAttempts = 0
		    };
		    return taskQueue;
		}
	}
}