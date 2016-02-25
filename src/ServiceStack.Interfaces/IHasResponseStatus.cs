//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack
{
    /// <summary>
    /// Contract indication that the Response DTO has a ResponseStatus
    /// </summary>
    public interface IHasResponseStatus
    {
        ResponseStatus ResponseStatus { get; set; }
    }
}