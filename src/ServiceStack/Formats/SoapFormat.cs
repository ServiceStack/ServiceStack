#if !NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack
{
    public class SoapFormat : IPlugin
    {
        public bool DisableSoap11 { get; set; }
        public bool DisableSoap12 { get; set; }
        
        public void Register(IAppHost appHost)
        {
            var contentTypes = (ContentTypes)appHost.ContentTypes;
            var predefinedRoutes = appHost.GetPlugin<PredefinedRoutesFeature>();
            if (predefinedRoutes == null)
                throw new NotSupportedException("SoapFormat requires the PredefinedRoutesFeature Plugin");
                
            if (!DisableSoap11)
            {
                contentTypes.Register(MimeTypes.Soap11, SoapHandler.SerializeSoap11ToStream, null);
                contentTypes.ContentTypeStringSerializers[MimeTypes.Soap11] = (r, o) =>
                    SoapHandler.SerializeSoap11ToBytes(r, o).FromUtf8Bytes();
                
                var soap11 = ContentFormat.GetContentFormat(Format.Soap11);
                predefinedRoutes.HandlerMappings[soap11] = () => new Soap11MessageReplyHttpHandler();
            }

            if (!DisableSoap12)
            {
                contentTypes.Register(MimeTypes.Soap12, SoapHandler.SerializeSoap12ToStream, null);
                contentTypes.ContentTypeStringSerializers[MimeTypes.Soap12] = (r, o) =>
                    SoapHandler.SerializeSoap12ToBytes(r, o).FromUtf8Bytes();
                
                var soap12 = ContentFormat.GetContentFormat(Format.Soap12);
                predefinedRoutes.HandlerMappings[soap12] = () => new Soap12MessageReplyHttpHandler();
            }
        }
    }

    // Overridable APIs to customize SOAP behavior
    public abstract partial class ServiceStackHost
    {
        public virtual List<Type> ExportSoapOperationTypes(List<Type> operationTypes)
        {
            var types = operationTypes
                .Where(x => !x.AllAttributes<ExcludeAttribute>()
                    .Any(attr => attr.Feature.Has(Feature.Soap)))
                .Where(x => !x.IsGenericTypeDefinition)
                .ToList();
            return types;
        }

        public virtual bool ExportSoapType(Type type)
        {
            return !type.IsGenericTypeDefinition &&
                   !type.AllAttributes<ExcludeAttribute>()
                       .Any(attr => attr.Feature.Has(Feature.Soap));
        }

        public virtual void WriteSoapMessage(IRequest req, System.ServiceModel.Channels.Message message, Stream outputStream)
        {
            try
            {
                using (var writer = XmlWriter.Create(outputStream, Config.XmlWriterSettings))
                {
                    message.WriteMessage(writer);
                }
            }
            catch (Exception ex)
            {
                var response = OnServiceException(req, req.Dto, ex).Result;
                if (response == null || !outputStream.CanSeek)
                    return;

                outputStream.Position = 0;
                try
                {
                    message = SoapHandler.CreateResponseMessage(response, message.Version, req.Dto.GetType(),
                        req.GetSoapMessage().Headers.Action == null);
                    using (var writer = XmlWriter.Create(outputStream, Config.XmlWriterSettings))
                    {
                        message.WriteMessage(writer);
                    }
                }
                catch { }
            }
            finally
            {
                HostContext.CompleteRequest(req);
            }
        }
    }
}

#endif