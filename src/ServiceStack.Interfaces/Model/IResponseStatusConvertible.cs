//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Model
{
    //Allow Exceptions to Customize ResponseStatus returned
    public interface IResponseStatusConvertible
    {
        ResponseStatus ToResponseStatus();
    }
}