// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class FreeLicenseUsageServiceClientTests : LicenseUsageTests
    {
        [SetUp]
        public void SetUp()
        {
            LicenseUtils.RemoveLicense();
            JsConfig.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Licensing.RegisterLicense(new AppSettings().GetString("servicestack:license"));
        }

        [Test]
        public void Allows_registration_of_10_operations()
        {
            using (var appHost = new LicenseTestsAppHost(typeof(Services10)))
            {
                appHost.Init();
                appHost.Start(Config.ListeningOn);

                Assert.That(appHost.Metadata.GetOperationDtos().Count, Is.EqualTo(10));
            }
        }

        [Test]
        public void Throws_on_registration_of_11_operations()
        {
            using (var appHost = new LicenseTestsAppHost(typeof(Services10), typeof(Service1)))
            {
                Assert.Throws<LicenseException>(() =>
                    appHost.Init());
            }
        }

        [Ignore, Test]
        public void Allows_MegaDto_through_ServiceClient()
        {
            using (var appHost = new LicenseTestsAppHost(typeof(MegaDtoService)))
            {
                appHost.Init();
                appHost.Start(Config.ListeningOn);

                var client = new JsonServiceClient(Config.AbsoluteBaseUri);

                var request = MegaDto.Create();

                var response = client.Post(request);
                Assert.That(request.T01.Id, Is.EqualTo(response.T01.Id));

                Assert.Throws<LicenseException>(() =>
                    request.ToJson());

                response = client.Post(request);
                Assert.That(request.T01.Id, Is.EqualTo(response.T01.Id));

                Assert.Throws<LicenseException>(() =>
                    MegaDto.Create().ToJson());
            }
        }
    }

    [TestFixture]
    public class FreeUsageRabbitMqClientTests : LicenseUsageTests
    {
        [Test]
        public void Allows_MegaDto_through_RabbitMqClients()
        {
            var mqFactory = new RabbitMqMessageFactory();

            var request = MegaDto.Create();

            using (var mqClient = mqFactory.CreateMessageProducer())
            {
                mqClient.Publish(request);
            }

            using (var mqClient = mqFactory.CreateMessageQueueClient())
            {
                var msg = mqClient.Get<MegaDto>(QueueNames<MegaDto>.In);
                var response = msg.GetBody();

                Assert.That(request.T01.Id, Is.EqualTo(response.T01.Id));
            }
        }
    }

    [TestFixture]
    public class FreeUsageRedisMqClientTests : LicenseUsageTests
    {
        [Test]
        public void Allows_MegaDto_through_RedisMqClients()
        {
            var mqFactory = new RedisMessageFactory(new BasicRedisClientManager());

            var request = MegaDto.Create();

            using (var mqClient = mqFactory.CreateMessageProducer())
            {
                mqClient.Publish(request);
            }

            using (var mqClient = mqFactory.CreateMessageQueueClient())
            {
                var msg = mqClient.Get<MegaDto>(QueueNames<MegaDto>.In);
                var response = msg.GetBody();

                Assert.That(request.T01.Id, Is.EqualTo(response.T01.Id));
            }
        }
    }

    [TestFixture]
    public class RegisteredLicenseUsageTests : LicenseUsageTests
    {
        [Test]
        public void Allows_registration_of_11_operations()
        {
            Licensing.RegisterLicense(new AppSettings().GetString("servicestack:license"));

            using (var appHost = new LicenseTestsAppHost(typeof(Services10), typeof(Service1)))
            {
                appHost.Init();
                appHost.Start(Config.ListeningOn);

                Assert.That(appHost.Metadata.GetOperationDtos().Count, Is.EqualTo(11));
            }
        }
    }

    public class LicenseUsageTests
    {
        public class T01 { public int Id { get; set; } }
        public class T02 { public int Id { get; set; } }
        public class T03 { public int Id { get; set; } }
        public class T04 { public int Id { get; set; } }
        public class T05 { public int Id { get; set; } }
        public class T06 { public int Id { get; set; } }
        public class T07 { public int Id { get; set; } }
        public class T08 { public int Id { get; set; } }
        public class T09 { public int Id { get; set; } }
        public class T10 { public int Id { get; set; } }
        public class T11 { public int Id { get; set; } }
        public class T12 { public int Id { get; set; } }
        public class T13 { public int Id { get; set; } }
        public class T14 { public int Id { get; set; } }
        public class T15 { public int Id { get; set; } }
        public class T16 { public int Id { get; set; } }
        public class T17 { public int Id { get; set; } }
        public class T18 { public int Id { get; set; } }
        public class T19 { public int Id { get; set; } }
        public class T20 { public int Id { get; set; } }
        public class T21 { public int Id { get; set; } }

        public class Services10 : IService
        {
            public void Any(T01 request) { }
            public void Any(T02 request) { }
            public void Any(T03 request) { }
            public void Any(T04 request) { }
            public void Any(T05 request) { }
            public void Any(T06 request) { }
            public void Any(T07 request) { }
            public void Any(T08 request) { }
            public void Any(T09 request) { }
            public void Any(T10 request) { }
        }

        public class Service1 : IService
        {
            public void Any(T11 request) { }
        }

        public class MegaDto : IReturn<MegaDto>
        {
            public T01 T01 { get; set; }
            public T02 T02 { get; set; }
            public T03 T03 { get; set; }
            public T04 T04 { get; set; }
            public T05 T05 { get; set; }
            public T06 T06 { get; set; }
            public T07 T07 { get; set; }
            public T08 T08 { get; set; }
            public T09 T09 { get; set; }
            public T10 T10 { get; set; }
            public T11 T11 { get; set; }
            public T12 T12 { get; set; }
            public T13 T13 { get; set; }
            public T14 T14 { get; set; }
            public T15 T15 { get; set; }
            public T16 T16 { get; set; }
            public T17 T17 { get; set; }
            public T18 T18 { get; set; }
            public T19 T19 { get; set; }
            public T20 T20 { get; set; }
            public T21 T21 { get; set; }

            public static MegaDto Create()
            {
                return new MegaDto
                {
                    T01 = new T01 { Id = 1 },
                    T02 = new T02 { Id = 1 },
                    T03 = new T03 { Id = 1 },
                    T04 = new T04 { Id = 1 },
                    T05 = new T05 { Id = 1 },
                    T06 = new T06 { Id = 1 },
                    T07 = new T07 { Id = 1 },
                    T08 = new T08 { Id = 1 },
                    T09 = new T09 { Id = 1 },
                    T10 = new T10 { Id = 1 },
                    T11 = new T11 { Id = 1 },
                    T12 = new T12 { Id = 1 },
                    T13 = new T13 { Id = 1 },
                    T14 = new T14 { Id = 1 },
                    T15 = new T15 { Id = 1 },
                    T16 = new T16 { Id = 1 },
                    T17 = new T17 { Id = 1 },
                    T18 = new T18 { Id = 1 },
                    T19 = new T19 { Id = 1 },
                    T20 = new T20 { Id = 1 },
                    T21 = new T21 { Id = 1 },
                };
            }
        }

        public class MegaDtoService : IService
        {
            public object Any(MegaDto request)
            {
                return request;
            }
        }

        protected class LicenseTestsAppHost : AppHostHttpListenerBase
        {
            private readonly List<Type> services;
            public LicenseTestsAppHost(params Type[] services)
                : base(typeof(LicenseTestsAppHost).Name)
            {
                this.services = new List<Type>(services);
            }

            protected override ServiceController CreateServiceController(params Assembly[] assembliesWithServices)
            {
                return new ServiceController(this, () => services);
            }

            public override void Configure(Container container)
            {
                Plugins.RemoveAll(x => x is NativeTypesFeature);
            }
        }
    }
}