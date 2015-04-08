using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Host.Handlers;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class ContentTypes : IContentTypes
    {
        private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(false);

        public static ContentTypes Instance = new ContentTypes();

        public Dictionary<string, StreamSerializerDelegate> ContentTypeSerializers
            = new Dictionary<string, StreamSerializerDelegate>();

        public Dictionary<string, ResponseSerializerDelegate> ContentTypeResponseSerializers
            = new Dictionary<string, ResponseSerializerDelegate>();

        public Dictionary<string, StreamDeserializerDelegate> ContentTypeDeserializers
            = new Dictionary<string, StreamDeserializerDelegate>();

        public ContentTypes()
        {
            this.ContentTypeFormats = new Dictionary<string, string>();
        }

        public void ClearCustomFilters()
        {
            this.ContentTypeFormats = new Dictionary<string, string>();
            this.ContentTypeSerializers = new Dictionary<string, StreamSerializerDelegate>();
            this.ContentTypeDeserializers = new Dictionary<string, StreamDeserializerDelegate>();
        }

        public Dictionary<string, string> ContentTypeFormats { get; set; }

        public string GetFormatContentType(string format)
        {
            //built-in formats
            if (format == "json")
                return MimeTypes.Json;
            if (format == "xml")
                return MimeTypes.Xml;
            if (format == "jsv")
                return MimeTypes.Jsv;

            string registeredFormats;
            ContentTypeFormats.TryGetValue(format, out registeredFormats);

            return registeredFormats;
        }

        public void Register(string contentType, StreamSerializerDelegate streamSerializer, StreamDeserializerDelegate streamDeserializer)
        {
            if (contentType.IsNullOrEmpty())
                throw new ArgumentNullException("contentType");

            var parts = contentType.Split('/');
            var format = parts[parts.Length - 1];
            this.ContentTypeFormats[format] = contentType;

            SetContentTypeSerializer(contentType, streamSerializer);
            SetContentTypeDeserializer(contentType, streamDeserializer);
        }

        public void Register(string contentType, ResponseSerializerDelegate responseSerializer,
                             StreamDeserializerDelegate streamDeserializer)
        {
            if (contentType.IsNullOrEmpty())
                throw new ArgumentNullException("contentType");

            var parts = contentType.Split('/');
            var format = parts[parts.Length - 1];
            this.ContentTypeFormats[format] = contentType;

            this.ContentTypeResponseSerializers[contentType] = responseSerializer;
            SetContentTypeDeserializer(contentType, streamDeserializer);
        }

        public void SetContentTypeSerializer(string contentType, StreamSerializerDelegate streamSerializer)
        {
            this.ContentTypeSerializers[contentType] = streamSerializer;
        }

        public void SetContentTypeDeserializer(string contentType, StreamDeserializerDelegate streamDeserializer)
        {
            this.ContentTypeDeserializers[contentType] = streamDeserializer;
        }
        
        public byte[] SerializeToBytes(IRequest req, object response)
        {
            var contentType = req.ResponseContentType;

            StreamSerializerDelegate responseStreamWriter;
            if (this.ContentTypeSerializers.TryGetValue(contentType, out responseStreamWriter) ||
                this.ContentTypeSerializers.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseStreamWriter))
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseStreamWriter(req, response, ms);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }

            ResponseSerializerDelegate responseWriter;
            if (this.ContentTypeResponseSerializers.TryGetValue(contentType, out responseWriter) ||
                this.ContentTypeResponseSerializers.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseWriter))
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    var httpRes = new HttpResponseStreamWrapper(ms, req);
                    responseWriter(req, response, httpRes);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }

            var contentTypeAttr = ContentFormat.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case RequestAttributes.Xml:
                    return XmlSerializer.SerializeToString(response).ToUtf8Bytes();

                case RequestAttributes.Json:
                    return JsonDataContractSerializer.Instance.SerializeToString(response).ToUtf8Bytes();

                case RequestAttributes.Jsv:
                    return TypeSerializer.SerializeToString(response).ToUtf8Bytes();

                case RequestAttributes.Soap11:
                    return SoapHandler.SerializeSoap11ToBytes(req, response);

                case RequestAttributes.Soap12:
                    return SoapHandler.SerializeSoap12ToBytes(req, response);
            }

            throw new NotSupportedException("ContentType not supported: " + contentType);
        }

        public string SerializeToString(IRequest req, object response)
        {
            var contentType = req.ResponseContentType;

            StreamSerializerDelegate responseStreamWriter;
            if (this.ContentTypeSerializers.TryGetValue(contentType, out responseStreamWriter) ||
                this.ContentTypeSerializers.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseStreamWriter))
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseStreamWriter(req, response, ms);

                    ms.Position = 0;
                    var result = new StreamReader(ms, UTF8EncodingWithoutBom).ReadToEnd();
                    return result;
                }
            }

            ResponseSerializerDelegate responseWriter;
            if (this.ContentTypeResponseSerializers.TryGetValue(contentType, out responseWriter) ||
                this.ContentTypeResponseSerializers.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseWriter))
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    var httpRes = new HttpResponseStreamWrapper(ms, req) {
                        KeepOpen = true, //Don't let view engines close the OutputStream
                    };
                    responseWriter(req, response, httpRes);

                    var bytes = ms.ToArray();
                    var result = bytes.FromUtf8Bytes();

                    return result;
                }
            }
            
            var contentTypeAttr = ContentFormat.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case RequestAttributes.Xml:
                    return XmlSerializer.SerializeToString(response);

                case RequestAttributes.Json:
                    return JsonDataContractSerializer.Instance.SerializeToString(response);

                case RequestAttributes.Jsv:
                    return TypeSerializer.SerializeToString(response);

                case RequestAttributes.Soap11:
                    return SoapHandler.SerializeSoap11ToBytes(req, response).FromUtf8Bytes();

                case RequestAttributes.Soap12:
                    return SoapHandler.SerializeSoap12ToBytes(req, response).FromUtf8Bytes();
            }

            throw new NotSupportedException("ContentType not supported: " + contentType);
        }

        public void SerializeToStream(IRequest req, object response, Stream responseStream)
        {
            var contentType = req.ResponseContentType;
            var serializer = GetResponseSerializer(contentType);
            if (serializer == null)
                throw new NotSupportedException("ContentType not supported: " + contentType);

            var httpRes = new HttpResponseStreamWrapper(responseStream, req) {
                Dto = req.Response.Dto
            };
            serializer(req, response, httpRes);
        }

        public void SerializeToResponse(IRequest requestContext, object response, IResponse httpResponse)
        {
            var contentType = requestContext.ResponseContentType;
            var serializer = GetResponseSerializer(contentType);
            if (serializer == null)
                throw new NotSupportedException("ContentType not supported: " + contentType);

            serializer(requestContext, response, httpResponse);
        }

        public ResponseSerializerDelegate GetResponseSerializer(string contentType)
        {
            ResponseSerializerDelegate responseWriter;
            if (this.ContentTypeResponseSerializers.TryGetValue(contentType, out responseWriter) ||
                this.ContentTypeResponseSerializers.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseWriter))
            {
                return responseWriter;
            }

            var serializer = GetStreamSerializer(contentType);
            if (serializer == null) return null;

            return (httpReq, dto, httpRes) => serializer(httpReq, dto, httpRes.OutputStream);
        }

        public StreamSerializerDelegate GetStreamSerializer(string contentType)
        {
            StreamSerializerDelegate responseWriter;
            if (this.ContentTypeSerializers.TryGetValue(contentType, out responseWriter) ||
                this.ContentTypeSerializers.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseWriter))
            {
                return responseWriter;
            }

            var contentTypeAttr = ContentFormat.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case RequestAttributes.Xml:
                    return (r, o, s) => XmlSerializer.SerializeToStream(o, s);

                case RequestAttributes.Json:
                    return (r, o, s) => JsonDataContractSerializer.Instance.SerializeToStream(o, s);

                case RequestAttributes.Jsv:
                    return (r, o, s) => TypeSerializer.SerializeToStream(o, s);

                case RequestAttributes.Soap11:
                    return SoapHandler.SerializeSoap11ToStream;

                case RequestAttributes.Soap12:
                    return SoapHandler.SerializeSoap12ToStream;
            }

            return null;
        }

        public object DeserializeFromString(string contentType, Type type, string request)
        {
            var contentTypeAttr = ContentFormat.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case RequestAttributes.Xml:
                    return XmlSerializer.DeserializeFromString(request, type);

                case RequestAttributes.Json:
                    return JsonDataContractSerializer.Instance.DeserializeFromString(request, type);

                case RequestAttributes.Jsv:
                    return TypeSerializer.DeserializeFromString(request, type);

                default:
                    throw new NotSupportedException("ContentType not supported: " + contentType);
            }
        }

        public object DeserializeFromStream(string contentType, Type type, Stream fromStream)
        {
            var deserializer = GetStreamDeserializer(contentType);
            if (deserializer == null)
                throw new NotSupportedException("ContentType not supported: " + contentType);

            return deserializer(type, fromStream);
        }

        public StreamDeserializerDelegate GetStreamDeserializer(string contentType)
        {
            StreamDeserializerDelegate streamReader;
            var realContentType = ContentFormat.GetRealContentType(contentType);
            if (this.ContentTypeDeserializers.TryGetValue(realContentType, out streamReader))
            {
                return streamReader;
            }

            var contentTypeAttr = ContentFormat.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case RequestAttributes.Xml:
                case RequestAttributes.Soap11: //"text/xml; charset=utf-8" also matches xml
                    return XmlSerializer.DeserializeFromStream;

                case RequestAttributes.Json:
                    return JsonDataContractSerializer.Instance.DeserializeFromStream;

                case RequestAttributes.Jsv:
                    return TypeSerializer.DeserializeFromStream;
            }

            return null;
        }
    }
}