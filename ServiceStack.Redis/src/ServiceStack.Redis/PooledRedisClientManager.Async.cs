//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using ServiceStack.Caching;
using ServiceStack.Redis.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.AsyncEx;

namespace ServiceStack.Redis
{
    public partial class PooledRedisClientManager
        : IRedisClientsManagerAsync
    {
        /// <summary>
        /// Use previous client resolving behavior
        /// </summary>
        public static bool UseGetClientBlocking = false;
        
        ValueTask<ICacheClientAsync> IRedisClientsManagerAsync.GetCacheClientAsync(CancellationToken token)
            => new RedisClientManagerCacheClient(this).AsValueTaskResult<ICacheClientAsync>();

        ValueTask<IRedisClientAsync> IRedisClientsManagerAsync.GetClientAsync(CancellationToken token) => UseGetClientBlocking
            ? GetClientBlocking().AsValueTaskResult<IRedisClientAsync>()
            : GetClientAsync();

        ValueTask<ICacheClientAsync> IRedisClientsManagerAsync.GetReadOnlyCacheClientAsync(CancellationToken token)
            => new RedisClientManagerCacheClient(this) { ReadOnly = true }.AsValueTaskResult<ICacheClientAsync>();

        ValueTask<IRedisClientAsync> IRedisClientsManagerAsync.GetReadOnlyClientAsync(CancellationToken token) => UseGetClientBlocking
            ? GetReadOnlyClientBlocking().AsValueTaskResult<IRedisClientAsync>()
            : GetReadOnlyClientAsync();

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            Dispose();
            return default;
        }

        private AsyncManualResetEvent readAsyncEvent;
        partial void PulseAllReadAsync()
        {
            readAsyncEvent?.Set();
            readAsyncEvent?.Reset();
        }

        private AsyncManualResetEvent writeAsyncEvent;
        partial void PulseAllWriteAsync()
        {
            writeAsyncEvent?.Set();
            writeAsyncEvent?.Reset();
        }

        private async Task<bool> WaitForWriter(int msTimeout)
        {
            // If we're not doing async, no need to create this till we need it.
            writeAsyncEvent ??= new AsyncManualResetEvent(false);
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(msTimeout));
            try
            {
                await writeAsyncEvent.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException) { return false; }
            return true;
        }

        private async ValueTask<IRedisClientAsync> GetClientAsync()
        {
            try
            {
                var inactivePoolIndex = -1;
                do
                {
                   lock (writeClients)
                   {
                      AssertValidReadWritePool();

                      // If it's -1, then we want to try again after a delay of some kind. So if it's NOT negative one, process it...
                      if ((inactivePoolIndex = GetInActiveWriteClient(out var inActiveClient)) != -1)
                      {
                         //inActiveClient != null only for Valid InActive Clients
                         if (inActiveClient != null)
                         {
                            WritePoolIndex++;
                            inActiveClient.Activate();

                            InitClient(inActiveClient);

                            return inActiveClient;
                         }
                         else
                         {
                            // Still need to be in lock for this!
                            break;
                         }
                      }
                   }

                   if (PoolTimeout.HasValue)
                   {
                      // We have a timeout value set - so try to not wait longer than this.
                      if (!await WaitForWriter(PoolTimeout.Value))
                      {
                         throw new TimeoutException(PoolTimeoutError);
                      }
                   }
                   else
                   {
                      // Wait forever, so just retry till we get one.
                      await WaitForWriter(RecheckPoolAfterMs);
                   }
                } while (true); // Just keep repeating until we get a slot.

                //Reaches here when there's no Valid InActive Clients, but we have a slot for one!
                try
                {
                    //inactivePoolIndex = index of reservedSlot || index of invalid client
                    var existingClient = writeClients[inactivePoolIndex];
                    if (existingClient != null && existingClient != reservedSlot && existingClient.HadExceptions)
                    {
                        RedisState.DeactivateClient(existingClient);
                    }

                    var newClient = InitNewClient(RedisResolver.CreateMasterClient(inactivePoolIndex));

                    //Put all blocking I/O or potential Exceptions before lock
                    lock (writeClients)
                    {
                        //If existingClient at inactivePoolIndex changed (failover) return new client outside of pool
                        if (writeClients[inactivePoolIndex] != existingClient)
                        {
                            if (Log.IsDebugEnabled)
                                Log.Debug("writeClients[inactivePoolIndex] != existingClient: {0}".Fmt(writeClients[inactivePoolIndex]));

                            return newClient; //return client outside of pool
                        }

                        WritePoolIndex++;
                        writeClients[inactivePoolIndex] = newClient;

                        return !AssertAccessOnlyOnSameThread
                            ? newClient
                            : newClient.LimitAccessToThread(Thread.CurrentThread.ManagedThreadId, Environment.StackTrace);
                    }
                }
                catch
                {
                    //Revert free-slot for any I/O exceptions that can throw (before lock)
                    lock (writeClients)
                    {
                        writeClients[inactivePoolIndex] = null; //free slot
                    }
                    throw;
                }
            }
            finally
            {
                RedisState.DisposeExpiredClients();
            }
        }
        
        private async Task<bool> WaitForReader(int msTimeout)
        {
            // If we're not doing async, no need to create this till we need it.
            readAsyncEvent ??= new AsyncManualResetEvent(false);
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(msTimeout));
            try
            {
                await readAsyncEvent.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException) { return false; }
            return true;
        }

        private async ValueTask<IRedisClientAsync> GetReadOnlyClientAsync()
        {
            try
            {
                var inactivePoolIndex = -1;
                do
                {
                    lock (readClients)
                    {
                        AssertValidReadOnlyPool();

                        // If it's -1, then we want to try again after a delay of some kind. So if it's NOT negative one, process it...
                        if ((inactivePoolIndex = GetInActiveReadClient(out var inActiveClient)) != -1)
                        {
                            //inActiveClient != null only for Valid InActive Clients
                            if (inActiveClient != null)
                            {
                                ReadPoolIndex++;
                                inActiveClient.Activate();

                                InitClient(inActiveClient);

                                return inActiveClient;
                            }
                            else
                            {
                                // Still need to be in lock for this!
                                break;
                            }
                        }
                    }

                    if (PoolTimeout.HasValue)
                    {
                        // We have a timeout value set - so try to not wait longer than this.
                        if (!await WaitForReader(PoolTimeout.Value))
                        {
                            throw new TimeoutException(PoolTimeoutError);
                        }
                    }
                    else
                    {
                        // Wait forever, so just retry till we get one.
                        await WaitForReader(RecheckPoolAfterMs);
                    }
                } while (true); // Just keep repeating until we get a slot.
                
                //Reaches here when there's no Valid InActive Clients
                try
                {
                    //inactivePoolIndex = index of reservedSlot || index of invalid client
                    var existingClient = readClients[inactivePoolIndex];
                    if (existingClient != null && existingClient != reservedSlot && existingClient.HadExceptions)
                    {
                        RedisState.DeactivateClient(existingClient);
                    }

                    var newClient = InitNewClient(RedisResolver.CreateSlaveClient(inactivePoolIndex));

                    //Put all blocking I/O or potential Exceptions before lock
                    lock (readClients)
                    {
                        //If existingClient at inactivePoolIndex changed (failover) return new client outside of pool
                        if (readClients[inactivePoolIndex] != existingClient)
                        {
                            if (Log.IsDebugEnabled)
                                Log.Debug("readClients[inactivePoolIndex] != existingClient: {0}".Fmt(readClients[inactivePoolIndex]));

                            Interlocked.Increment(ref RedisState.TotalClientsCreatedOutsidePool);

                            //Don't handle callbacks for new client outside pool
                            newClient.ClientManager = null;
                            return newClient; //return client outside of pool
                        }

                        ReadPoolIndex++;
                        readClients[inactivePoolIndex] = newClient;
                        return newClient;
                    }
                }
                catch
                {
                    //Revert free-slot for any I/O exceptions that can throw
                    lock (readClients)
                    {
                        readClients[inactivePoolIndex] = null; //free slot
                    }
                    throw;
                }
            }
            finally
            {
                RedisState.DisposeExpiredClients();
            }
        }

    }

}