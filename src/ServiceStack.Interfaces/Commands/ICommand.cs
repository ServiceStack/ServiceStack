//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Commands
{
    public interface ICommand<ReturnType>
    {
        ReturnType Execute();
    }
}