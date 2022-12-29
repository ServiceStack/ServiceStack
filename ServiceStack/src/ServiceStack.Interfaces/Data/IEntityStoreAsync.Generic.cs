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
        Task<T> GetByIdAsync(object id, CancellationToken token = default);

        Task<IList<T>> GetByIdsAsync(IEnumerable ids, CancellationToken token = default);

        Task<IList<T>> GetAllAsync(CancellationToken token = default);

        Task<T> StoreAsync(T entity, CancellationToken token = default);

        Task StoreAllAsync(IEnumerable<T> entities, CancellationToken token = default);

        Task DeleteAsync(T entity, CancellationToken token = default);

        Task DeleteByIdAsync(object id, CancellationToken token = default);

        Task DeleteByIdsAsync(IEnumerable ids, CancellationToken token = default);

        Task DeleteAllAsync(CancellationToken token = default);
    }
}