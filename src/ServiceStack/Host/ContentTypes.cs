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

        public Dictionary<string, StreamDeserializerDelegate> ContentTypeDeserializers
            = new Dictionary<string, StreamDeserializerDelegate>();

        public Dictionary<string, StreamSerializerDelegateAsync> ContentTypeSerializersAsync
            = new Dictionary<string, StreamSerializerDelegateAsync>();

        public Dictionary<string, StreamDeserializerDelegateAsync> ContentTypeDeserializersAsync
            = new Dictionary<string, StreamDeserializerDelegateAsync>();

        public static HashSet<string> KnownFormats = new HashSet<string>
        {
            "json",
            "xml",
            "jsv",
            "csv",
            "html",
            "protobuf",
            "msgpack",
            "wire",
        };

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

            ContentTypeFormats.TryGetValue(format, out var registeredFormats);

            return registeredFormats;
        }

        public void Register(string contentType, StreamSerializerDelegate streamSerializer, StreamDeserializerDelegate streamDeserializer)
        {
            if (contentType.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(contentType));

            var parts = contentType.Split('/');
            var format = parts[parts.Length - 1];
            this.ContentTypeFormats[format] = contentType;

            SetContentTypeSerializer(contentType, streamSerializer);
            SetContentTypeDeserializer(contentType, streamDeserializer);
        }

        public void RegisterAsync(string contentType, 
            StreamSerializerDelegateAsync streamSerializer, StreamDeserializerDelegateAsync streamDeserializer)
        {
            if (contentType.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(contentType));

            var parts = contentType.Split('/');
            var format = parts[parts.Length - 1];
            this.ContentTypeFormats[format] = contentType;

            this.ContentTypeSerializersAsync[contentType] = streamSerializer;
            this.ContentTypeDeserializersAsync[contentType] = streamDeserializer;
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

            if (this.ContentTypeSerializers.TryGetValue(contentType, out var responseStreamWriter) ||
                this.ContentTypeSerializers.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseStreamWriter))
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseStreamWriter(req, response, ms);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }

            if (this.ContentTypeSerializersAsync.TryGetValue(contentType, out var responseWriterAsync) ||
                this.ContentTypeSerializersAsync.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseWriterAsync))
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseWriterAsync(req, response, ms).Wait();
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
#if !NETSTANDARD2_0
                case RequestAttributes.Soap11:
                    return SoapHandler.SerializeSoap11ToBytes(req, response);

                case RequestAttributes.Soap12:
                    return SoapHandler.SerializeSoap12ToBytes(req, response);
#endif
            }

            throw new NotSupportedException("ContentType not supported: " + contentType);
        }

        public string SerializeToString(IRequest req, object response)
        {
            var contentType = req.ResponseContentType;

            if (this.ContentTypeSerializers.TryGetValue(contentType, out var responseStreamWriter) ||
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

            if (this.ContentTypeSerializersAsync.TryGetValue(contentType, out var responseWriter) ||
                this.ContentTypeSerializersAsync.TryGetValue(ContentFormat.GetRealContentType(contentType), out responseWriter))
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseWriter(req, response, ms).Wait();

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

#if !NETSTANDARD2_0
                case RequestAttributes.Soap11:
                    return SoapHandler.SerializeSoap11ToBytes(req, response).FromUtf8Bytes();

                case RequestAttributes.Soap12:
                    return SoapHandler.SerializeSoap12ToBytes(req, response).FromUtf8Bytes();
#endif
            }

            throw new NotSupportedException("ContentType not supported: " + contentType);
        }

        public void SerializeToStream(IRequest req, object response, Stream responseStream)
        {
            var contentType = req.ResponseContentType;
            var serializerAsync = GetStreamSerializerAsync(contentType);
            if (serializerAsync == null)
                throw new NotSupportedException("ContentType not supported: " + contentType);

            serializerAsync(req, response, responseStream).Wait();
        }

        public StreamSerializerDelegateAsync GetStreamSerializerAsync(string contentType)
        {
            if (this.ContentTypeSerializersAsync.TryGetValue(contentType, out var serializerAsync) ||
                this.ContentTypeSerializersAsync.TryGetValue(ContentFormat.GetRealContentType(contentType), out serializerAsync))
            {
                return serializerAsync;
            }

            var serializer = GetStreamSerializer(contentType);
            if (serializer == null) return null;

            return (httpReq, dto, stream) =>
            {
                serializer(httpReq, dto, stream);
                return TypeConstants.EmptyTask;
            };
        }

        public StreamSerializerDelegate GetStreamSerializer(string contentType)
        {
            if (this.ContentTypeSerializers.TryGetValue(contentType, out var responseWriter) ||
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

#if !NETSTANDARD2_0
                case RequestAttributes.Soap11:
                    return SoapHandler.SerializeSoap11ToStream;

                case RequestAttributes.Soap12:
                    return SoapHandler.SerializeSoap12ToStream;
#endif
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
            var realContentType = ContentFormat.GetRealContentType(contentType);
            if (this.ContentTypeDeserializers.TryGetValue(realContentType, out var streamReader))
                return streamReader;

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