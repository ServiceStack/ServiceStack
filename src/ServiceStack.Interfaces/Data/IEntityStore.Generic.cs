//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.Data
{
    /// <summary>
    /// For providers that want a cleaner API with a little more perf
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityStore<T>
    {
        T GetById(object id);

        IList<T> GetByIds(IEnumerable ids);

        IList<T> GetAll();

        T Store(T entity);

        void StoreAll(IEnumerable<T> entities);

        void Delete(T entity);

        void DeleteById(object id);

        void DeleteByIds(IEnumerable ids);

        void DeleteAll();
    }
}