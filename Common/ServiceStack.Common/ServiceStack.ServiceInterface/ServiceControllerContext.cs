using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using ServiceStack.DesignPatterns.Serialization;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.ServiceInterface
{
	[Obsolete("Use IService<> instead")]
	public class ServiceControllerContext 
		: IServiceController
	{
		private readonly ILog log = LogManager.GetLogger(typeof(ServiceControllerContext));

		public ServiceControllerContext(IServiceResolver serviceResolver)
		{
			this.ServiceResolver = serviceResolver;
			this.XmlSerializer = DataContractSerializer.Instance;
			this.XmlDeserializer = DataContractDeserializer.Instance;
			this.MessageInspector = new XmlMessageInspector().Parse;
			this.EnablePortRestrictions = true;
			this.OperationContextFactory = CreateOperationContext;	
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

		public Func<object, EndpointAttributes, IOperationContext> OperationContextFactory;

		private static IOperationContext CreateOperationContext(object requestDto, EndpointAttributes endpointAttributes)
		{
			return new BasicOperationContext<IApplicationContext, RequestContext>(
				ApplicationContext.Instance, new RequestContext(requestDto, endpointAttributes));
		}

		public object Execute(object request, IRequestContext requestContext)
		{
			using (var operationContext = OperationContextFactory(request, requestContext.EndpointAttributes))
			{
				return Execute(operationContext);
			} 
		}

		public object Execute(IOperationContext context)
		{
			var requestContext = (RequestContext) context.Request;
			var serviceName = requestContext.Dto.GetType().Name;
			var service = this.ServiceResolver.FindService(serviceName);
			AssertServiceExists(service, serviceName);
			if (EnablePortRestrictions)
			{
				AssertServiceRestrictions(service, context.Request.EndpointAttributes, serviceName);
			}

			var dtoService = (IService)service;
			return Execute(() => dtoService.Execute(context), serviceName);
		}

		public object ExecuteText(string text, IRequestContext requestContext)
		{
			// Create a xml request DTO which the service controller will parse and reassign the call
			// context request DTO to a object expected by the relevant port
			var requestDto = new XmlRequestDto(text);

			using (var operationContext = OperationContextFactory(requestDto, requestContext.EndpointAttributes))
			{
				return ExecuteXml(operationContext);
			}
		}

		public string ExecuteXml(IOperationContext context)
		{
			var requestContext = (RequestContext)context.Request;
			var xmlRequest = (IXmlRequest)requestContext.Dto;

			var xmlServiceRequest = this.MessageInspector(xmlRequest.Xml);
			var service = this.ServiceResolver.FindService(xmlServiceRequest.OperationName, xmlServiceRequest.Version.GetValueOrDefault());
			AssertServiceExists(service, xmlServiceRequest.OperationName);
			if (EnablePortRestrictions)
			{
				AssertServiceRestrictions(service, context.Request.EndpointAttributes, xmlServiceRequest.OperationName);
			}

			var xelementService = service as IXElementService;
			if (xelementService != null)
			{
				requestContext.Dto = XElement.Parse(xmlRequest.Xml);
				var response = Execute(() => xelementService.Execute(context), xmlServiceRequest.OperationName);
				var responseXml = this.XmlSerializer.Parse(response);
				return responseXml;
			}

			var xmlService = service as IXmlService;
			if (xmlService != null)
			{
				requestContext.Dto = xmlRequest.Xml;
				return Execute(() => xmlService.Execute(context), xmlServiceRequest.OperationName);
			}

			var dtoService = service as IService;
			if (dtoService != null)
			{

				var requestType = this.ServiceResolver.FindOperationType(xmlServiceRequest.OperationName, xmlServiceRequest.Version);

				// Deserialize xml into request DTO
				requestContext.Dto = this.XmlDeserializer.Parse(xmlRequest.Xml, requestType);

				var response = Execute(() => dtoService.Execute(context), xmlServiceRequest.OperationName);
				if (response == null) return null;
				var responseXml = this.XmlSerializer.Parse(response);
				return responseXml;
			}

			throw new NotSupportedException("Cannot execute unknown service type: " + xmlServiceRequest.OperationName);
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