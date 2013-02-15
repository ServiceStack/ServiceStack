using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.Common.Web
{
    public class HttpResponseFilter : IContentTypeFilter
    {
        private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(false);

        public static HttpResponseFilter Instance = new HttpResponseFilter();

        public Dictionary<string, StreamSerializerDelegate> ContentTypeSerializers
            = new Dictionary<string, StreamSerializerDelegate>();

        public Dictionary<string, ResponseSerializerDelegate> ContentTypeResponseSerializers
            = new Dictionary<string, ResponseSerializerDelegate>();

        public Dictionary<string, StreamDeserializerDelegate> ContentTypeDeserializers
            = new Dictionary<string, StreamDeserializerDelegate>();

        public HttpResponseFilter()
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

        public string Serialize(string contentType, object response)
        {
            switch (contentType)
            {
                case ContentType.Xml:
                    return XmlSerializer.SerializeToString(response);

                case ContentType.Json:
                    return JsonDataContractSerializer.Instance.SerializeToString(response);

                case ContentType.Jsv:
                    return TypeSerializer.SerializeToString(response);

                default:
                    throw new NotSupportedException("ContentType not supported: " + contentType);
            }
        }

        public byte[] SerializeToBytes(IRequestContext requestContext, object response)
        {
            var contentType = requestContext.ResponseContentType;

            StreamSerializerDelegate responseStreamWriter;
            if (this.ContentTypeSerializers.TryGetValue(contentType, out responseStreamWriter) ||
                this.ContentTypeSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseStreamWriter))
            {
                using (var ms = new MemoryStream())
                {
                    responseStreamWriter(requestContext, response, ms);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }

            ResponseSerializerDelegate responseWriter;
            if (this.ContentTypeResponseSerializers.TryGetValue(contentType, out responseWriter) ||
                this.ContentTypeResponseSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseWriter))
            {
                using (var ms = new MemoryStream())
                {
                    var httpRes = new HttpResponseStreamWrapper(ms);
                    responseWriter(requestContext, response, httpRes);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }

            var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case EndpointAttributes.Xml:
                    return XmlSerializer.SerializeToString(response).ToUtf8Bytes();

                case EndpointAttributes.Json:
                    return JsonDataContractSerializer.Instance.SerializeToString(response).ToUtf8Bytes();

                case EndpointAttributes.Jsv:
                    return TypeSerializer.SerializeToString(response).ToUtf8Bytes();
            }

            throw new NotSupportedException("ContentType not supported: " + contentType);
        }

        public string SerializeToString(IRequestContext requestContext, object response)
        {
            var contentType = requestContext.ResponseContentType;

            StreamSerializerDelegate responseStreamWriter;
            if (this.ContentTypeSerializers.TryGetValue(contentType, out responseStreamWriter) ||
                this.ContentTypeSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseStreamWriter))
            {
                using (var ms = new MemoryStream())
                {
                    responseStreamWriter(requestContext, response, ms);

                    ms.Position = 0;
                    var result = new StreamReader(ms, UTF8EncodingWithoutBom).ReadToEnd();
                    return result;
                }
            }

            ResponseSerializerDelegate responseWriter;
            if (this.ContentTypeResponseSerializers.TryGetValue(contentType, out responseWriter) ||
                this.ContentTypeResponseSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseWriter))
            {
                using (var ms = new MemoryStream())
                {

                    var httpRes = new HttpResponseStreamWrapper(ms) {
                        KeepOpen = true, //Don't let view engines close the OutputStream
                    };
                    responseWriter(requestContext, response, httpRes);

                    ms.Position = 0;
                    var result = new StreamReader(ms, UTF8EncodingWithoutBom).ReadToEnd();

                    httpRes.ForceClose(); //Manually close the OutputStream

                    return result;
                }
            }


            var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case EndpointAttributes.Xml:
                    return XmlSerializer.SerializeToString(response);

                case EndpointAttributes.Json:
                    return JsonDataContractSerializer.Instance.SerializeToString(response);

                case EndpointAttributes.Jsv:
                    return TypeSerializer.SerializeToString(response);
            }

            throw new NotSupportedException("ContentType not supported: " + contentType);
        }

        public void SerializeToStream(IRequestContext requestContext, object response, Stream responseStream)
        {
            var contentType = requestContext.ResponseContentType;
            var serializer = GetResponseSerializer(contentType);
            if (serializer == null)
                throw new NotSupportedException("ContentType not supported: " + contentType);

            var httpRes = new HttpResponseStreamWrapper(responseStream);
            serializer(requestContext, response, httpRes);
        }

        public void SerializeToResponse(IRequestContext requestContext, object response, IHttpResponse httpResponse)
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
                this.ContentTypeResponseSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseWriter))
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
                this.ContentTypeSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseWriter))
            {
                return responseWriter;
            }

            var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case EndpointAttributes.Xml:
                    return (r, o, s) => XmlSerializer.SerializeToStream(o, s);

                case EndpointAttributes.Json:
                    return (r, o, s) => JsonDataContractSerializer.Instance.SerializeToStream(o, s);

                case EndpointAttributes.Jsv:
                    return (r, o, s) => TypeSerializer.SerializeToStream(o, s);
            }

            return null;
        }

        public object DeserializeFromString(string contentType, Type type, string request)
        {
            var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case EndpointAttributes.Xml:
                    return XmlSerializer.DeserializeFromString(request, type);

                case EndpointAttributes.Json:
                    return JsonDataContractDeserializer.Instance.DeserializeFromString(request, type);

                case EndpointAttributes.Jsv:
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
            var realContentType = contentType.Split(';')[0].Trim();
            if (this.ContentTypeDeserializers.TryGetValue(realContentType, out streamReader))
            {
                return streamReader;
            }

            var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
            switch (contentTypeAttr)
            {
                case EndpointAttributes.Xml:
                    return XmlSerializer.DeserializeFromStream;

                case EndpointAttributes.Json:
                    return JsonDataContractDeserializer.Instance.DeserializeFromStream;

                case EndpointAttributes.Jsv:
                    return TypeSerializer.DeserializeFromStream;
            }

            return null;
        }
    }
}