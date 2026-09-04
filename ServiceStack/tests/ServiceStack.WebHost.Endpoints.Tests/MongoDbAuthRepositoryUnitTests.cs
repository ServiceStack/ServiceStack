using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Authentication.MongoDb;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class MongoDbAuthRepositoryUnitTests
    {
        private class FakeCursor<T> : IAsyncCursor<T>
        {
            private readonly List<T> items;
            private bool read = false;

            public FakeCursor(List<T> items)
            {
                this.items = items;
            }

            public IEnumerable<T> Current => items;

            public bool MoveNext(CancellationToken cancellationToken = default)
            {
                if (read) return false;
                read = true;
                return true;
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(MoveNext(cancellationToken));
            }

            public void Dispose()
            {
            }
        }

        public class MongoDbMockProxy : DispatchProxy
        {
            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == "ListCollections")
                {
                    var docs = new List<BsonDocument>
                    {
                        new BsonDocument("name", nameof(UserAuth)),
                        new BsonDocument("name", nameof(UserAuthDetails)),
                        new BsonDocument("name", "Counters")
                    };
                    return new FakeCursor<BsonDocument>(docs);
                }
                if (targetMethod.Name == "ListCollectionsAsync")
                {
                    var docs = new List<BsonDocument>
                    {
                        new BsonDocument("name", nameof(UserAuth)),
                        new BsonDocument("name", nameof(UserAuthDetails)),
                        new BsonDocument("name", "Counters")
                    };
                    return Task.FromResult<IAsyncCursor<BsonDocument>>(new FakeCursor<BsonDocument>(docs));
                }

                if (targetMethod.ReturnType == typeof(Task))
                    return Task.CompletedTask;

                if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = targetMethod.ReturnType.GetGenericArguments()[0];
                    var defaultVal = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                    return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, new[] { defaultVal });
                }

                return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
            }
        }

        private MongoDbAuthRepository CreateRepo()
        {
            var db = DispatchProxy.Create<IMongoDatabase, MongoDbMockProxy>();
            return new MongoDbAuthRepository(db, createMissingCollections: false);
        }

        [Test]
        public void CollectionsExists_Returns_True_When_All_Collections_Present()
        {
            var repo = CreateRepo();
            Assert.That(repo.CollectionsExists(), Is.True);
        }

        [Test]
        public async Task CollectionsExistsAsync_Returns_True_When_All_Collections_Present()
        {
            var repo = CreateRepo();
            Assert.That(await repo.CollectionsExistsAsync(), Is.True);
        }

        [Test]
        public void TryAuthenticate_Digest_Null_Or_Empty_Headers_Returns_False()
        {
            var repo = CreateRepo();

            var result = repo.TryAuthenticate(digestHeaders: null, "key", 300, "seq", out var userAuth);
            Assert.That(result, Is.False);
            Assert.That(userAuth, Is.Null);

            result = repo.TryAuthenticate(new Dictionary<string, string>(), "key", 300, "seq", out userAuth);
            Assert.That(result, Is.False);
            Assert.That(userAuth, Is.Null);
        }

        [Test]
        public async Task TryAuthenticateAsync_Digest_Null_Or_Empty_Headers_Returns_Null()
        {
            var repo = CreateRepo();

            var userAuth = await repo.TryAuthenticateAsync(digestHeaders: null, "key", 300, "seq");
            Assert.That(userAuth, Is.Null);

            userAuth = await repo.TryAuthenticateAsync(new Dictionary<string, string>(), "key", 300, "seq");
            Assert.That(userAuth, Is.Null);
        }

        [Test]
        public void GetUserAuth_Invalid_Or_Null_Id_Returns_Null()
        {
            var repo = CreateRepo();

            Assert.That(repo.GetUserAuth((string)null), Is.Null);
            Assert.That(repo.GetUserAuth(""), Is.Null);
            Assert.That(repo.GetUserAuth("not-an-int"), Is.Null);
        }

        [Test]
        public async Task GetUserAuthAsync_Invalid_Or_Null_Id_Returns_Null()
        {
            var repo = CreateRepo();

            Assert.That(await repo.GetUserAuthAsync((string)null), Is.Null);
            Assert.That(await repo.GetUserAuthAsync(""), Is.Null);
            Assert.That(await repo.GetUserAuthAsync("not-an-int"), Is.Null);
        }

        [Test]
        public void GetUserAuthDetails_Invalid_Or_Null_Id_Returns_EmptyList()
        {
            var repo = CreateRepo();

            Assert.That(repo.GetUserAuthDetails(null), Is.Empty);
            Assert.That(repo.GetUserAuthDetails(""), Is.Empty);
            Assert.That(repo.GetUserAuthDetails("not-an-int"), Is.Empty);
        }

        [Test]
        public async Task GetUserAuthDetailsAsync_Invalid_Or_Null_Id_Returns_EmptyList()
        {
            var repo = CreateRepo();

            Assert.That(await repo.GetUserAuthDetailsAsync(null), Is.Empty);
            Assert.That(await repo.GetUserAuthDetailsAsync(""), Is.Empty);
            Assert.That(await repo.GetUserAuthDetailsAsync("not-an-int"), Is.Empty);
        }

        [Test]
        public void DeleteUserAuth_Invalid_Or_Null_Id_Does_Not_Throw()
        {
            var repo = CreateRepo();

            Assert.DoesNotThrow(() => repo.DeleteUserAuth(null));
            Assert.DoesNotThrow(() => repo.DeleteUserAuth(""));
            Assert.DoesNotThrow(() => repo.DeleteUserAuth("not-an-int"));
        }

        [Test]
        public async Task DeleteUserAuthAsync_Invalid_Or_Null_Id_Does_Not_Throw()
        {
            var repo = CreateRepo();

            Assert.DoesNotThrowAsync(async () => await repo.DeleteUserAuthAsync(null));
            Assert.DoesNotThrowAsync(async () => await repo.DeleteUserAuthAsync(""));
            Assert.DoesNotThrowAsync(async () => await repo.DeleteUserAuthAsync("not-an-int"));
        }

        [Test]
        public void SaveUserAuth_Null_AuthSession_Throws_ArgumentNullException()
        {
            var repo = CreateRepo();
            Assert.Throws<ArgumentNullException>(() => repo.SaveUserAuth((IAuthSession)null));
        }

        [Test]
        public async Task SaveUserAuthAsync_Null_AuthSession_Throws_ArgumentNullException()
        {
            var repo = CreateRepo();
            Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.SaveUserAuthAsync((IAuthSession)null));
        }

        [Test]
        public void ApiKey_Methods_Handle_Null_And_Empty_Safely()
        {
            var repo = CreateRepo();

            Assert.That(repo.ApiKeyExists(null), Is.False);
            Assert.That(repo.ApiKeyExists(""), Is.False);
            Assert.That(repo.GetApiKey(null), Is.Null);
            Assert.That(repo.GetApiKey(""), Is.Null);
            Assert.That(repo.GetUserApiKeys(null), Is.Empty);
            Assert.That(repo.GetUserApiKeys(""), Is.Empty);
            Assert.DoesNotThrow(() => repo.StoreAll(null));
            Assert.DoesNotThrow(() => repo.StoreAll(new List<ApiKey>()));
        }

        [Test]
        public async Task ApiKey_Async_Methods_Handle_Null_And_Empty_Safely()
        {
            var repo = CreateRepo();

            Assert.That(await repo.ApiKeyExistsAsync(null), Is.False);
            Assert.That(await repo.ApiKeyExistsAsync(""), Is.False);
            Assert.That(await repo.GetApiKeyAsync(null), Is.Null);
            Assert.That(await repo.GetApiKeyAsync(""), Is.Null);
            Assert.That(await repo.GetUserApiKeysAsync(null), Is.Empty);
            Assert.That(await repo.GetUserApiKeysAsync(""), Is.Empty);
            Assert.DoesNotThrowAsync(async () => await repo.StoreAllAsync(null));
            Assert.DoesNotThrowAsync(async () => await repo.StoreAllAsync(new List<ApiKey>()));
        }

        [Test]
        public void GetUserAuth_Session_Tokens_Null_Safety()
        {
            var repo = CreateRepo();

            Assert.That(repo.GetUserAuth(null, null), Is.Null);
            Assert.That(repo.GetUserAuth(new AuthUserSession(), null), Is.Null);
        }

        [Test]
        public async Task GetUserAuthAsync_Session_Tokens_Null_Safety()
        {
            var repo = CreateRepo();

            Assert.That(await repo.GetUserAuthAsync(null, null), Is.Null);
            Assert.That(await repo.GetUserAuthAsync(new AuthUserSession(), null), Is.Null);
        }
    }
}
