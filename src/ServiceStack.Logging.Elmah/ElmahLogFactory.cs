using System;

namespace ServiceStack.Logging.Elmah
{
	/// <summary>
	/// Elmah log factory that wraps another log factory, providing interception facilities on log calls.  For Error or Fatal calls, the
	/// details will be logged to Elmah in addition to the originally intended logger.  For all other log types, only the original logger is
	/// used.
	/// </summary>
	/// <remarks>	9/2/2011. </remarks>
	public class ElmahLogFactory : ILogFactory
	{
		private readonly ILogFactory _logFactory;

		/// <summary>	Constructor. </summary>
		/// <remarks>	9/2/2011. </remarks>
		/// <param name="logFactory">	The log factory that provides the original . </param>
		public ElmahLogFactory(ILogFactory logFactory)
		{
			if (null == logFactory) { throw new ArgumentNullException("logFactory"); }

			_logFactory = logFactory;
		}

		/// <summary>	Gets a logger from the wrapped logFactory. </summary>
		/// <remarks>	9/2/2011. </remarks>
		/// <param name="typeName">	Name of the type. </param>
		/// <returns>	The logger. </returns>
		public ILog GetLogger(string typeName)
		{
			return new ElmahInterceptingLogger(_logFactory.GetLogger(typeName));
		}

		/// <summary>	Gets a logger from the wrapped logFactory. </summary>
		/// <remarks>	9/2/2011. </remarks>
		/// <param name="type">	The type. </param>
		/// <returns>	The logger. </returns>
		public ILog GetLogger(Type type)
		{
			return new ElmahInterceptingLogger(_logFactory.GetLogger(type));
		}
	}
}