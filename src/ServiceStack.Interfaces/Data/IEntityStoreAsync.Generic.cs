//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Data
{
    /// <summary>
    /// For providers that want a cleaner API with a little more perf
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityStoreAsync<T>
    {
        Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        Task<IList<T>> GetByIdsAsync(IEnumerable ids, CancellationToken cancellationToken = default);

        Task<IList<T>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<T> StoreAsync(T entity, CancellationToken cancellationToken = default);

        Task StoreAllAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

        Task DeleteByIdAsync(object id, CancellationToken cancellationToken = default);

        Task DeleteByIdsAsync(IEnumerable ids, CancellationToken cancellationToken = default);

        Task DeleteAllAsync(CancellationToken cancellationToken = default);
    }
}