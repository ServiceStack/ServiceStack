//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.Data
{
    public interface IEntityStore : IDisposable
    {
        T GetById<T>(object id);

        IList<T> GetByIds<T>(ICollection ids);

        T Store<T>(T entity);

        void StoreAll<TEntity>(IEnumerable<TEntity> entities);

        void Delete<T>(T entity);

        void DeleteById<T>(object id);

        void DeleteByIds<T>(ICollection ids);

        void DeleteAll<TEntity>();
    }
}