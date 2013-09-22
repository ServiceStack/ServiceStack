using ServiceStack.Text;

namespace ServiceStack.Web
{
    public static class ContentFormat
    {
        public const string Utf8Suffix = "; charset=utf-8";

        public static EndpointAttributes GetEndpointAttributes(string contentType)
        {
            if (contentType == null)
                return EndpointAttributes.None;

            var realContentType = GetRealContentType(contentType);
            switch (realContentType)
            {
                case MimeTypes.Json:
                case MimeTypes.JsonText:
                    return EndpointAttributes.Json;

                case MimeTypes.Xml:
                case MimeTypes.XmlText:
                    return EndpointAttributes.Xml;

                case MimeTypes.Html:
                    return EndpointAttributes.Html;

                case MimeTypes.Jsv:
                case MimeTypes.JsvText:
                    return EndpointAttributes.Jsv;

                case MimeTypes.Yaml:
                case MimeTypes.YamlText:
                    return EndpointAttributes.Yaml;

                case MimeTypes.Csv:
                    return EndpointAttributes.Csv;

                case MimeTypes.Soap11:
                    return EndpointAttributes.Soap11;

                case MimeTypes.Soap12:
                    return EndpointAttributes.Soap12;

                case MimeTypes.ProtoBuf:
                    return EndpointAttributes.ProtoBuf;

                case MimeTypes.MsgPack:
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
                case MimeTypes.ProtoBuf:
                case MimeTypes.MsgPack:
                case MimeTypes.Binary:
                case MimeTypes.Bson:
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
                case MimeTypes.Json:
                case MimeTypes.JsonText:
                    return Feature.Json;

                case MimeTypes.Xml:
                case MimeTypes.XmlText:
                    return Feature.Xml;

                case MimeTypes.Html:
                    return Feature.Html;

                case MimeTypes.Jsv:
                case MimeTypes.JsvText:
                    return Feature.Jsv;

                case MimeTypes.Csv:
                    return Feature.Csv;

                case MimeTypes.Soap11:
                    return Feature.Soap11;

                case MimeTypes.Soap12:
                    return Feature.Soap12;

                case MimeTypes.ProtoBuf:
                    return Feature.ProtoBuf;

                case MimeTypes.MsgPack:
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
                    return MimeTypes.Xml;

                case Format.Json:
                    return MimeTypes.Json;

                case Format.Jsv:
                    return MimeTypes.JsvText;

                case Format.Csv:
                    return MimeTypes.Csv;

                case Format.ProtoBuf:
                    return MimeTypes.ProtoBuf;

                case Format.MsgPack:
                    return MimeTypes.MsgPack;

                case Format.Html:
                    return MimeTypes.Html;

                case Format.Yaml:
                    return MimeTypes.Yaml;

                default:
                    return null;
            }
        }
    }

}