// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Runtime.CompilerServices;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class SessionIncr : IReturn<SessionResponse> { }

    public class SessionResponse
    {
        public int Counter { get; set; }
    }

    public class SessionCartIncr : IReturn<Cart>
    {
        public Guid CartId { get; set; }
    }

    public class Cart
    {
        public int Qty { get; set; }
    }

    public class SessionTypedIncr : IReturn<AuthUserSession> {}

    public class SessionService : Service
    {
        public SessionResponse Get(SessionIncr request)
        {
            var counter = base.SessionBag.Get<int>("counter");

            base.SessionBag["counter"] = ++counter;

            return new SessionResponse
            {
                Counter = counter
            };
        }

        public Cart Get(SessionCartIncr request)
        {
            var sessionKey = UrnId.Create<Cart>(request.CartId);
            var cart = base.SessionBag.Get<Cart>(sessionKey) ?? new Cart();
            cart.Qty++;

            base.SessionBag[sessionKey] = cart;

            return cart;
        }

        public AuthUserSession Get(SessionTypedIncr request)
        {
            var session = base.SessionAs<AuthUserSession>();
            session.Tag++;

            this.SaveSession(session);

            return session;
        }
    }

    [TestFixture]
    public class SessionTests
    {
        private SessionAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new SessionAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public class SessionAppHost : AppHostHttpListenerBase
        {
            public SessionAppHost() : base(typeof(SessionTests).Name, typeof(SessionTests).Assembly) {}

            public override void Configure(Container container)
            {
                Plugins.Add(new SessionFeature());

                const bool UseOrmLiteCache = false;
                if (UseOrmLiteCache)
                {
                    container.Register<IDbConnectionFactory>(c =>
                        new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                    container.RegisterAs<OrmLiteCacheClient, ICacheClient>();

                    container.Resolve<ICacheClient>().InitSchema();
                }
            }
        }

        [Test]
        public void Can_increment_session_int_counter()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            Assert.That(client.Get(new SessionIncr()).Counter, Is.EqualTo(1));
            Assert.That(client.Get(new SessionIncr()).Counter, Is.EqualTo(2));
        }

        [Test]
        public void Different_clients_have_different_session_int_counters()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var altClient = new JsonServiceClient(Config.AbsoluteBaseUri);

            Assert.That(client.Get(new SessionIncr()).Counter, Is.EqualTo(1));
            Assert.That(client.Get(new SessionIncr()).Counter, Is.EqualTo(2));
            Assert.That(altClient.Get(new SessionIncr()).Counter, Is.EqualTo(1));
        }

        [Test]
        public void Can_increment_session_cart_qty()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var request = new SessionCartIncr { CartId = Guid.NewGuid() };

            Assert.That(client.Get(request).Qty, Is.EqualTo(1));
            Assert.That(client.Get(request).Qty, Is.EqualTo(2));
        }

        [Test]
        public void Different_clients_have_different_session_cart_qty()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var altClient = new JsonServiceClient(Config.AbsoluteBaseUri);
            var request = new SessionCartIncr { CartId = Guid.NewGuid() };

            Assert.That(client.Get(request).Qty, Is.EqualTo(1));
            Assert.That(client.Get(request).Qty, Is.EqualTo(2));
            Assert.That(altClient.Get(request).Qty, Is.EqualTo(1));
        }

        public static T Log<T>(T item, [CallerMemberName] string caller = null)
        {
            "{0}: {1}".Print(caller, item.Dump());
            return item;
        }

        [Test]
        public void Can_increment_typed_session_tag()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            Assert.That(Log(client.Get(new SessionTypedIncr())).Tag, Is.EqualTo(1));
            Assert.That(Log(client.Get(new SessionTypedIncr())).Tag, Is.EqualTo(2));
        }

        [Test]
        public void Different_clients_have_different_typed_session_tag()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var altClient = new JsonServiceClient(Config.AbsoluteBaseUri);

            Assert.That(Log(client.Get(new SessionTypedIncr())).Tag, Is.EqualTo(1));
            Assert.That(Log(client.Get(new SessionTypedIncr())).Tag, Is.EqualTo(2));
            Assert.That(Log(altClient.Get(new SessionTypedIncr())).Tag, Is.EqualTo(1));
            Assert.That(Log(altClient.Get(new SessionTypedIncr())).Tag, Is.EqualTo(2));

            Assert.That(client.Get(new SessionTypedIncr()).Tag, Is.EqualTo(3));
            Assert.That(altClient.Get(new SessionTypedIncr()).Tag, Is.EqualTo(3));
        }
    }
}