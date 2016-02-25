//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Model
{
    public interface IHasNamed<T>
    {
        T this[string listId] { get; set; }
    }
}