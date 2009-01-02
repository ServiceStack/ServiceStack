using System;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceModel;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Sakila.Tests.ServiceInterface.Version100
{
	public abstract class BaseAppTestFixture
	{
		protected BaseAppTestFixture(ITestParameters parameters)
		{
			this.Parameters = parameters;
		}

		/// <summary>
		/// Test configuration parameters
		/// </summary>
		protected ITestParameters Parameters { get; private set; }

		/// <summary>
		/// Application context
		/// </summary>
		protected OperationContext AppContext { get; private set; }

		/// <summary>
		/// String resource manager
		/// </summary>
		protected virtual IResourceManager ResourceManager { get; private set; }

		/// <summary>
		/// Log factory
		/// </summary>
		protected virtual ILogFactory LogFactory { get; private set; }

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Log { get; private set; }

		[TestFixtureSetUp]
		public virtual void FixtureSetUp()
		{
			try
			{
				this.LogFactory = this.Parameters.LogFactory;

				// Create logger
				this.Log = this.LogFactory.GetLogger(GetType());

				// Create the string manager
				//this.ResourceManager = new StringResourceManager(this.LogFactory);

				// Create the app context
				this.AppContext = new OperationContext {
					Cache = this.Parameters.Cache,
				};
			}
			catch (Exception ex)
			{
				this.LogException(ex);
				throw;
			}
		}

		[TestFixtureTearDown]
		public virtual void FixtureTearDown()
		{
			this.AppContext = null;
			this.Log = null;
			this.LogFactory = null;
		}

		/// <summary>
		/// // Create a call context with the provided facade and request DTO
		/// </summary>
		/// <param name="facade">The call context facade</param>
		/// <param name="requestDto">The request DTO</param>
		/// <returns>A call context</returns>
		protected virtual CallContext CreateCallContext(IDisposable facade, object requestDto)
		{
			return new CallContext(this.AppContext, new RequestContext(requestDto, new FactoryProvider(null, facade)));
		}

		/// <summary>
		/// // Create a call context with the provided facade and xml request DTO parameters
		/// </summary>
		/// <param name="facade">The call context facade</param>
		/// <param name="xml">The request xml string</param>
		/// <param name="modelInfo">The service model info</param>
		/// <returns>A call context</returns>
		protected virtual CallContext CreateCallContext(IDisposable facade, string xml, ServiceModelInfo modelInfo)
		{
			var requestDto = new XmlRequestDto(xml, modelInfo);
			return new CallContext(this.AppContext, new RequestContext(requestDto, new FactoryProvider(null, facade)));
		}

		protected virtual void LogException(Exception ex)
		{
			string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
			string errorMsg = string.Format("{0}: {1}", callingMethodName, ex.Message);
			this.Log.Error(errorMsg, ex);
		}
	}
}