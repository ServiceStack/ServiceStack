using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Messaging;
using ServiceStack.Text.Tests.JsonTests;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class InterfaceTests : TestBase
    {
        [Test]
        public void Can_serialize_Message()
        {
            var message = new Message<string> { Id = new Guid(), CreatedDate = new DateTime(), Body = "test" };
            var messageString = TypeSerializer.SerializeToString(message);

            Assert.That(messageString, Is.EqualTo(
            "{Id:00000000000000000000000000000000,CreatedDate:0001-01-01,Priority:0,RetryAttempts:0,Options:1,Body:test}"));

            Serialize(message);
        }

        [Test]
        public void Can_serialize_IMessage()
        {
            var message = new Message<string> { Id = new Guid(), CreatedDate = new DateTime(), Body = "test" };
            var messageString = TypeSerializer.SerializeToString((IMessage<string>)message);

            var assembly = typeof(string).Assembly.GetName().Name;

            Assert.That(messageString, Is.EqualTo(
            "{__type:\"ServiceStack.Messaging.Message`1[[System.String, " + assembly + "]], ServiceStack.Interfaces\","
             + "Id:00000000000000000000000000000000,CreatedDate:0001-01-01,Priority:0,RetryAttempts:0,Options:1,Body:test}"));
        }

        public class DtoWithObject
        {
            public object Results { get; set; }
        }

        [Test]
        public void Can_deserialize_dto_with_object()
        {
            var dto = Serialize(new DtoWithObject { Results = new Message<string>("Body") }, includeXml: false);
            Assert.That(dto.Results, Is.Not.Null);
            Assert.That(dto.Results.GetType(), Is.EqualTo(typeof(Message<string>)));
        }

        [Test]
        public void Can_serialize_ToString()
        {
            var type = Type.GetType(typeof(Message<string>).AssemblyQualifiedName);
            Assert.That(type, Is.Not.Null);

            type = AssemblyUtils.FindType(typeof(Message<string>).AssemblyQualifiedName);
            Assert.That(type, Is.Not.Null);

            type = Type.GetType("ServiceStack.Messaging.Message`1[[System.String, mscorlib]], ServiceStack.Interfaces");
            Assert.That(type, Is.Not.Null);
        }

        [Test, TestCaseSource(typeof(InterfaceTests), "EndpointExpectations")]
        public void Does_serialize_minimum_type_info_whilst_still_working(
            Type type, string expectedTypeString)
        {
            Assert.That(type.ToTypeString(), Is.EqualTo(expectedTypeString));
            var newType = AssemblyUtils.FindType(type.ToTypeString());
            Assert.That(newType, Is.Not.Null);
            Assert.That(newType, Is.EqualTo(type));
        }

        public static IEnumerable EndpointExpectations
        {
            get
            {
                var assembly = typeof(string).Assembly.GetName().Name;

                yield return new TestCaseData(typeof(Message<string>),
                    "ServiceStack.Messaging.Message`1[[System.String, " + assembly + "]], ServiceStack.Interfaces");

                yield return new TestCaseData(typeof(Cat),
                    "ServiceStack.Text.Tests.JsonTests.Cat, ServiceStack.Text.Tests");

                yield return new TestCaseData(typeof(Zoo),
                    "ServiceStack.Text.Tests.JsonTests.Zoo, ServiceStack.Text.Tests");
            }
        }

        [Test]
        public void Can_deserialize_interface_into_concrete_type()
        {
            var dto = Serialize(new MessagingTests.DtoWithInterface { Results = new Message<string>("Body") }, includeXml: false);
            Assert.That(dto.Results, Is.Not.Null);
        }

        public class UserSession
        {
            public UserSession()
            {
                this.ProviderOAuthAccess = new Dictionary<string, IAuthTokens>();
            }

            public string ReferrerUrl { get; set; }

            public string Id { get; set; }

            public string TwitterUserId { get; set; }

            public string TwitterScreenName { get; set; }

            public string RequestTokenSecret { get; set; }

            public DateTime CreatedAt { get; set; }

            public DateTime LastModified { get; set; }

            public Dictionary<string, IAuthTokens> ProviderOAuthAccess { get; set; }
        }

#if NETFRAMEWORK
        [Test]
        public void Can_Serialize_User_OAuthSession_map()
        {
            var userSession = new UserSession
            {
                Id = "1",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                ReferrerUrl = "http://referrer.com",
                ProviderOAuthAccess = new Dictionary<string, IAuthTokens>
                {
                    {"twitter", new AuthTokens { Provider = "twitter", AccessToken = "TAccessToken", Items = { {"a","1"}, {"b","2"}, }} },
                    {"facebook", new AuthTokens { Provider = "facebook", AccessToken = "FAccessToken", Items = { {"a","1"}, {"b","2"}, }} },
                }
            };

            var fromDto = Serialize(userSession, includeXml: false);
            Console.WriteLine(fromDto.Dump());

            Assert.That(fromDto.ProviderOAuthAccess.Count, Is.EqualTo(2));
            Assert.That(fromDto.ProviderOAuthAccess["twitter"].Provider, Is.EqualTo("twitter"));
            Assert.That(fromDto.ProviderOAuthAccess["facebook"].Provider, Is.EqualTo("facebook"));
            Assert.That(fromDto.ProviderOAuthAccess["twitter"].Items.Count, Is.EqualTo(2));
            Assert.That(fromDto.ProviderOAuthAccess["facebook"].Items.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_Serialize_User_AuthSession_list()
        {
            var userSession = new AuthUserSession
            {
                Id = "1",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                ReferrerUrl = "http://referrer.com",
                ProviderOAuthAccess = new List<IAuthTokens>
                {
                    new AuthTokens { Provider = "twitter", AccessToken = "TAccessToken", Items = { {"a","1"}, {"b","2"}, }},
                    new AuthTokens { Provider = "facebook", AccessToken = "FAccessToken", Items = { {"a","1"}, {"b","2"}, }},
                }
            };

            var fromDto = Serialize(userSession, includeXml: false);
            Console.WriteLine(fromDto.Dump());

            Assert.That(fromDto.ProviderOAuthAccess.Count, Is.EqualTo(2));
            Assert.That(fromDto.ProviderOAuthAccess[0].Provider, Is.EqualTo("twitter"));
            Assert.That(fromDto.ProviderOAuthAccess[1].Provider, Is.EqualTo("facebook"));
            Assert.That(fromDto.ProviderOAuthAccess[0].Items.Count, Is.EqualTo(2));
            Assert.That(fromDto.ProviderOAuthAccess[1].Items.Count, Is.EqualTo(2));
        }

        [Test]
        public void Doesnt_serialize_TypeInfo_when_set()
        {
            try
            {
                JsConfig.ExcludeTypeInfo = true;
                var userSession = new AuthUserSession
                {
                    Id = "1",
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    ReferrerUrl = "http://referrer.com",
                    ProviderOAuthAccess = new List<IAuthTokens>
                    {
                        new AuthTokens { Provider = "twitter", AccessToken = "TAccessToken", Items = { {"a","1"}, {"b","2"}, }},
                        new AuthTokens { Provider = "facebook", AccessToken = "FAccessToken", Items = { {"a","1"}, {"b","2"}, }},
                    }
                };

                Assert.That(userSession.ToJson().IndexOf("__type") == -1, Is.True);
                Assert.That(userSession.ToJsv().IndexOf("__type") == -1, Is.True);
            }
            finally
            {
                JsConfig.Reset();
            }
        }
#endif
        public class AggregateEvents
        {
            public Guid Id { get; set; }
            public List<DomainEvent> Events { get; set; }
        }

        public abstract class DomainEvent { }

        public class UserRegisteredEvent : DomainEvent
        {
            public Guid UserId { get; set; }
            public string Name { get; set; }
        }

        [Serializable]
        public class UserPromotedEvent : DomainEvent
        {
            public Guid UserId { get; set; }
            public string NewRole { get; set; }
        }


        [Test]
        public void Can_deserialize_DomainEvent_into_Concrete_Type()
        {
            var userId = Guid.NewGuid();
            var dto = (DomainEvent)new UserPromotedEvent { UserId = userId };
            var json = dto.ToJson();
            var userPromoEvent = (UserPromotedEvent)json.FromJson<DomainEvent>();
            Assert.That(userPromoEvent.UserId, Is.EqualTo(userId));
        }

        public class Habitat<T> where T : Animal
        {
            public string Continent { get; set; }

            public object Species { get; set; }
        }

        public class Animal
        {
            public string Name { get; set; }
        }

        public class CatAnimal : Animal
        {
            public string Color { get; set; }
        }

        [Test]
        public void Can_serialize_dependent_type_properties()
        {
            var jungle = new Habitat<Animal>
            {
                Continent = "South America",
                Species = new CatAnimal { Name = "Tiger", Color = "Orange" }
            };

            Console.WriteLine(jungle.ToJson());
        }
    }
}