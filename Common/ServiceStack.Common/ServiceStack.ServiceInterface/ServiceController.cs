using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ServiceStack.DesignPatterns.Serialization;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.ServiceInterface
{
	public class ServiceController : IServiceController
	{
		private readonly ILog log = LogManager.GetLogger(typeof(ServiceController));

		public ServiceController(IServiceResolver serviceResolver)
		{
			this.ServiceResolver = serviceResolver;
			this.XmlSerializer = DataContractSerializer.Instance;
			this.XmlDeserializer = DataContractDeserializer.Instance;
			this.MessageInspector = new XmlMessageInspector().Parse;
		}

		public IServiceResolver ServiceResolver { get; private set; }
		public IStringSerializer XmlSerializer { get; private set; }
		public IStringDeserializer XmlDeserializer { get; private set; }
		public Func<string, IXmlServiceRequest> MessageInspector { get; private set; }

		public T Execute<T>(Func<T> service, string serviceName)
		{
			var before = DateTime.Now;
			this.log.InfoFormat("Executing service '{0}' ...", serviceName);
			var result = service();
			var timeTaken = DateTime.Now - before;
			this.log.InfoFormat("Service '{0}' completed in {1}ms.", serviceName, timeTaken.TotalMilliseconds);
			return result;
		}

		public object Execute(IOperationContext context)
		{
			var serviceName = context.Request.Dto.GetType().Name;
			var service = this.ServiceResolver.FindService(serviceName);
			AssertServiceExists(service, serviceName);

			var dtoService = (IService)service;
			return Execute(() => dtoService.Execute(context), serviceName);
		}

		public string ExecuteXml(IOperationContext context)
		{
			var xmlRequest = (IXmlRequest)context.Request.Dto;

			var requestContext = this.MessageInspector(xmlRequest.Xml);
			var service = this.ServiceResolver.FindService(requestContext.OperationName, requestContext.Version.GetValueOrDefault());
			AssertServiceExists(service, requestContext.OperationName);

			var xelementService = service as IXElementService;
			if (xelementService != null)
			{
				context.Request.Dto = XElement.Parse(xmlRequest.Xml);
				var response = Execute(() => xelementService.Execute(context), requestContext.OperationName);
				var responseXml = this.XmlSerializer.Parse(response);
				return responseXml;
			}

			var xmlService = service as IXmlService;
			if (xmlService != null)
			{
				context.Request.Dto = xmlRequest.Xml;
				return Execute(() => xmlService.Execute(context), requestContext.OperationName);
			}

			var dtoService = service as IService;
			if (dtoService != null)
			{

				var requestType = this.ServiceResolver.FindOperationType(requestContext.OperationName, requestContext.Version);

				// Deserialize xml into request DTO
				context.Request.Dto = this.XmlDeserializer.Parse(xmlRequest.Xml, requestType);

				var response = Execute(() => dtoService.Execute(context), requestContext.OperationName);
				if (response == null) return null;
				var responseXml = this.XmlSerializer.Parse(response);
				return responseXml;
			}

			throw new NotSupportedException("Cannot execute unknown service type: " + requestContext.OperationName);
		}

		public IList<Type> OperationTypes
		{
			get { return this.ServiceResolver.OperationTypes; }
		}

		private static void AssertServiceExists(object service, string operationName)
		{
			if (service == null)
			{
				throw new NotImplementedException(
						string.Format("Unable to resolve service '{0}'", operationName));
			}
		}
	}
}