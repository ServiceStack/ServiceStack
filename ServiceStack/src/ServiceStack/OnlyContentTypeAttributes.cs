using System;
using ServiceStack.Web;

namespace ServiceStack;

public class HtmlOnly : RequestFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object requestDto) => req.ResponseContentType = MimeTypes.Html;
}

public class JsonOnly : RequestFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object requestDto) => req.ResponseContentType = MimeTypes.Json;
}

public class XmlOnly : RequestFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object requestDto) => req.ResponseContentType = MimeTypes.Xml;
}

public class JsvOnly : RequestFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object requestDto) => req.ResponseContentType = MimeTypes.Jsv;
}

public class CsvOnly : RequestFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object requestDto) => req.ResponseContentType = MimeTypes.Csv;
}
    
public class ReturnExceptionsInJsonAttribute : ResponseFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object responseDto)
    {
        if (responseDto is Exception || responseDto is IHttpError)
            req.ResponseContentType = MimeTypes.Json;
    }
}