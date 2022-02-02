using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class OrmLiteValidationRulesTests : ValidationRulesTests
    {
        protected override ValidationFeature GetValidationFeature(Container container) =>
            new ValidationFeature {
                ValidationSource = new OrmLiteValidationSource(container.Resolve<IDbConnectionFactory>()),
            };
    }
    
    public class OrmLiteWithCacheValidationRulesTests : ValidationRulesTests
    {
        protected override ValidationFeature GetValidationFeature(Container container) =>
            new ValidationFeature {
                ValidationSource = new OrmLiteValidationSource(
                    container.Resolve<IDbConnectionFactory>(),
                    container.Resolve<MemoryCacheClient>()),
            };
    }
    
    public class MemoryValidationRulesTests : ValidationRulesTests
    {
        protected override ValidationFeature GetValidationFeature(Container container) =>
            new ValidationFeature {
                ValidationSource = new MemoryValidationSource(),
            };
    }
    
    public abstract class ValidationRulesTests
    {
        private const string AuthSecret = "secretz";

        protected abstract ValidationFeature GetValidationFeature(Container container);
        
        class AppHost : AppSelfHostBase
        {
            public ValidationFeature ValidationFeature { get; set; }
            public AppHost() : base(nameof(ValidationExceptionTests), typeof(ValidationExceptionTests).Assembly) { }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig {
                    AdminAuthSecret = AuthSecret,
                });

                Plugins.Add(ValidationFeature);
            }
        }

        private readonly ServiceStackHost appHost;
        public ValidationRulesTests()
        {
            ValidationExtensions.RegisteredDtoValidators.Clear();
            
            appHost = new AppHost();
            var container = appHost.Container;
            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            container.Register<IDbConnectionFactory>(dbFactory);
            container.Register(appHost.GetMemoryCacheClient());
            ((AppHost)appHost).ValidationFeature = GetValidationFeature(container);
                    
            appHost.Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        protected virtual JsonServiceClient GetClient() => new JsonServiceClient(Config.ListeningOn);

        [Test]
        public void Does_only_allow_access_to_Admin_by_Default()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            try
            {
                client.Get(new GetValidationRules { Type = nameof(ValidationRulesTest) });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
            }
            
            client.Get(new GetValidationRules {
                Type = nameof(ValidationRulesTest),
                AuthSecret = AuthSecret 
            });
        }

        [Test]
        public void Does_now_allow_registering_invalid_validators()
        {
            (appHost.Resolve<IValidationSource>() as IClearable)?.Clear();
            
            var client = GetClient();
            
            void saveRules(params ValidationRule[] rules) => client.Post(new ModifyValidationRules {
                AuthSecret = AuthSecret,
                SaveRules = new List<ValidationRule>(rules)
            });

            void assertThrows(Action fn, Action<WebServiceException> onError)
            {
                try
                {
                    fn();
                    Assert.Fail($"Should throw {nameof(WebServiceException)}");
                }
                catch (WebServiceException e)
                {
                    onError(e);
                }
            }

            assertThrows(() => saveRules(new ValidationRule {
                Validator = nameof(ValidateScripts.IsAuthenticated),
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Type)));
            
            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest) + "NotExists",
                Validator = nameof(ValidateScripts.IsAuthenticated),
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Type)));
            
            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
                Field = "NotExists",
                Validator = nameof(ValidateScripts.IsAuthenticated),
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Field)));
            
            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
                Validator = nameof(ValidateScripts.IsAuthenticated) + "NotExists",
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Validator)));
            
            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
                Field = nameof(ValidationRulesTest.AuthSecret),
                Validator = nameof(ValidateScripts.IsAuthenticated), //should be IPropertyValidator
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Validator)));
            
            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
                Validator = nameof(ValidateScripts.Null), //should be ITypeValidator
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Validator)));

            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
                Field = nameof(ValidationRulesTest.AuthSecret),
                Validator = $"[{nameof(ValidateScripts.Null)},{nameof(ValidateScripts.IsAuthenticated)}]", //validate all
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Validator)));
            
            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
                Validator = $"[{nameof(ValidateScripts.IsAuthenticated)},{nameof(ValidateScripts.Null)}]", //validate all
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Validator)));
            
            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
                Validator = nameof(ValidateScripts.IsAuthenticated),
                Condition = "true",
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Condition)));
            
            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Validator)));

            assertThrows(() => saveRules(new ValidationRule {
                Type = nameof(ValidationRulesTest),
                Condition = "Invalid (ode"
            }), e => Assert.That(e.StatusCode == 400 &&
                e.GetFieldErrors()[0].FieldName == nameof(ValidationRule.Condition)));
        }

        [Test]
        public void Can_ModifyValidationRules_suspend_and_delete()
        {
            (appHost.Resolve<IValidationSource>() as IClearable)?.Clear();
            
            var client = GetClient();

            var noRules = client.Get(new ValidationRulesTest());

            client.Post(new ModifyValidationRules {
                AuthSecret = AuthSecret,
                SaveRules = new List<ValidationRule> {
                    new ValidationRule { Type = nameof(ValidationRulesTest), Validator = nameof(ValidateScripts.IsAuthenticated) },
                } 
            });

            static void AssertRule(ValidationRule rule)
            {
                // Assert.That(rule.CreatedBy, Is.Not.Null);  //AuthSecret is null
                Assert.That(rule.CreatedDate, Is.Not.Null);
                // Assert.That(rule.ModifiedBy, Is.Not.Null); //AuthSecret is null
                Assert.That(rule.ModifiedDate, Is.Not.Null);
            }

            var typeRules = client.Get(new GetValidationRules {
                AuthSecret = AuthSecret, Type = nameof(ValidationRulesTest)
            });
            Assert.That(typeRules.Results.Count, Is.EqualTo(1));
            AssertRule(typeRules.Results[0]);

            try
            {
                var requiresAuth = client.Get(new ValidationRulesTest());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
            }

            var requiresAuthAsAdmin = client.Get(new ValidationRulesTest {
                AuthSecret = AuthSecret
            });

            client.Post(new ModifyValidationRules {
                AuthSecret = AuthSecret,
                SaveRules = new List<ValidationRule> {
                    new ValidationRule { Type = nameof(ValidationRulesTest), Field = nameof(ValidationRulesTest.Id), Validator = nameof(ValidateScripts.NotNull) },
                } 
            });

            typeRules = client.Get(new GetValidationRules {
                AuthSecret = AuthSecret, Type = nameof(ValidationRulesTest)
            });
            Assert.That(typeRules.Results.Count, Is.EqualTo(2));
            AssertRule(typeRules.Results[0]);
            AssertRule(typeRules.Results[1]);

            try 
            { 
                requiresAuthAsAdmin = client.Get(new ValidationRulesTest {
                    AuthSecret = AuthSecret
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.GetFieldErrors()[0].ErrorCode, Is.EqualTo("NotNull"));
            }

            requiresAuthAsAdmin = client.Get(new ValidationRulesTest {
                AuthSecret = AuthSecret,
                Id = "Id"
            });
            
            client.Post(new ModifyValidationRules {
                AuthSecret = AuthSecret,
                SuspendRuleIds = new[] {
                    typeRules.Results.First(x => x.Field == nameof(ValidationRulesTest.Id)).Id
                }
            });

            typeRules = client.Get(new GetValidationRules {
                AuthSecret = AuthSecret, Type = nameof(ValidationRulesTest)
            });
            Assert.That(typeRules.Results.First(x => x.Field == nameof(ValidationRulesTest.Id)).SuspendedDate, 
                Is.Not.Null);

            // Same as not having last rule
            try
            {
                var requiresAuth = client.Get(new ValidationRulesTest());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
            }

            requiresAuthAsAdmin = client.Get(new ValidationRulesTest {
                AuthSecret = AuthSecret
            });
             
            client.Post(new ModifyValidationRules {
                AuthSecret = AuthSecret,
                DeleteRuleIds = typeRules.Results.Map(x => x.Id).ToArray()
            });
         
            typeRules = client.Get(new GetValidationRules {
                AuthSecret = AuthSecret, Type = nameof(ValidationRulesTest)
            });
            Assert.That(typeRules.Results.Count, Is.EqualTo(0));
            
            noRules = client.Get(new ValidationRulesTest());
        }
        
    }
}