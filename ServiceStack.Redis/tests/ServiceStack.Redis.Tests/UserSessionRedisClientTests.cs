using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Model;
using ServiceStack.Logging;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class UserSessionTests
    {
        static UserSessionTests()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
        }

        //MasterUser master;

        static readonly Guid UserClientGlobalId1 = new Guid("71A30DE3-D7AF-4B8E-BCA2-AB646EE1F3E9");
        static readonly Guid UserClientGlobalId2 = new Guid("A8D300CF-0414-4C99-A495-A7F34C93CDE1");
        static readonly string UserClientKey = new Guid("10B7D0F7-4D4E-4676-AAC7-CF0234E9133E").ToString("N");
        static readonly Guid UserId = new Guid("5697B030-A369-43A2-A842-27303A0A62BC");
        private const string UserName = "User1";
        private const string ShardId = "0";

        readonly UserClientSession session = new UserClientSession(
            Guid.NewGuid(), UserId, "192.168.0.1", UserClientKey, UserClientGlobalId1);

        private RedisClient redisCache;

        [SetUp]
        public void OnBeforeEachTest()
        {
            redisCache = new RedisClient(TestConfig.SingleHost);
            redisCache.FlushAll();
            //master = UserMasterDataAccessModel.Instance.MasterUsers.NewDataAccessObject(true);
        }

        public CachedUserSessionManager GetCacheManager(ICacheClient cacheClient)
        {
            return new CachedUserSessionManager(cacheClient);
        }

        private static void AssertClientSessionsAreEqual(
            UserClientSession clientSession, UserClientSession resolvedClientSession)
        {
            Assert.That(resolvedClientSession.Id, Is.EqualTo(clientSession.Id));
            Assert.That(resolvedClientSession.Base64ClientModulus, Is.EqualTo(clientSession.Base64ClientModulus));
            Assert.That(resolvedClientSession.IPAddress, Is.EqualTo(clientSession.IPAddress));
            Assert.That(resolvedClientSession.UserClientGlobalId, Is.EqualTo(clientSession.UserClientGlobalId));
            Assert.That(resolvedClientSession.UserId, Is.EqualTo(clientSession.UserId));
        }

        [Test]
        public void Can_add_single_UserSession()
        {
            var cacheManager = GetCacheManager(redisCache);

            var clientSession = cacheManager.StoreClientSession(
                UserId,
                UserName,
                ShardId,
                session.IPAddress,
                UserClientKey,
                UserClientGlobalId1);

            var resolvedClientSession = cacheManager.GetUserClientSession(
                clientSession.UserId, clientSession.Id);

            AssertClientSessionsAreEqual(clientSession, resolvedClientSession);
        }

        [Test]
        public void Can_add_multiple_UserClientSessions()
        {
            var cacheManager = GetCacheManager(redisCache);

            var clientSession1 = cacheManager.StoreClientSession(
                UserId,
                UserName,
                ShardId,
                session.IPAddress,
                UserClientKey,
                UserClientGlobalId1);

            var clientSession2 = cacheManager.StoreClientSession(
                UserId,
                UserName,
                ShardId,
                session.IPAddress,
                UserClientKey,
                UserClientGlobalId2);

            var resolvedClientSession1 = cacheManager.GetUserClientSession(
                clientSession1.UserId, clientSession1.Id);

            var resolvedClientSession2 = cacheManager.GetUserClientSession(
                clientSession2.UserId, clientSession2.Id);

            AssertClientSessionsAreEqual(clientSession1, resolvedClientSession1);
            AssertClientSessionsAreEqual(clientSession2, resolvedClientSession2);
        }

        [Test]
        public void Does_remove_UserClientSession()
        {
            var cacheManager = GetCacheManager(redisCache);

            var clientSession1 = cacheManager.StoreClientSession(
                UserId,
                UserName,
                ShardId,
                session.IPAddress,
                UserClientKey,
                UserClientGlobalId1);

            var userSession = cacheManager.GetUserSession(UserId);
            var resolvedClientSession1 = userSession.GetClientSession(clientSession1.Id);
            AssertClientSessionsAreEqual(resolvedClientSession1, clientSession1);

            resolvedClientSession1.ExpiryDate = DateTime.UtcNow.AddSeconds(-1);
            cacheManager.UpdateUserSession(userSession);

            userSession = cacheManager.GetUserSession(UserId);
            Assert.That(userSession, Is.Null);
        }

    }

    public class CachedUserSessionManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CachedUserSessionManager));

        /// <summary>
        /// Google/Yahoo seems to make you to login every 2 weeks??
        /// </summary>
        private readonly ICacheClient cacheClient;

        /// <summary>
        /// Big perf hit if we Log on every session change
        /// </summary>
        /// <param name="fmt">The FMT.</param>
        /// <param name="args">The args.</param>
        [Conditional("DEBUG")]
        protected void LogIfDebug(string fmt, params object[] args)
        {
            if (args.Length > 0)
                Log.DebugFormat(fmt, args);
            else
                Log.Debug(fmt);
        }

        public CachedUserSessionManager(ICacheClient cacheClient)
        {
            this.cacheClient = cacheClient;
        }

        /// <summary>
        /// Removes the client session.
        /// </summary>
        /// <param name="userId">The user global id.</param>
        /// <param name="clientSessionIds">The client session ids.</param>
        public void RemoveClientSession(Guid userId, ICollection<Guid> clientSessionIds)
        {
            var userSession = this.GetUserSession(userId);
            if (userSession == null) return;

            foreach (var clientSessionId in clientSessionIds)
            {
                userSession.RemoveClientSession(clientSessionId);
            }
            this.UpdateUserSession(userSession);
        }

        /// <summary>
        /// Adds a new client session.
        /// Should this be changed to GetOrCreateClientSession? 
        /// </summary>
        /// <param name="userId">The user global id.</param>
        /// <param name="userName">Title of the user.</param>
        /// <param name="shardId"></param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="base64ClientModulus">The base64 client modulus.</param>
        /// <param name="userClientGlobalId">The user client global id.</param>
        /// <returns></returns>
        public UserClientSession StoreClientSession(Guid userId, string userName, string shardId, string ipAddress, string base64ClientModulus, Guid userClientGlobalId)
        {
            var userSession = this.GetOrCreateSession(userId, userName, shardId);

            var existingClientSession = userSession.GetClientSessionWithClientId(userClientGlobalId);
            if (existingClientSession != null)
            {
                userSession.RemoveClientSession(existingClientSession.Id);
            }

            var newClientSession = userSession.CreateNewClientSession(
                ipAddress, base64ClientModulus, userClientGlobalId);

            this.UpdateUserSession(userSession);

            return newClientSession;
        }

        /// <summary>
        /// Updates the UserSession in the cache, or removes expired ones.
        /// </summary>
        /// <param name="userSession">The user session.</param>
        public void UpdateUserSession(UserSession userSession)
        {
            var hasSessionExpired = userSession.HasExpired();
            if (hasSessionExpired)
            {
                LogIfDebug("Session has expired, removing: " + userSession.ToCacheKey());
                this.cacheClient.Remove(userSession.ToCacheKey());
            }
            else
            {
                LogIfDebug("Updating session: " + userSession.ToCacheKey());
                this.cacheClient.Replace(userSession.ToCacheKey(), userSession, userSession.ExpiryDate.Value);
            }
        }

        /// <summary>
        /// Gets the user session if it exists or null.
        /// </summary>
        /// <param name="userId">The user global id.</param>
        /// <returns></returns>
        public UserSession GetUserSession(Guid userId)
        {
            var cacheKey = UserSession.ToCacheKey(userId);
            var bytes = this.cacheClient.Get<byte[]>(cacheKey);
            if (bytes != null)
            {
                var modelStr = Encoding.UTF8.GetString(bytes);
                LogIfDebug("UserSession => " + modelStr);
            }
            return this.cacheClient.Get<UserSession>(cacheKey);
        }

        /// <summary>
        /// Gets or create a user session if one doesn't exist.
        /// </summary>
        /// <param name="userId">The user global id.</param>
        /// <param name="userName">Title of the user.</param>
        /// <param name="shardId"></param>
        /// <returns></returns>
        public UserSession GetOrCreateSession(Guid userId, string userName, string shardId)
        {
            var userSession = this.GetUserSession(userId);
            if (userSession == null)
            {
                userSession = new UserSession(userId, userName, shardId);

                this.cacheClient.Add(userSession.ToCacheKey(), userSession,
                    userSession.ExpiryDate.GetValueOrDefault(DateTime.UtcNow) + TimeSpan.FromHours(1));
            }
            return userSession;
        }

        /// <summary>
        /// Gets the user client session identified by the id if exists otherwise null.
        /// </summary>
        /// <param name="userId">The user global id.</param>
        /// <param name="clientSessionId">The client session id.</param>
        /// <returns></returns>
        public UserClientSession GetUserClientSession(Guid userId, Guid clientSessionId)
        {
            var userSession = this.GetUserSession(userId);
            return userSession != null ? userSession.GetClientSession(clientSessionId) : null;
        }
    }

#if !NETCORE
    [Serializable /* was required when storing in memcached, not required in Redis */]
#endif
    public class UserSession
    {
        //Empty constructor required for TypeSerializer
        public UserSession()
        {
            this.PublicClientSessions = new Dictionary<Guid, UserClientSession>();
        }

        public Guid UserId { get; private set; }

        public string UserName { get; private set; }

        public string ShardId { get; private set; }

        public Dictionary<Guid, UserClientSession> PublicClientSessions { get; private set; }

        public UserSession(Guid userId, string userName, string shardId)
            : this()
        {
            this.UserId = userId;
            this.UserName = userName;
            this.ShardId = shardId;
        }

        /// <summary>
        /// Gets the max expiry date of all the users client sessions.
        /// If the user has no more active client sessions we can remove them from the cache.
        /// </summary>
        /// <value>The expiry date.</value>
        public DateTime? ExpiryDate
        {
            get
            {
                DateTime? maxExpiryDate = null;

                foreach (var session in this.PublicClientSessions.Values)
                {
                    if (maxExpiryDate == null || session.ExpiryDate > maxExpiryDate)
                    {
                        maxExpiryDate = session.ExpiryDate;
                    }
                }
                return maxExpiryDate;
            }
        }

        /// <summary>
        /// Creates a new client session for the user.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="base64ClientModulus">The base64 client modulus.</param>
        /// <param name="userClientGlobalId">The user client global id.</param>
        /// <returns></returns>
        public UserClientSession CreateNewClientSession(string ipAddress, string base64ClientModulus, Guid userClientGlobalId)
        {
            return this.CreateClientSession(Guid.NewGuid(), ipAddress, base64ClientModulus, userClientGlobalId);
        }

        public UserClientSession CreateClientSession(Guid sessionId, string ipAddress, string base64ClientModulus, Guid userClientGlobalId)
        {
            var clientSession = new UserClientSession(
                sessionId, this.UserId, ipAddress, base64ClientModulus, userClientGlobalId);

            this.PublicClientSessions[clientSession.Id] = clientSession;

            return clientSession;
        }

        /// <summary>
        /// Removes the client session.
        /// </summary>
        /// <param name="clientSessionId">The client session id.</param>
        public void RemoveClientSession(Guid clientSessionId)
        {
            if (this.PublicClientSessions.ContainsKey(clientSessionId))
            {
                this.PublicClientSessions.Remove(clientSessionId);
            }
        }

        public UserClientSession GetClientSessionWithClientId(Guid userClientId)
        {
            foreach (var entry in this.PublicClientSessions)
            {
                if (entry.Value.UserClientGlobalId == userClientId)
                {
                    return entry.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Verifies this UserSession, removing any expired sessions.
        /// Returns true to keep the UserSession in the cache.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this session has any active client sessions; otherwise, <c>false</c>.
        /// </returns>
        public bool HasExpired()
        {
            RemoveExpiredSessions(this.PublicClientSessions);

            //If there are no more active client sessions we can remove the entire UserSessions
            var sessionHasExpired =
                this.ExpiryDate == null                         //There are no UserClientSessions
                || this.ExpiryDate.Value <= DateTime.UtcNow;    //The max UserClientSession ExpiryDate has expired

            return sessionHasExpired;
        }

        private static void RemoveExpiredSessions(IDictionary<Guid, UserClientSession> clientSessions)
        {
            var expiredSessionKeys = new List<Guid>();

            foreach (var clientSession in clientSessions)
            {
                if (clientSession.Value.ExpiryDate < DateTime.UtcNow)
                {
                    expiredSessionKeys.Add(clientSession.Key);
                }
            }

            foreach (var sessionKey in expiredSessionKeys)
            {
                clientSessions.Remove(sessionKey);
            }
        }

        public void RemoveAllSessions()
        {
            this.PublicClientSessions.Clear();
        }

        public UserClientSession GetClientSession(Guid clientSessionId)
        {
            UserClientSession session;

            if (this.PublicClientSessions.TryGetValue(clientSessionId, out session))
            {
                return session;
            }

            return null;
        }

        public string ToCacheKey()
        {
            return ToCacheKey(this.UserId);
        }

        public static string ToCacheKey(Guid userId)
        {
            return UrnId.Create<UserSession>(userId.ToString());
        }
    }

#if !NETCORE
    [Serializable]
#endif
    public class UserClientSession
        : IHasGuidId
    {
        private const int ValidForTwoWeeks = 14;
        public string IPAddress { get; private set; }
        public DateTime ExpiryDate { get; set; }

        //Empty constructor required for TypeSerializer
        public UserClientSession() { }

        public UserClientSession(Guid sessionId, Guid userId, string ipAddress, string base64ClientModulus, Guid userClientGlobalId)
        {
            this.Id = sessionId;
            this.UserId = userId;
            this.IPAddress = ipAddress;
            this.Base64ClientModulus = base64ClientModulus;
            this.UserClientGlobalId = userClientGlobalId;
            this.ExpiryDate = DateTime.UtcNow.AddDays(ValidForTwoWeeks);
        }

        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Base64ClientModulus { get; set; }
        public Guid UserClientGlobalId { get; set; }
    }

}