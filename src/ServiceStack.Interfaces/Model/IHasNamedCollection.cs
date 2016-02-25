//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Collections.Generic;

namespace ServiceStack.Model
{
    public interface IHasNamedCollection<T> : IHasNamed<ICollection<T>>
    {
    }
}