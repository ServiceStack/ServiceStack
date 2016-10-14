#if !NETSTANDARD1_6
// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using System.Xml;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Messaging;
using ServiceStack.Metadata;
using ServiceStack.MiniProfiler;
using ServiceStack.Redis;
using ServiceStack.Serialization;
using ServiceStack.Support.WebHost;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost
    {
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
                var response = OnServiceException(req, req.Dto, ex);
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
