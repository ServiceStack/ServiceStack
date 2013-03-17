using System;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Common.Web
{
    public static class ContentType
    {
        public const string Utf8Suffix = "; charset=utf-8";

        public const string HeaderContentType = "Content-Type";

        public const string FormUrlEncoded = "application/x-www-form-urlencoded";

        public const string MultiPartFormData = "multipart/form-data";

        public const string Html = "text/html";

        public const string JsonReport = "text/jsonreport";

        public const string Xml = "application/xml";

        public const string XmlText = "text/xml";

        public const string Soap11 = " text/xml; charset=utf-8";

        public const string Soap12 = " application/soap+xml";

        public const string Json = "application/json";

        public const string JsonText = "text/json";

        public const string JavaScript = "application/javascript";

        public const string Jsv = "application/jsv";

        public const string JsvText = "text/jsv";

        public const string Csv = "text/csv";

        public const string Yaml = "application/yaml";

        public const string YamlText = "text/yaml";

        public const string PlainText = "text/plain";

        public const string MarkdownText = "text/markdown";

        public const string ProtoBuf = "application/x-protobuf";

        public const string MsgPack = "application/x-msgpack";

        public const string Bson = "application/bson";

        public const string Binary = "application/octet-stream";

        public static EndpointAttributes GetEndpointAttributes(string contentType)
        {
            if (contentType == null)
                return EndpointAttributes.None;

            var realContentType = GetRealContentType(contentType);
            switch (realContentType)
            {
                case Json:
                case JsonText:
                    return EndpointAttributes.Json;

                case Xml:
                case XmlText:
                    return EndpointAttributes.Xml;

                case Html:
                    return EndpointAttributes.Html;

                case Jsv:
                case JsvText:
                    return EndpointAttributes.Jsv;

                case Yaml:
                case YamlText:
                    return EndpointAttributes.Yaml;

                case Csv:
                    return EndpointAttributes.Csv;

                case Soap11:
                    return EndpointAttributes.Soap11;

                case Soap12:
                    return EndpointAttributes.Soap12;

                case ProtoBuf:
                    return EndpointAttributes.ProtoBuf;

                case MsgPack:
                    return EndpointAttributes.MsgPack;

            }

            return EndpointAttributes.FormatOther;
        }

        public static string GetRealContentType(string contentType)
        {
            return contentType == null
                       ? null
                       : contentType.Split(';')[0].Trim();
        }

        public static bool MatchesContentType(this string contentType, string matchesContentType)
        {
            return GetRealContentType(contentType) == GetRealContentType(matchesContentType);
        }

        public static bool IsBinary(this string contentType)
        {
            var realContentType = GetRealContentType(contentType);
            switch (realContentType)
            {
                case ProtoBuf:
                case MsgPack:
                case Binary:
                case Bson:
                    return true;
            }

            var primaryType = realContentType.SplitOnFirst('/')[0];
            switch (primaryType)
            {
                case "image":
                case "audio":
                case "video":
                    return true;
            }

            return false;
        }

        public static Feature ToFeature(this string contentType)
        {
            if (contentType == null)
                return Feature.None;

            var realContentType = GetRealContentType(contentType);
            switch (realContentType)
            {
                case Json:
                case JsonText:
                    return Feature.Json;

                case Xml:
                case XmlText:
                    return Feature.Xml;

                case Html:
                    return Feature.Html;

                case Jsv:
                case JsvText:
                    return Feature.Jsv;

                case Csv:
                    return Feature.Csv;

                case Soap11:
                    return Feature.Soap11;

                case Soap12:
                    return Feature.Soap12;

                case ProtoBuf:
                    return Feature.ProtoBuf;

                case MsgPack:
                    return Feature.MsgPack;
            }

            return Feature.CustomFormat;
        }

        public static string GetContentFormat(Format format)
        {
            var formatStr = format.ToString().ToLower();
            return format == Format.MsgPack || format == Format.ProtoBuf 
                ? "x-" + formatStr 
                : formatStr;
        }

        public static string GetContentFormat(string contentType)
        {
            if (contentType == null) 
                return null;

            var parts = contentType.Split('/');
            return parts[parts.Length - 1];
        }

        public static string ToContentFormat(this string contentType)
        {
            return GetContentFormat(contentType);
        }

        public static string ToContentType(this Format formats)
        {
            switch (formats)
            {
                case Format.Soap11:
                case Format.Soap12:
                case Format.Xml:
                    return Xml;

                case Format.Json:
                    return Json;
                    
                case Format.Jsv:
                    return JsvText;

                case Format.Csv:
                    return Csv;

                case Format.ProtoBuf:
                    return ProtoBuf;

                case Format.MsgPack:
                    return MsgPack;

                case Format.Html:
                    return Html;

                case Format.Yaml:
                    return Yaml;
                
                default:
                    return null;
            }
        }
    }

}