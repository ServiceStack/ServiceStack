using System;
using System.Collections.Generic;

namespace ServiceStack
{
    public static class ContentFormat
    {
        public const string Utf8Suffix = "; charset=utf-8";

        public static RequestAttributes GetEndpointAttributes(string contentType)
        {
            if (contentType == null)
                return RequestAttributes.None;

            if (contentType == MimeTypes.Soap11)
                return RequestAttributes.Soap11;

            var realContentType = GetRealContentType(contentType);
            switch (realContentType)
            {
                case MimeTypes.Json:
                case MimeTypes.JsonText:
                    return RequestAttributes.Json;

                case MimeTypes.Xml:
                case MimeTypes.XmlText:
                    return RequestAttributes.Xml;

                case MimeTypes.Html:
                    return RequestAttributes.Html;

                case MimeTypes.Jsv:
                case MimeTypes.JsvText:
                    return RequestAttributes.Jsv;

                case MimeTypes.Yaml:
                case MimeTypes.YamlText:
                    return RequestAttributes.FormatOther;

                case MimeTypes.Csv:
                    return RequestAttributes.Csv;

                case MimeTypes.Soap12:
                    return RequestAttributes.Soap12;

                case MimeTypes.ProtoBuf:
                    return RequestAttributes.ProtoBuf;

                case MimeTypes.MsgPack:
                    return RequestAttributes.MsgPack;

                case MimeTypes.Wire:
                    return RequestAttributes.Wire;
            }

            return RequestAttributes.FormatOther;
        }
        
        public static readonly Dictionary<string, string> ContentTypeAliases = new Dictionary<string, string> {
            { MimeTypes.JsonText, MimeTypes.Json },
            { MimeTypes.XmlText, MimeTypes.Xml },
            { MimeTypes.JsvText, MimeTypes.Jsv },
            { MimeTypes.YamlText, MimeTypes.Yaml },
        };

        public static string NormalizeContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return null;

            if (contentType == MimeTypes.Soap11)
                return MimeTypes.Soap11;

            var realContentType = GetRealContentType(contentType);
            return ContentTypeAliases.TryGetValue(realContentType, out var alias)
                ? alias
                : realContentType;
        }

        //lowercases and trims left part of content-type prior ';'
        public static string GetRealContentType(string contentType)
        {
            if (contentType == null)
                return null;

            int start = -1, end = -1;

            for(int i=0; i < contentType.Length; i++)
            {
                if (!char.IsWhiteSpace(contentType[i]))
                {
                    if (contentType[i] == ';')
                        break;
                    if (start == -1)
                    {
                        start = i;
                    }
                    end = i;
                }
            }

            return start != -1 
                    ? contentType.Substring(start, end - start + 1).ToLowerInvariant()
                    :  null;
        }

        //Compares two string from start to ';' char, case-insensitive,
        //ignoring (trimming) spaces at start and end
        public static bool MatchesContentType(this string contentType, string matchesContentType)
        {
            if (contentType == null || matchesContentType == null)
                return false;
            
            int start = -1, matchStart = -1, matchEnd = -1;

            for(int i=0; i < contentType.Length; i++)
            {
                if (!char.IsWhiteSpace(contentType[i]))
                {
                    start = i;
                    break;
                }
            }

            for(int i=0; i < matchesContentType.Length; i++)
            {
                if (!char.IsWhiteSpace(matchesContentType[i]))
                {
                    if (matchesContentType[i] == ';')
                        break;
                    if (matchStart == -1)
                        matchStart = i;
                    matchEnd = i;
                }
            }
            
            return start != -1 && matchStart != -1 && matchEnd != -1
                  && string.Compare(contentType, start,
                        matchesContentType, matchStart, matchEnd - matchStart + 1,
                        StringComparison.OrdinalIgnoreCase) == 0;
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
                case MimeTypes.Wire:
                    return true;
            }

            var primaryType = realContentType.LeftPart('/');
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

                case MimeTypes.Wire:
                    return Feature.Wire;
            }

            return Feature.CustomFormat;
        }

        public static string GetContentFormat(Format format)
        {
            if (format == Format.Soap11)
                return "soap11";
            if (format == Format.Soap12)
                return "soap12";
            
            var formatStr = format.ToString().ToLowerInvariant();
            return format == Format.MsgPack || format == Format.ProtoBuf
                ? "x-" + formatStr
                : formatStr;
        }

        public static string GetContentFormat(string contentType)
        {
            if (contentType == MimeTypes.Soap11)
                return "soap11";
            if (contentType == MimeTypes.Soap12)
                return "soap12";
            
            return contentType?.RightPart('/');
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

                case Format.Wire:
                    return MimeTypes.Wire;

                default:
                    return null;
            }
        }
        
        public static RequestAttributes GetRequestAttribute(string httpMethod)
        {
            switch (httpMethod.ToUpper())
            {
                case HttpMethods.Get:
                    return RequestAttributes.HttpGet;
                case HttpMethods.Put:
                    return RequestAttributes.HttpPut;
                case HttpMethods.Post:
                    return RequestAttributes.HttpPost;
                case HttpMethods.Delete:
                    return RequestAttributes.HttpDelete;
                case HttpMethods.Patch:
                    return RequestAttributes.HttpPatch;
                case HttpMethods.Head:
                    return RequestAttributes.HttpHead;
                case HttpMethods.Options:
                    return RequestAttributes.HttpOptions;
            }

            return RequestAttributes.HttpOther;
        }
    }

}