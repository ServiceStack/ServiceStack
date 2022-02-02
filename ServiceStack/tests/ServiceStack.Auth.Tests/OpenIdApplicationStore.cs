using System;
using System.Collections.Generic;
using System.Linq;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OpenId;

namespace ServiceStack.Auth.Tests
{
    public class InMemoryOpenIdApplicationStore : IOpenIdApplicationStore, ICryptoKeyStore, INonceStore
    {
        /// <summary>
        /// How frequently to check for and remove expired secrets.
        /// </summary>
        private static readonly TimeSpan cleaningInterval = TimeSpan.FromMinutes(30);

        /// <summary>
        /// An in-memory cache of decrypted symmetric keys.
        /// </summary>
        /// <remarks>
        /// The key is the bucket name.  The value is a dictionary whose key is the handle and whose value is the cached key.
        /// </remarks>
        private readonly Dictionary<string, Dictionary<string, CryptoKey>> store = new Dictionary<string, Dictionary<string, CryptoKey>>(StringComparer.Ordinal);

        /// <summary>
        /// The last time the cache had expired keys removed from it.
        /// </summary>
        private DateTime lastCleaning = DateTime.UtcNow;

        /// <summary>
        /// Gets the key in a given bucket and handle.
        /// </summary>
        /// <param name="bucket">The bucket name.  Case sensitive.</param>
        /// <param name="handle">The key handle.  Case sensitive.</param>
        /// <returns>
        /// The cryptographic key, or <c>null</c> if no matching key was found.
        /// </returns>
        public CryptoKey GetKey(string bucket, string handle)
        {
            lock (this.store)
            {
                Dictionary<string, CryptoKey> cacheBucket;
                if (this.store.TryGetValue(bucket, out cacheBucket))
                {
                    CryptoKey key;
                    if (cacheBucket.TryGetValue(handle, out key))
                    {
                        return key;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a sequence of existing keys within a given bucket.
        /// </summary>
        /// <param name="bucket">The bucket name.  Case sensitive.</param>
        /// <returns>
        /// A sequence of handles and keys, ordered by descending <see cref="CryptoKey.ExpiresUtc"/>.
        /// </returns>
        public IEnumerable<KeyValuePair<string, CryptoKey>> GetKeys(string bucket)
        {
            lock (this.store)
            {
                Dictionary<string, CryptoKey> cacheBucket;
                if (this.store.TryGetValue(bucket, out cacheBucket))
                {
                    return cacheBucket.ToList();
                }
                else
                {
                    return Enumerable.Empty<KeyValuePair<string, CryptoKey>>();
                }
            }
        }

        /// <summary>
        /// Stores a cryptographic key.
        /// </summary>
        /// <param name="bucket">The name of the bucket to store the key in.  Case sensitive.</param>
        /// <param name="handle">The handle to the key, unique within the bucket.  Case sensitive.</param>
        /// <param name="key">The key to store.</param>
        /// <exception cref="CryptoKeyCollisionException">Thrown in the event of a conflict with an existing key in the same bucket and with the same handle.</exception>
        public void StoreKey(string bucket, string handle, CryptoKey key)
        {
            lock (this.store)
            {
                Dictionary<string, CryptoKey> cacheBucket;
                if (!this.store.TryGetValue(bucket, out cacheBucket))
                {
                    this.store[bucket] = cacheBucket = new Dictionary<string, CryptoKey>(StringComparer.Ordinal);
                }

                if (cacheBucket.ContainsKey(handle))
                {
                    throw new CryptoKeyCollisionException();
                }

                cacheBucket[handle] = key;

                this.CleanExpiredKeysFromMemoryCacheIfAppropriate();
            }
        }

        /// <summary>
        /// Removes the key.
        /// </summary>
        /// <param name="bucket">The bucket name.  Case sensitive.</param>
        /// <param name="handle">The key handle.  Case sensitive.</param>
        public void RemoveKey(string bucket, string handle)
        {
            lock (this.store)
            {
                Dictionary<string, CryptoKey> cacheBucket;
                if (this.store.TryGetValue(bucket, out cacheBucket))
                {
                    cacheBucket.Remove(handle);
                }
            }
        }

        /// <summary>
        /// Cleans the expired keys from memory cache if the cleaning interval has passed.
        /// </summary>
        private void CleanExpiredKeysFromMemoryCacheIfAppropriate()
        {
            if (DateTime.UtcNow > this.lastCleaning + cleaningInterval)
            {
                lock (this.store)
                {
                    if (DateTime.UtcNow > this.lastCleaning + cleaningInterval)
                    {
                        this.ClearExpiredKeysFromMemoryCache();
                    }
                }
            }
        }

        /// <summary>
        /// Weeds out expired keys from the in-memory cache.
        /// </summary>
        private void ClearExpiredKeysFromMemoryCache()
        {
            lock (this.store)
            {
                var emptyBuckets = new List<string>();
                foreach (var bucketPair in this.store)
                {
                    var expiredKeys = new List<string>();
                    foreach (var handlePair in bucketPair.Value)
                    {
                        if (handlePair.Value.ExpiresUtc < DateTime.UtcNow)
                        {
                            expiredKeys.Add(handlePair.Key);
                        }
                    }

                    foreach (var expiredKey in expiredKeys)
                    {
                        bucketPair.Value.Remove(expiredKey);
                    }

                    if (bucketPair.Value.Count == 0)
                    {
                        emptyBuckets.Add(bucketPair.Key);
                    }
                }

                foreach (string emptyBucket in emptyBuckets)
                {
                    this.store.Remove(emptyBucket);
                }

                this.lastCleaning = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// How frequently we should take time to clear out old nonces.
        /// </summary>
        private const int AutoCleaningFrequency = 10;

        /// <summary>
        /// The maximum age a message can be before it is discarded.
        /// </summary>
        /// <remarks>
        /// This is useful for knowing how long used nonces must be retained.
        /// </remarks>
        private readonly TimeSpan maximumMessageAge;

        /// <summary>
        /// A list of the consumed nonces.
        /// </summary>
        private readonly SortedDictionary<DateTime, List<string>> usedNonces = new SortedDictionary<DateTime, List<string>>();

        /// <summary>
        /// A lock object used around accesses to the <see cref="usedNonces"/> field.
        /// </summary>
        private object nonceLock = new object();

        /// <summary>
        /// Where we're currently at in our periodic nonce cleaning cycle.
        /// </summary>
        private int nonceClearingCounter;

        #region INonceStore Members

        /// <summary>
        /// Stores a given nonce and timestamp.
        /// </summary>
        /// <param name="context">The context, or namespace, within which the <paramref name="nonce"/> must be unique.</param>
        /// <param name="nonce">A series of random characters.</param>
        /// <param name="timestamp">The timestamp that together with the nonce string make it unique.
        /// The timestamp may also be used by the data store to clear out old nonces.</param>
        /// <returns>
        /// True if the nonce+timestamp (combination) was not previously in the database.
        /// False if the nonce was stored previously with the same timestamp.
        /// </returns>
        /// <remarks>
        /// The nonce must be stored for no less than the maximum time window a message may
        /// be processed within before being discarded as an expired message.
        /// If the binding element is applicable to your channel, this expiration window
        /// is retrieved or set using the
        /// <see cref="StandardExpirationBindingElement.MaximumMessageAge"/> property.
        /// </remarks>
        public bool StoreNonce(string context, string nonce, DateTime timestamp)
        {
            if (timestamp.ToUniversalTime() + this.maximumMessageAge < DateTime.UtcNow)
            {
                // The expiration binding element should have taken care of this, but perhaps
                // it's at the boundary case.  We should fail just to be safe.
                return false;
            }

            // We just concatenate the context with the nonce to form a complete, namespace-protected nonce.
            string completeNonce = context + "\0" + nonce;

            lock (this.nonceLock)
            {
                List<string> nonces;
                if (!this.usedNonces.TryGetValue(timestamp, out nonces))
                {
                    this.usedNonces[timestamp] = nonces = new List<string>(4);
                }

                if (nonces.Contains(completeNonce))
                {
                    return false;
                }

                nonces.Add(completeNonce);

                // Clear expired nonces if it's time to take a moment to do that.
                // Unchecked so that this can int overflow without an exception.
                unchecked
                {
                    this.nonceClearingCounter++;
                }
                if (this.nonceClearingCounter % AutoCleaningFrequency == 0)
                {
                    this.ClearExpiredNonces();
                }

                return true;
            }
        }

        #endregion

        /// <summary>
        /// Clears consumed nonces from the cache that are so old they would be
        /// rejected if replayed because it is expired.
        /// </summary>
        public void ClearExpiredNonces()
        {
            lock (this.nonceLock)
            {
                var oldNonceLists = this.usedNonces.Keys.Where(time => time.ToUniversalTime() + this.maximumMessageAge < DateTime.UtcNow).ToList();
                foreach (DateTime time in oldNonceLists)
                {
                    this.usedNonces.Remove(time);
                }

                // Reset the auto-clean counter so that if this method was called externally
                // we don't auto-clean right away.
                this.nonceClearingCounter = 0;
            }
        }
    }
}
