using System;
using System.Collections.Generic;

namespace ServiceStack.Web
{
    public interface IRestPath
    {
        bool IsWildCardPath { get; }

        Type RequestType { get; }

        object CreateRequest(string pathInfo, Dictionary<string, string> queryStringAndFormData, object fromInstance);
    }
}