using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
            = new Dictionary<string, StreamSerializerDelegate> {
                { MimeTypes.Json, (r, o, s) => JsonDataContractSerializer.Instance.SerializeToStream(o, s) },
                { MimeTypes.Jsv, (r, o, s) => TypeSerializer.SerializeToStream(o, s) },
                { MimeTypes.Xml, (r, o, s) => XmlSerializer.SerializeToStream(o, s) },
            };

        public Dictionary<string, StreamDeserializerDelegate> ContentTypeDeserializers
            = new Dictionary<string, StreamDeserializerDelegate> {
                { MimeTypes.Json, JsonDataContractSerializer.Instance.DeserializeFromStream },
                { MimeTypes.Jsv, TypeSerializer.DeserializeFromStream },
                { MimeTypes.Xml, XmlSerializer.DeserializeFromStream },
                { "text/xml; charset=utf-8", XmlSerializer.DeserializeFromStream }, //"text/xml; charset=utf-8" matches xml + soap11
            };

        public Dictionary<string, StreamSerializerDelegateAsync> ContentTypeSerializersAsync
            = new Dictionary<string, StreamSerializerDelegateAsync>();

        public Dictionary<string, StreamDeserializerDelegateAsync> ContentTypeDeserializersAsync
            = new Dictionary<string, StreamDeserializerDelegateAsync>();

        public Dictionary<string, StringSerializerDelegate> ContentTypeStringSerializers
            = new Dictionary<string, StringSerializerDelegate> {
                { MimeTypes.Json, (r, o) => JsonDataContractSerializer.Instance.SerializeToString(o) },
                { MimeTypes.Jsv, (r, o) => TypeSerializer.SerializeToString(o) },
                { MimeTypes.Xml, (r, o) => XmlSerializer.SerializeToString(o) },
            };

        public Dictionary<string, StringDeserializerDelegate> ContentTypeStringDeserializers
            = new Dictionary<string, StringDeserializerDelegate> {
                { MimeTypes.Json, JsonDataContractSerializer.Instance.DeserializeFromString },
                { MimeTypes.Jsv, TypeSerializer.DeserializeFromString },
                { MimeTypes.Xml, XmlSerializer.DeserializeFromString },
            };

        public static StreamSerializerDelegateAsync UnknownContentTypeSerializer { get; set; } = SerializeUnknownContentType;
        public static StreamDeserializerDelegateAsync UnknownContentTypeDeserializer { get; set; }

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

        //built-in formats
        public Dictionary<string, string> ContentTypeFormats { get; } = new Dictionary<string, string> {
            { "json", MimeTypes.Json },
            { "xml", MimeTypes.Xml },
            { "jsv", MimeTypes.Jsv },
        };

        public string GetFormatContentType(string format)
        {
            return ContentTypeFormats.TryGetValue(format, out var registeredFormats)
                ? registeredFormats
                : null;
        }

        public void Register(string contentType, StreamSerializerDelegate streamSerializer, StreamDeserializerDelegate streamDeserializer)
        {
            if (contentType.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(contentType));

            var format = ContentFormat.GetContentFormat(contentType);
            
            var normalizedContentType = ContentFormat.NormalizeContentType(contentType);
            ContentTypeFormats[format] = normalizedContentType;

            SetContentTypeSerializer(normalizedContentType, streamSerializer);
            SetContentTypeDeserializer(normalizedContentType, streamDeserializer);
        }

        public void RegisterAsync(string contentType, 
            StreamSerializerDelegateAsync streamSerializer, StreamDeserializerDelegateAsync streamDeserializer)
        {
            if (contentType.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(contentType));

            var format = ContentFormat.GetContentFormat(contentType);
            
            var normalizedContentType = ContentFormat.NormalizeContentType(contentType);
            ContentTypeFormats[format] = normalizedContentType;

            ContentTypeSerializersAsync[normalizedContentType] = streamSerializer;
            ContentTypeDeserializersAsync[normalizedContentType] = streamDeserializer;
        }

        public void Remove(string contentType)
        {
            contentType = ContentFormat.NormalizeContentType(contentType);

            ContentTypeFormats.Remove(contentType);
            
            ContentTypeSerializers.Remove(contentType);
            ContentTypeSerializersAsync.Remove(contentType);
            ContentTypeStringSerializers.Remove(contentType);

            ContentTypeDeserializers.Remove(contentType);
            ContentTypeDeserializersAsync.Remove(contentType);
            ContentTypeStringDeserializers.Remove(contentType);

            if (contentType == MimeTypes.Xml)
            {
                //"text/xml; charset=utf-8" also matches xml
                ContentTypeSerializers.Remove(MimeTypes.Soap11);
                ContentTypeDeserializers.Remove(MimeTypes.Soap11);
            }
        }

        public void SetContentTypeSerializer(string contentType, StreamSerializerDelegate streamSerializer)
        {
            ContentTypeSerializers[ContentFormat.NormalizeContentType(contentType)] = streamSerializer;
        }

        public void SetContentTypeDeserializer(string contentType, StreamDeserializerDelegate streamDeserializer)
        {
            ContentTypeDeserializers[ContentFormat.NormalizeContentType(contentType)] = streamDeserializer;
        }

        public static async Task SerializeUnknownContentType(IRequest req, object response, Stream stream)
        {
            switch (response) 
            {
                case string text:
                    await stream.WriteAsync(text);
                    break;
                case byte[] bytes:
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    break;
                case Stream input:
                    await input.CopyToAsync(stream);
                    break;
#if !NETSTANDARD2_0
                case System.Drawing.Image img:
                    img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    break;
#endif
                default:
                    throw new NotSupportedException(ErrorMessages.ContentTypeNotSupported.Fmt(req.ResponseContentType));
            }
        }
        
        public byte[] SerializeToBytes(IRequest req, object response)
        {
            var contentType = ContentFormat.NormalizeContentType(req.ResponseContentType);

            var responseStreamWriter = GetStreamSerializer(contentType);
            if (responseStreamWriter != null)
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseStreamWriter(req, response, ms);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }

            var responseWriterAsync = GetStreamSerializerAsync(contentType);
            if (responseWriterAsync != null)
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseWriterAsync(req, response, ms).Wait();
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }

            throw new NotSupportedException(ErrorMessages.ContentTypeNotSupported.Fmt(contentType));
        }

        public string SerializeToString(IRequest req, object response)
        {
            var contentType = ContentFormat.NormalizeContentType(req.ResponseContentType);

            if (ContentTypeStringSerializers.TryGetValue(contentType, out var stringSerializer))
            {
                return stringSerializer(req, response);
            }

            var responseStreamWriter = GetStreamSerializer(contentType);
            if (responseStreamWriter != null)
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseStreamWriter(req, response, ms);

                    ms.Position = 0;
                    var result = new StreamReader(ms, UTF8EncodingWithoutBom).ReadToEnd();
                    return result;
                }
            }

            var responseWriterAsync = GetStreamSerializerAsync(contentType);
            if (responseWriterAsync != null)
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    responseWriterAsync(req, response, ms).Wait();

                    var bytes = ms.ToArray();
                    var result = bytes.FromUtf8Bytes();

                    return result;
                }
            }

            throw new NotSupportedException(ErrorMessages.ContentTypeNotSupported.Fmt(contentType));
        }
        
        public async Task SerializeToStreamAsync(IRequest req, object response, Stream responseStream)
        {
            var contentType = ContentFormat.NormalizeContentType(req.ResponseContentType);

            var serializer = GetStreamSerializer(contentType);
            if (serializer != null)
            {
                serializer(req, response, responseStream);
                return;
            }

            var serializerAsync = GetStreamSerializerAsync(contentType);
            if (serializerAsync != null)
            {
                await serializerAsync(req, response, responseStream);
                return;
            }
            
            throw new NotSupportedException(ErrorMessages.ContentTypeNotSupported.Fmt(contentType));
        }

        public StreamSerializerDelegateAsync GetStreamSerializerAsync(string contentType)
        {
            contentType = ContentFormat.NormalizeContentType(contentType);
            
            if (ContentTypeSerializersAsync.TryGetValue(contentType, out var serializerAsync))
                return serializerAsync;

            var serializer = GetStreamSerializer(contentType);
            if (serializer == null) 
                return UnknownContentTypeSerializer;

            return (httpReq, dto, stream) =>
            {
                serializer(httpReq, dto, stream);
                return TypeConstants.EmptyTask;
            };
        }

        public StreamSerializerDelegate GetStreamSerializer(string contentType)
        {
            return ContentTypeSerializers.TryGetValue(ContentFormat.NormalizeContentType(contentType), out var serializer) 
                ? serializer 
                : null;
        }

        public object DeserializeFromString(string contentType, Type type, string request)
        {
            contentType = ContentFormat.NormalizeContentType(contentType);

            if (ContentTypeStringDeserializers.TryGetValue(contentType, out var stringDeserializer))
                return stringDeserializer(request, type);

            var deserializerAsync = GetStreamDeserializerAsync(contentType);
            if (deserializerAsync != null)
            {
                using (var ms = MemoryStreamFactory.GetStream(request.ToUtf8Bytes()))
                {
                    var task = deserializerAsync(type, ms);
                    return task.Result;
                }
            }
            
            throw new NotSupportedException(ErrorMessages.ContentTypeNotSupported.Fmt(contentType));
        }

        public object DeserializeFromStream(string contentType, Type type, Stream fromStream)
        {
            contentType = ContentFormat.NormalizeContentType(contentType);

            var deserializer = GetStreamDeserializer(contentType);
            if (deserializer != null)
                return deserializer(type, fromStream);

            var deserializerAsync = GetStreamDeserializerAsync(contentType);
            if (deserializerAsync != null)
            {
                var task = deserializerAsync(type, fromStream);
                return task.Result;
            }
            
            throw new NotSupportedException(ErrorMessages.ContentTypeNotSupported.Fmt(contentType));
        }

        public StreamDeserializerDelegate GetStreamDeserializer(string contentType)
        {
            return ContentTypeDeserializers.TryGetValue(ContentFormat.NormalizeContentType(contentType), out var deserializer) 
                ? deserializer 
                : null;
        }
 
        public StreamDeserializerDelegateAsync GetStreamDeserializerAsync(string contentType)
        {
            contentType = ContentFormat.NormalizeContentType(contentType);
            
            if (ContentTypeDeserializersAsync.TryGetValue(contentType, out var deserializerAsync))
                return deserializerAsync;

            var deserializer = GetStreamDeserializer(contentType);
            if (deserializer == null) 
                return UnknownContentTypeDeserializer;

            return (type, stream) => Task.FromResult(deserializer(type, stream));
        }
    }
}