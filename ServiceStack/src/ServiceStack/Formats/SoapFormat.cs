#if !NETCORE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack;

public class SoapFormat : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Soap;
    public bool DisableSoap11 { get; set; }
    public bool DisableSoap12 { get; set; }
    
    public void Register(IAppHost appHost)
    {
        if (appHost == null)
            return;

        var contentTypes = appHost.ContentTypes as ContentTypes;
        if (contentTypes == null)
            return;

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
        if (operationTypes == null) return new List<Type>();
        var types = operationTypes
            .Where(x => x != null && !x.AllAttributes<ExcludeAttribute>()
                .Any(attr => attr.Feature.Has(Feature.Soap)))
            .Where(x => !x.IsGenericTypeDefinition)
            .ToList();
        return types;
    }

    public virtual bool ExportSoapType(Type type)
    {
        return type != null &&
               !type.IsGenericTypeDefinition &&
               !type.AllAttributes<ExcludeAttribute>()
                   .Any(attr => attr.Feature.Has(Feature.Soap));
    }

    public virtual void WriteSoapMessage(IRequest req, System.ServiceModel.Channels.Message message, Stream outputStream)
    {
        if (message == null || outputStream == null)
            return;

        try
        {
            using (var writer = XmlWriter.Create(outputStream, Config.XmlWriterSettings))
            {
                message.WriteMessage(writer);
            }
        }
        catch (Exception ex)
        {
            var response = req != null ? OnServiceException(req, req.Dto, ex).Result : null;
            if (response == null || outputStream == null || !outputStream.CanSeek)
                return;

            outputStream.Position = 0;
            try
            {
                var dtoType = req?.Dto?.GetType();
                var soapMsg = req?.GetSoapMessage();
                var isActionNull = soapMsg?.Headers?.Action == null;
                message = SoapHandler.CreateResponseMessage(response, message.Version, dtoType, isActionNull);
                using (var writer = XmlWriter.Create(outputStream, Config.XmlWriterSettings))
                {
                    message.WriteMessage(writer);
                }
            }
            catch { }
        }
        finally
        {
            if (req != null)
                HostContext.CompleteRequest(req);
        }
    }
}

#endif