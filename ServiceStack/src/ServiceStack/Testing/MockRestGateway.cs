// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;

namespace ServiceStack.Testing;

public delegate object RestGatewayDelegate(string httpVerb, Type responseType, object requestDto);

public class MockRestGateway : IRestGateway
{
    public RestGatewayDelegate ResultsFilter { get; set; }

    public T Send<T>(IReturn<T> request)
    {
        var method = request is IPost ?
            HttpMethods.Post
            : request is IPut ?
                HttpMethods.Put
                : request is IDelete ?
                    HttpMethods.Delete :
                    HttpMethods.Get;

        return ResultsFilter != null
            ? (T)ResultsFilter(method, typeof(T), request)
            : default(T);
    }

    public T Get<T>(IReturn<T> request)
    {
        return ResultsFilter != null
            ? (T)ResultsFilter(HttpMethods.Get, typeof(T), request)
            : default(T);
    }

    public T Post<T>(IReturn<T> request)
    {
        return ResultsFilter != null
            ? (T)ResultsFilter(HttpMethods.Post, typeof(T), request)
            : default(T);
    }

    public T Put<T>(IReturn<T> request)
    {
        return ResultsFilter != null
            ? (T)ResultsFilter(HttpMethods.Put, typeof(T), request)
            : default(T);
    }

    public T Delete<T>(IReturn<T> request)
    {
        return ResultsFilter != null
            ? (T)ResultsFilter(HttpMethods.Delete, typeof(T), request)
            : default(T);
    }
}