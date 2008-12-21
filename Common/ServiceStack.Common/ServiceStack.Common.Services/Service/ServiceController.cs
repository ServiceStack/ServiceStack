using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ServiceStack.Common.DesignPatterns.Serialization;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Common.Services.Support.Service;
using ServiceStack.Logging;

namespace ServiceStack.Common.Services.Service
{
    public class ServiceController
    {
        private readonly ILog log = LogManager.GetLogger(typeof(ServiceController));

        public ServiceController()
        {
            XmlSerializer = new DataContractSerializer();
            XmlDeserializer = new DataContractDeserializer();
            MessageInspector = new XmlMessageInspector().Parse;
        }

        public IXmlSerializer XmlSerializer { get; set; }
        public IXmlDeserializer XmlDeserializer { get; set; }
        public Assembly ServiceModelAssembly { get; set; }
        public Func<string, IXmlServiceRequest> MessageInspector { get; set; }
        public IServiceResolver ServiceResolver { get; set; }        

        private T Execute<T>(Func<T> service)
        {
            var before = DateTime.Now;
            log.DebugFormat("Executing service '{0}'", service.GetType().Name);
            var result = service();
            var timeTaken = DateTime.Now - before;
            log.DebugFormat("service '{0}' executed. Took {1} ms.", service.GetType().Name, timeTaken.TotalMilliseconds);
            return result;
        }

        public object Execute(object request)
        {
            var serviceName = request.GetType().Name;
            var service = this.ServiceResolver.FindService(serviceName);
            AssertServiceExists(service, serviceName);
            var dtoService = (IService)service;
            return Execute(() => dtoService.Execute(request));
        }

        public string ExecuteXml(string xml)
        {
            var requestContext = MessageInspector(xml);
            var service = this.ServiceResolver.FindService(requestContext.OperationName, requestContext.Version.GetValueOrDefault());
            AssertServiceExists(service, requestContext.OperationName);
            var xelementService = service as IXElementService;
            if (xelementService != null)
            {
                var request = XElement.Parse(xml);
                var response = Execute(() => xelementService.Execute(request));
                var responseXml = XmlSerializer.Parse(response);
                return responseXml;
            }
            var xmlService = service as IXmlService;
            if (xmlService != null)
            {
                return Execute(() => xmlService.Execute(xml));
            }
            var dtoService = service as IService;
            if (dtoService != null)
            {
                if (this.ServiceModelAssembly == null)
                {
                    throw new ArgumentException("ServiceModelAssembly is required for executing an IService");
                }
                var requestType = ServiceModelAssembly.GetTypes().Single(x => x.Name == requestContext.OperationName);
                var request = XmlDeserializer.Parse(xml, requestType);
                var response = Execute(() => dtoService.Execute(request));
                if (response == null) return null;
                var responseXml = XmlSerializer.Parse(response);
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