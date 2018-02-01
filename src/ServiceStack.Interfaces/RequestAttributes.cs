//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;

namespace ServiceStack
{
    [Flags]
    public enum RequestAttributes : long
    {
        None = 0,

        Any = AnyNetworkAccessType | AnySecurityMode | AnyHttpMethod | AnyCallStyle | AnyFormat | AnyEndpoint,
        AnyNetworkAccessType = External | LocalSubnet | Localhost | InProcess,
        AnySecurityMode = Secure | InSecure,
        AnyHttpMethod = HttpHead | HttpGet | HttpPost | HttpPut | HttpDelete | HttpPatch | HttpOptions | HttpOther,
        AnyCallStyle = OneWay | Reply,
        AnyFormat = Soap11 | Soap12 | Xml | Json | Jsv | Html | ProtoBuf | Csv | MsgPack | Wire | FormatOther,
        AnyEndpoint = Http | MessageQueue | Tcp | EndpointOther,
        InternalNetworkAccess = InProcess | Localhost | LocalSubnet,

        //Whether it came from an Internal or External address
        Localhost = 1 << 0,
        LocalSubnet = 1 << 1,
        External = 1 << 2,

        //Called over a secure or insecure channel
        Secure = 1 << 3,
        InSecure = 1 << 4,

        //HTTP request type
        HttpHead = 1 << 5,
        HttpGet = 1 << 6,
        HttpPost = 1 << 7,
        HttpPut = 1 << 8,
        HttpDelete = 1 << 9,
        HttpPatch = 1 << 10,
        HttpOptions = 1 << 11,
        HttpOther = 1 << 12,

        //Call Styles
        OneWay = 1 << 13,
        Reply = 1 << 14,

        //Different formats
        Soap11 = 1 << 15,
        Soap12 = 1 << 16,
        //POX
        Xml = 1 << 17,
        //Javascript
        Json = 1 << 18,
        //Jsv i.e. TypeSerializer
        Jsv = 1 << 19,
        //e.g. protobuf-net
        ProtoBuf = 1 << 20,
        //e.g. text/csv
        Csv = 1 << 21,
        Html = 1 << 22,
        Wire = 1 << 23,
        MsgPack = 1 << 24,
        FormatOther = 1 << 25,

        //Different endpoints
        Http = 1 << 26,
        MessageQueue = 1 << 27,
        Tcp = 1 << 28,
        EndpointOther = 1 << 29,

        InProcess = 1 << 30, //Service was executed within code (e.g. ResolveService<T>)
    }

    public enum Network : long
    {
        Localhost = 1 << 0,
        LocalSubnet = 1 << 1,
        External = 1 << 2,
    }

    public enum Security : long
    {
        Secure = 1 << 3,
        InSecure = 1 << 4,
    }

    public enum Http : long
    {
        Head = 1 << 5,
        Get = 1 << 6,
        Post = 1 << 7,
        Put = 1 << 8,
        Delete = 1 << 9,
        Patch = 1 << 10,
        Options = 1 << 11,
        Other = 1 << 12,
    }

    public enum CallStyle : long
    {
        OneWay = 1 << 13,
        Reply = 1 << 14,
    }

    public enum Format : long
    {
        Soap11 = 1 << 15,
        Soap12 = 1 << 16,
        Xml = 1 << 17,
        Json = 1 << 18,
        Jsv = 1 << 19,
        ProtoBuf = 1 << 20,
        Csv = 1 << 21,
        Html = 1 << 22,
        Wire = 1 << 23,
        MsgPack = 1 << 24,
        Other = 1 << 25,
    }

    public enum Endpoint : long
    {
        Http = 1 << 26,
        Mq = 1 << 27,
        Tcp = 1 << 28,
        Other = 1 << 29,
    }

    public static class RequestAttributesExtensions
    {
        public static bool IsLocalhost(this RequestAttributes attrs)
        {
            return (RequestAttributes.Localhost & attrs) == RequestAttributes.Localhost;
        }

        public static bool IsLocalSubnet(this RequestAttributes attrs)
        {
            return (RequestAttributes.LocalSubnet & attrs) == RequestAttributes.LocalSubnet;
        }

        public static bool IsExternal(this RequestAttributes attrs)
        {
            return (RequestAttributes.External & attrs) == RequestAttributes.External;
        }

        public static Format ToFormat(this string format)
        {
            try
            {
                return (Format)Enum.Parse(typeof(Format), format.ToUpper().Replace("X-", ""), true);
            }
            catch (Exception)
            {
                return Format.Other;
            }
        }

        public static string FromFormat(this Format format)
        {
            var formatStr = format.ToString().ToLowerInvariant();
            if (format == Format.ProtoBuf || format == Format.MsgPack)
                return "x-" + formatStr;
            return formatStr;
        }

        public static Format ToFormat(this Feature feature)
        {
            switch (feature)
            {
                case Feature.Xml:
                    return Format.Xml;
                case Feature.Json:
                    return Format.Json;
                case Feature.Jsv:
                    return Format.Jsv;
                case Feature.Csv:
                    return Format.Csv;
                case Feature.Html:
                    return Format.Html;
                case Feature.MsgPack:
                    return Format.MsgPack;
                case Feature.ProtoBuf:
                    return Format.ProtoBuf;
                case Feature.Soap11:
                    return Format.Soap11;
                case Feature.Soap12:
                    return Format.Soap12;
            }
            return Format.Other;
        }

        public static Feature ToFeature(this Format format)
        {
            switch (format)
            {
                case Format.Xml:
                    return Feature.Xml;
                case Format.Json:
                    return Feature.Json;
                case Format.Jsv:
                    return Feature.Jsv;
                case Format.Csv:
                    return Feature.Csv;
                case Format.Html:
                    return Feature.Html;
                case Format.MsgPack:
                    return Feature.MsgPack;
                case Format.ProtoBuf:
                    return Feature.ProtoBuf;
                case Format.Soap11:
                    return Feature.Soap11;
                case Format.Soap12:
                    return Feature.Soap12;
            }
            return Feature.CustomFormat;
        }

        public static Feature ToSoapFeature(this RequestAttributes attributes)
        {
            if ((RequestAttributes.Soap11 & attributes) == RequestAttributes.Soap11)
                return Feature.Soap11;
            if ((RequestAttributes.Soap12 & attributes) == RequestAttributes.Soap12)
                return Feature.Soap12;            
            return Feature.None;
        }
    }
}