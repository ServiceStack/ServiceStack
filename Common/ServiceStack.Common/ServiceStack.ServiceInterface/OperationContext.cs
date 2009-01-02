using System;
using System.Web;
using ServiceStack.CacheAccess;
using ServiceStack.Configuration;
using ServiceStack.LogicFacade;
using ServiceStack.Logging;

namespace ServiceStack.ServiceInterface
{

	public class OperationContext : IOperationContext
	{
		public static OperationContext Instance { get; private set; }

		public static void SetInstanceContext(OperationContext operationContext)
		{
			if (OperationContext.Instance != null)
			{
				throw new NotSupportedException("Cannot set the singleton instance once it has already been set");
			}
			OperationContext.Instance = operationContext;
		}

		[ThreadStatic]
		public static OperationContext current;
		public static OperationContext Current
		{
			get { return current; }
			set { current = value; }
		}

		public ICacheClient Cache { get; set; }

		public IResourceManager Resources { get; set; }

		public IFactoryProvider Factory { get; set; }
	}
}