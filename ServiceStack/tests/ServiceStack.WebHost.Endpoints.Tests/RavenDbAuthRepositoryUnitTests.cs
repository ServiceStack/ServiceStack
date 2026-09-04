using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using ServiceStack.Auth;
using ServiceStack.Authentication.RavenDb;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class RavenDbAuthRepositoryUnitTests
    {
        public class RavenStoreMockProxy : DispatchProxy
        {
            private DocumentConventions conventions = new DocumentConventions();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == "get_Conventions")
                    return conventions;
                if (targetMethod.Name == "ExecuteIndex" || targetMethod.Name == "ExecuteIndexAsync")
                    return null;
                if (targetMethod.Name == "OpenSession")
                    return null;
                if (targetMethod.Name == "OpenAsyncSession")
                    return null;

                if (targetMethod.ReturnType == typeof(Task))
                    return Task.CompletedTask;
                if (typeof(Task).IsAssignableFrom(targetMethod.ReturnType))
                {
                    var resultType = targetMethod.ReturnType.GetGenericArguments()[0];
                    var defaultVal = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                    return typeof(Task).GetMethod(nameof(Task.FromResult))!
                        .MakeGenericMethod(resultType)
                        .Invoke(null, new[] { defaultVal });
                }

                if (targetMethod.ReturnType.IsValueType)
                    return Activator.CreateInstance(targetMethod.ReturnType);

                return null;
            }

            public static IDocumentStore Create()
            {
                return DispatchProxy.Create<IDocumentStore, RavenStoreMockProxy>();
            }
        }

        [Test]
        public void RavenIdConverter_Converts_Standard_Composite_Id()
        {
            Assert.That(RavenIdConverter.ToInt("RavenUserAuths/1-A"), Is.EqualTo(100));
            Assert.That(RavenIdConverter.ToString("RavenUserAuths", 100), Is.EqualTo("RavenUserAuths/1-A"));

            Assert.That(RavenIdConverter.ToInt("RavenUserAuths/1-B"), Is.EqualTo(101));
            Assert.That(RavenIdConverter.ToString("RavenUserAuths", 101), Is.EqualTo("RavenUserAuths/1-B"));

            Assert.That(RavenIdConverter.ToInt("users/25-Z"), Is.EqualTo(2525));
            Assert.That(RavenIdConverter.ToString("users", 2525), Is.EqualTo("users/25-Z"));
        }

        [Test]
        public void RavenIdConverter_Converts_Numeric_String()
        {
            Assert.That(RavenIdConverter.ToInt("100"), Is.EqualTo(100));
            Assert.That(RavenIdConverter.ToInt("101"), Is.EqualTo(101));
            Assert.That(RavenIdConverter.TryToInt("100", out var id), Is.True);
            Assert.That(id, Is.EqualTo(100));
        }

        [Test]
        public void RavenIdConverter_Converts_Single_Segment()
        {
            Assert.That(RavenIdConverter.ToInt("users/5"), Is.EqualTo(500));
            Assert.That(RavenIdConverter.TryToInt("users/5", out var id), Is.True);
            Assert.That(id, Is.EqualTo(500));
        }

        [Test]
        public void RavenIdConverter_Converts_Deep_Path()
        {
            Assert.That(RavenIdConverter.ToInt("databases/mydb/docs/users/2-C"), Is.EqualTo(202));
            Assert.That(RavenIdConverter.TryToInt("databases/mydb/docs/users/2-C", out var id), Is.True);
            Assert.That(id, Is.EqualTo(202));
        }

        [Test]
        public void RavenIdConverter_Handles_Case_Insensitive_Cluster_Tag()
        {
            Assert.That(RavenIdConverter.ToInt("users/1-a"), Is.EqualTo(100));
            Assert.That(RavenIdConverter.ToInt("users/1-b"), Is.EqualTo(101));
        }

        [Test]
        public void RavenIdConverter_TryToInt_Returns_False_On_Invalid_Inputs()
        {
            Assert.That(RavenIdConverter.TryToInt(null, out _), Is.False);
            Assert.That(RavenIdConverter.TryToInt("", out _), Is.False);
            Assert.That(RavenIdConverter.TryToInt("   ", out _), Is.False);
            Assert.That(RavenIdConverter.TryToInt("users/", out _), Is.False);
            Assert.That(RavenIdConverter.TryToInt("users/abc", out _), Is.False);
            Assert.That(RavenIdConverter.TryToInt("invalid", out _), Is.False);

            Assert.Throws<FormatException>(() => RavenIdConverter.ToInt("invalid"));
        }

        [Test]
        public void RavenIdConverter_ToString_Edge_Cases()
        {
            Assert.That(RavenIdConverter.ToString("users", -5), Is.EqualTo("users/0-A"));
            Assert.That(RavenIdConverter.ToString("users/", 100), Is.EqualTo("users/1-A"));
            Assert.That(RavenIdConverter.ToString("", 100), Is.EqualTo("/1-A"));
            Assert.That(RavenIdConverter.ToString(null, 100), Is.EqualTo("/1-A"));
        }

        [Test]
        public void FindIdentityProperty_Evaluates_Correctly()
        {
            var keyProp = typeof(RavenUserAuth).GetProperty(nameof(RavenUserAuth.Key));
            var idProp = typeof(RavenUserAuth).GetProperty(nameof(RavenUserAuth.Id));
            var standardIdProp = typeof(UserAuth).GetProperty(nameof(UserAuth.Id));

            Assert.That(RavenDbUserAuthRepository.DefaultFindIdentityProperty(keyProp), Is.True);
            Assert.That(RavenDbUserAuthRepository.DefaultFindIdentityProperty(idProp), Is.False);
            Assert.That(RavenDbUserAuthRepository.DefaultFindIdentityProperty(standardIdProp), Is.True);
            Assert.That(RavenDbUserAuthRepository.DefaultFindIdentityProperty(null), Is.False);
        }

        [Test]
        public void Constructor_Guards_Against_Null_Store()
        {
            Assert.Throws<ArgumentNullException>(() => new RavenDbUserAuthRepository(null));
        }

        [Test]
        public void TryAuthenticate_Digest_Null_Safety()
        {
            var store = RavenStoreMockProxy.Create();
            var repo = new RavenDbUserAuthRepository(store, createIndexes: false);

            Assert.That(repo.TryAuthenticate((Dictionary<string, string>)null, "key", 100, "1", out var userAuth), Is.False);
            Assert.That(userAuth, Is.Null);

            Assert.That(repo.TryAuthenticate(new Dictionary<string, string>(), "key", 100, "1", out userAuth), Is.False);
            Assert.That(userAuth, Is.Null);

            Assert.That(repo.TryAuthenticate(new Dictionary<string, string> { { "username", "" } }, "key", 100, "1", out userAuth), Is.False);
            Assert.That(userAuth, Is.Null);
        }

        [Test]
        public async Task TryAuthenticateAsync_Digest_Null_Safety()
        {
            var store = RavenStoreMockProxy.Create();
            var repo = new RavenDbUserAuthRepository(store, createIndexes: false);

            var userAuth = await repo.TryAuthenticateAsync((Dictionary<string, string>)null, "key", 100, "1");
            Assert.That(userAuth, Is.Null);

            userAuth = await repo.TryAuthenticateAsync(new Dictionary<string, string>(), "key", 100, "1");
            Assert.That(userAuth, Is.Null);

            userAuth = await repo.TryAuthenticateAsync(new Dictionary<string, string> { { "username", "" } }, "key", 100, "1");
            Assert.That(userAuth, Is.Null);
        }

        [Test]
        public async Task Null_Argument_Guards_Are_Enforced()
        {
            var store = RavenStoreMockProxy.Create();
            var repo = new RavenDbUserAuthRepository(store, createIndexes: false);

            // Null session guards
            Assert.That(repo.GetUserAuth((IAuthSession)null, null), Is.Null);
            Assert.That(await repo.GetUserAuthAsync((IAuthSession)null, null), Is.Null);

            // Null ID guards
            Assert.That(repo.GetUserAuth((string)null), Is.Null);
            Assert.That(await repo.GetUserAuthAsync((string)null), Is.Null);
            Assert.That(repo.GetUserAuth(""), Is.Null);
            Assert.That(await repo.GetUserAuthAsync(""), Is.Null);

            // Details null guards
            Assert.That(repo.GetUserAuthDetails(null), Is.Empty);
            Assert.That(await repo.GetUserAuthDetailsAsync(null), Is.Empty);
            Assert.That(repo.GetUserAuthDetails(""), Is.Empty);
            Assert.That(await repo.GetUserAuthDetailsAsync(""), Is.Empty);

            // Delete null guards
            Assert.DoesNotThrow(() => repo.DeleteUserAuth(null));
            Assert.DoesNotThrowAsync(async () => await repo.DeleteUserAuthAsync(null));

            // ApiKey null guards
            Assert.That(repo.ApiKeyExists(null), Is.False);
            Assert.That(await repo.ApiKeyExistsAsync(null), Is.False);
            Assert.That(repo.ApiKeyExists(""), Is.False);
            Assert.That(await repo.ApiKeyExistsAsync(""), Is.False);

            Assert.That(repo.GetApiKey(null), Is.Null);
            Assert.That(await repo.GetApiKeyAsync(null), Is.Null);
            Assert.That(repo.GetApiKey(""), Is.Null);
            Assert.That(await repo.GetApiKeyAsync(""), Is.Null);

            Assert.That(repo.GetUserApiKeys(null), Is.Empty);
            Assert.That(await repo.GetUserApiKeysAsync(null), Is.Empty);
            Assert.That(repo.GetUserApiKeys(""), Is.Empty);
            Assert.That(await repo.GetUserApiKeysAsync(""), Is.Empty);

            Assert.DoesNotThrow(() => repo.StoreAll(null));
            Assert.DoesNotThrowAsync(async () => await repo.StoreAllAsync(null));

            // Required entity argument checks
            Assert.Throws<ArgumentNullException>(() => repo.CreateUserAuth(null, "pass"));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateUserAuthAsync(null, "pass"));

            Assert.Throws<ArgumentNullException>(() => repo.SaveUserAuth((IAuthSession)null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.SaveUserAuthAsync((IAuthSession)null));

            Assert.Throws<ArgumentNullException>(() => repo.SaveUserAuth((IUserAuth)null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.SaveUserAuthAsync((IUserAuth)null));

            Assert.Throws<ArgumentNullException>(() => repo.UpdateUserAuth(null, new RavenUserAuth()));
            Assert.Throws<ArgumentNullException>(() => repo.UpdateUserAuth(new RavenUserAuth(), null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateUserAuthAsync(null, new RavenUserAuth()));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateUserAuthAsync(new RavenUserAuth(), null));
        }
    }
}
