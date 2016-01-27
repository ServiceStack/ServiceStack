//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack
{
    public interface IResponseStatus
    {
        string ErrorCode { get; set; }

        string ErrorMessage { get; set; }

        string StackTrace { get; set; }

        bool IsSuccess { get; }
    }
}