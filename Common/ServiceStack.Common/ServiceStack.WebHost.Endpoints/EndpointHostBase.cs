using System;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Service;

namespace ServiceStack.WebHost.Endpoints
{
	public abstract class EndpointHostBase : IServiceHost
	{
		private readonly ILog log = LogManager.GetLogger(typeof(EndpointHostBase));
		private IServiceController ServiceController { get; set; }
		private readonly DateTime StartTime;

		protected EndpointHostBase()
		{
			this.StartTime = DateTime.Now;
			log.Info("Begin Initializing Application...");
		}

		public void SetConfig(EndpointHostConfig config)
		{
			config.ServiceHost = config.ServiceHost ?? this;
			EndpointHost.Config = config;

			this.ServiceController = config.ServiceController;

			var elapsed = DateTime.Now - this.StartTime;
			log.InfoFormat("Initializing Application took {0}ms", elapsed.TotalMilliseconds);
		}

		protected abstract IOperationContext CreateOperationContext(object requestDto);

		public virtual object ExecuteService(object requestDto)
		{
			using (var context = CreateOperationContext(requestDto))
			{
				return this.ServiceController.Execute(context);
			}
		}

		public virtual string ExecuteXmlService(string xml)
		{
			// Create a xml request DTO which the service controller will parse and reassign the call
			// context request DTO to a object expected by the relevant port
			var requestDto = new XmlRequestDto(xml);

			using (var context = CreateOperationContext(requestDto))
			{
				return this.ServiceController.ExecuteXml(context);
			}
		}
	}
}