using System;
using System.Xml.Linq;
using ServiceStack.DesignPatterns.Serialization;
using ServiceStack.Logging;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.ServiceInterface
{
	public class ServiceController
	{
		private readonly ILog log = LogManager.GetLogger(typeof(ServiceController));

		public ServiceController(IServiceResolver serviceResolver)
		{
			this.ServiceResolver = serviceResolver;
			this.XmlSerializer = DataContractSerializer.Instance;
			this.XmlDeserializer = DataContractDeserializer.Instance;
			this.MessageInspector = new XmlMessageInspector().Parse;
		}

		public ServiceStack.ServiceInterface.IServiceResolver ServiceResolver { get; private set; }
		public IStringSerializer XmlSerializer { get; private set; }
		public IStringDeserializer XmlDeserializer { get; private set; }
		public Func<string, IXmlServiceRequest> MessageInspector { get; private set; }

		private T Execute<T>(Func<T> service)
		{
			var before = DateTime.Now;
			this.log.DebugFormat("Executing service '{0}'", service.GetType().Name);
			var result = service();
			var timeTaken = DateTime.Now - before;
			this.log.DebugFormat("service '{0}' executed. Took {1} ms.", service.GetType().Name, timeTaken.TotalMilliseconds);
			return result;
		}

		public object Execute(CallContext context)
		{
			var serviceName = context.Request.Dto.GetType().Name;
			var service = this.ServiceResolver.FindService(serviceName);
			AssertServiceExists(service, serviceName);
			var dtoService = (IService)service;
			return Execute(() => dtoService.Execute(context));
		}

		public string ExecuteXml(CallContext context)
		{
			XmlRequestDto xmlRequest = context.Request.GetDto<XmlRequestDto>();

			var requestContext = this.MessageInspector(xmlRequest.Xml);
			var service = this.ServiceResolver.FindService(requestContext.OperationName, requestContext.Version.GetValueOrDefault());
			AssertServiceExists(service, requestContext.OperationName);

			var xelementService = service as IXElementService;
			if (xelementService != null)
			{
				context.Request.Dto = XElement.Parse(xmlRequest.Xml);
				var response = Execute(() => xelementService.Execute(context));
				var responseXml = this.XmlSerializer.Parse(response);
				return responseXml;
			}

			IXmlService xmlService = service as IXmlService;
			if (xmlService != null)
			{
				context.Request.Dto = xmlRequest.Xml;
				return Execute(() => xmlService.Execute(context));
			}

			var dtoService = service as IService;
			if (dtoService != null)
			{
				if (xmlRequest.ServiceModelInfo == null)
				{
					throw new ArgumentException("ServiceModelAssembly is required for executing an IService");
				}

				Type requestType;
				if (requestContext.Version == null)
				{
					requestType = xmlRequest.ServiceModelInfo.GetDtoTypeFromOperation(requestContext.OperationName);
				}
				else
				{
					requestType = xmlRequest.ServiceModelInfo.GetDtoTypeFromOperation(requestContext.OperationName, (int)requestContext.Version);
				}

				// Deserialize xml into request DTO
				context.Request.Dto = this.XmlDeserializer.Parse(xmlRequest.Xml, requestType);

				var response = Execute(() => dtoService.Execute(context));
				if (response == null) return null;
				var responseXml = this.XmlSerializer.Parse(response);
				return responseXml;
			}

			throw new NotSupportedException("Cannot execute unknown service type: " + requestContext.OperationName);
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