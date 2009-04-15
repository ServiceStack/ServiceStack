using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using ServiceStack.DesignPatterns.Serialization;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
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
			this.EnablePortRestrictions = true;
		}

		public IServiceResolver ServiceResolver { get; private set; }
		public IStringSerializer XmlSerializer { get; private set; }
		public IStringDeserializer XmlDeserializer { get; private set; }
		public Func<string, IXmlServiceRequest> MessageInspector { get; private set; }
		public bool EnablePortRestrictions { get; set; }

		public T Execute<T>(Func<T> service, string serviceName)
		{
			var serviceLog = LogManager.GetLogger(serviceName);

			var before = DateTime.Now;
			//Log to global and service specific loggers
			this.log.InfoFormat("Executing service '{0}' at {1}", serviceName, before);
			serviceLog.InfoFormat("ServiceBegin: {0}, {1}", serviceName, before);

			var result = service();
			var after = DateTime.Now;
			var timeTaken = after - before;

			this.log.InfoFormat("Service '{0}' completed in {1}ms at {2}", serviceName, timeTaken.TotalMilliseconds, after);
			serviceLog.InfoFormat("ServiceEnd: {0}, {1}, {2}", serviceName, after, timeTaken.TotalMilliseconds);
			return result;
		}

		public object Execute(IOperationContext context)
		{
			var serviceName = context.Request.Dto.GetType().Name;
			var service = this.ServiceResolver.FindService(serviceName);
			AssertServiceExists(service, serviceName);
			AssertServiceRestrictions(service, context.Request.EndpointAttributes, serviceName);

			var dtoService = (IService)service;
			return Execute(() => dtoService.Execute(context), serviceName);
		}

		public string ExecuteXml(IOperationContext context)
		{
			var xmlRequest = (IXmlRequest)context.Request.Dto;

			var requestContext = this.MessageInspector(xmlRequest.Xml);
			var service = this.ServiceResolver.FindService(requestContext.OperationName, requestContext.Version.GetValueOrDefault());
			AssertServiceExists(service, requestContext.OperationName);
			AssertServiceRestrictions(service, context.Request.EndpointAttributes, requestContext.OperationName);

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

		public static void AssertServiceRestrictions(object service, EndpointAttributes attributes, string serviceName)
		{
			var serviceType = service.GetType();
			var attrs = serviceType.GetCustomAttributes(typeof(PortAttribute), false);
			if (attrs.Length == 0)
			{
				return;
			}

			var portAttr = (PortAttribute)attrs[0];

			var allPortRestrictionsMet = (portAttr.PortRestrictions & attributes) == portAttr.PortRestrictions;
			if (allPortRestrictionsMet)
			{
				return;
			}

			var failedRestrictions = new StringBuilder();
			foreach (EndpointAttributes value in Enum.GetValues(typeof(EndpointAttributes)))
			{
				var attributeInCurrentRequest = (attributes & value) == value;
				if (attributeInCurrentRequest)
				{
					continue;
				}

				//Not InCurrentRequest and Is in PortRestrictions
				var portRestrictionNotMet = (portAttr.PortRestrictions & value) == value;
				if (portRestrictionNotMet)
				{
					if (failedRestrictions.Length != 0) failedRestrictions.Append(", ");
					failedRestrictions.Append(value);
				}
			}

			throw new UnauthorizedAccessException(
				string.Format("Could not execute service '{0}', The following restrictions were not met: '{1}'",
					serviceName, failedRestrictions));
		}

		public IList<Type> OperationTypes
		{
			get { return this.ServiceResolver.OperationTypes; }
		}

		public IList<Type> AllOperationTypes
		{
			get { return this.ServiceResolver.AllOperationTypes; }
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