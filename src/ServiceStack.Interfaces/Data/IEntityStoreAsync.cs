//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Data
{
    public interface IEntityStoreAsync
    {
        Task<T> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default);

        Task<IList<T>> GetByIdsAsync<T>(ICollection ids, CancellationToken cancellationToken = default);

        Task<T> StoreAsync<T>(T entity, CancellationToken cancellationToken = default);

        Task StoreAllAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default);

        Task DeleteByIdAsync<T>(object id, CancellationToken cancellationToken = default);

        Task DeleteByIdsAsync<T>(ICollection ids, CancellationToken cancellationToken = default);

        Task DeleteAllAsync<TEntity>(CancellationToken cancellationToken = default);
    }
}