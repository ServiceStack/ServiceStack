// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack
{
    public interface IRestGateway
    {
        T Send<T>(IReturn<T> request);
        T Get<T>(IReturn<T> request);
        T Post<T>(IReturn<T> request);
        T Put<T>(IReturn<T> request);
        T Delete<T>(IReturn<T> request);
    }
}