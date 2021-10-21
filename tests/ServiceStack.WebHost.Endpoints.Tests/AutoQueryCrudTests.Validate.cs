using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.Model;
using ServiceStack.OrmLite;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class NoRockstarAlbumReferences : TypeValidator
    {
        public NoRockstarAlbumReferences() 
            : base("HasForeignKeyReferences", "Has RockstarAlbum References") {}

        public override async Task<bool> IsValidAsync(object dto, IRequest request)
        {
            //Example of using compiled accessor delegates to access `Id` property
            //var id = TypeProperties.Get(dto.GetType()).GetPublicGetter("Id")(dto).ConvertTo<int>();

            var id = ((IHasId<int>) dto).Id;
            using var db = HostContext.AppHost.GetDbConnection(request);
            return !await db.ExistsAsync<RockstarAlbum>(x => x.RockstarId == id);
        }
    }

    public class MyValidators : ScriptMethods
    {
        public ITypeValidator NoRockstarAlbumReferences() => new NoRockstarAlbumReferences();
    }

    public partial class AutoQueryCrudTests
    {
        private bool UseDbSource = true;
        
        partial void OnConfigure(AutoQueryAppHost host, Container container)
        {
            host.ScriptContext.ScriptMethods.AddRange(new ScriptMethods[] {
                new DbScriptsAsync(),
                new MyValidators(), 
            });

            host.Plugins.Add(new ValidationFeature {
                ConditionErrorCodes = {
                    [ValidationConditions.IsOdd] = "NotOdd",
                },
                ErrorCodeMessages = {
                    ["NotOdd"] = "{PropertyName} must be odd",
                    ["RuleMessage"] = "ErrorCodeMessages for RuleMessage",
                }
            });

            if (UseDbSource)
            {
                container.Register<IValidationSource>(c => 
                    new OrmLiteValidationSource(c.Resolve<IDbConnectionFactory>(), host.GetMemoryCacheClient()));
            }
            else
            {
                container.Register<IValidationSource>(new MemoryValidationSource());
            }
            
            var validationSource = container.Resolve<IValidationSource>();
            validationSource.InitSchema();
            validationSource.SaveValidationRulesAsync(new List<ValidationRule> {
                new ValidationRule { Type = nameof(DynamicValidationRules), Validator = "IsAuthenticated" },
                new ValidationRule { Type = nameof(DynamicValidationRules), Field = nameof(DynamicValidationRules.LastName), Validator = "NotNull" },
                new ValidationRule { Type = nameof(DynamicValidationRules), Field = nameof(DynamicValidationRules.Age), Validator = "InclusiveBetween(13,100)" },
            });
        }

        private static void AssertErrorResponse(WebServiceException ex)
        {
            Assert.That(ex.ErrorCode, Is.EqualTo("NotNull"));
            Assert.That(ex.ErrorMessage, Is.EqualTo("'First Name' must not be empty."));
            var status = ex.ResponseStatus;
            if (status.Errors.Count != 3)
                status.PrintDump();
            Assert.That(status.Errors.Count, Is.EqualTo(3));

            var fieldError = status.Errors.First(x => x.FieldName == nameof(RockstarBase.FirstName));
            Assert.That(fieldError.ErrorCode, Is.EqualTo("NotNull"));
            Assert.That(fieldError.Message, Is.EqualTo("'First Name' must not be empty."));
            
            fieldError = status.Errors.First(x => x.FieldName == nameof(RockstarBase.Age));
            Assert.That(fieldError.ErrorCode, Is.EqualTo("NotNull"));
            Assert.That(fieldError.Message, Is.EqualTo("'Age' must not be empty."));
            
            fieldError = status.Errors.First(x => x.FieldName == nameof(RockstarBase.LastName));
            Assert.That(fieldError.ErrorCode, Is.EqualTo("NotNull"));
            Assert.That(fieldError.Message, Is.EqualTo("'Last Name' must not be empty."));
        }

        [Test]
        public void Does_validate_when_no_Abstract_validator()
        {
            try
            {
                var response = client.Post(new NoAbstractValidator {
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                AssertErrorResponse(ex);
                Console.WriteLine(ex);
            }

            try
            {
                var response = client.Post(new NoAbstractValidator {
                    FirstName = "A",
                    LastName = "B",
                    Age = 12,
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("InclusiveBetween"));
                Assert.That(ex.ErrorMessage, Is.EqualTo("'Age' must be between 13 and 100. You entered 12."));
                var status = ex.ResponseStatus;
                Assert.That(status.Errors.Count, Is.EqualTo(1));
            }
            
            client.Post(new NoAbstractValidator {
                FirstName = "A",
                LastName = "B",
                Age = 13,
                DateOfBirth = new DateTime(2001,1,1),
            });
        }

        [Test]
        public void Does_validate_DynamicValidationRules_combined_with_IValidationSource_rules()
        {
            try
            {
                var anonClient = new JsonServiceClient(Config.ListeningOn);
                var response = anonClient.Post(new DynamicValidationRules {
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int) HttpStatusCode.Unauthorized));
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
            }
            
            var authClient = CreateAuthClient();
            try
            {
                var response = authClient.Post(new DynamicValidationRules {
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                AssertErrorResponse(ex);
                Console.WriteLine(ex);
            }

            try
            {
                var response = authClient.Post(new DynamicValidationRules {
                    FirstName = "A",
                    LastName = "B",
                    Age = 12,
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("InclusiveBetween"));
                Assert.That(ex.ErrorMessage, Is.EqualTo("'Age' must be between 13 and 100. You entered 12."));
                var status = ex.ResponseStatus;
                Assert.That(status.Errors.Count, Is.EqualTo(1));
            }
            
            authClient.Post(new DynamicValidationRules {
                FirstName = "A",
                LastName = "B",
                Age = 13,
                DateOfBirth = new DateTime(2001,1,1),
            });
        }
        
        [Test]
        public void Does_validate_combined_declarative_and_AbstractValidator()
        {
            try
            {
                var response = client.Post(new ValidateCreateRockstar {
                    DateOfBirth = new DateTime(2000,1,1)
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                AssertErrorResponse(ex);
                Console.WriteLine(ex);
            }
        }

        [Test]
        public void Does_validate_all_NotEmpty_Fields()
        {
            try
            {
                var response = client.Post(new EmptyValidators());
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ResponseStatus.Errors.Count, 
                    Is.EqualTo(typeof(EmptyValidators).GetPublicProperties().Length));
                Assert.That(ex.ResponseStatus.Errors.All(x => x.ErrorCode == "NotEmpty"));
                Console.WriteLine(ex);
            }
        }

        [Test]
        public void Does_Validate_TriggerAllValidators()
        {
            try
            {
                var response = client.Post(new TriggerAllValidators {
                    CreditCard = "NotCreditCard",
                    Email = "NotEmail",
                    Empty = "NotEmpty",
                    Equal = "NotEqual",
                    ExclusiveBetween = 1,
                    GreaterThan = 1,
                    GreaterThanOrEqual = 1,
                    InclusiveBetween = 1,
                    Length = "Length",
                    LessThan = 20,
                    LessThanOrEqual = 20,
                    NotEmpty = "",
                    NotEqual = "NotEqual",
                    Null = "NotNull",
                    RegularExpression = "FOO",
                    ScalePrecision = 123.456m
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.AssertTriggerValidators();
                Console.WriteLine(ex);
            }
        }

        [Test]
        public void Does_use_CustomErrorMessages()
        {
            try
            {
                var response = client.Post(new CustomValidationErrors());
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Console.WriteLine(ex);
                var status = ex.ResponseStatus;
                Assert.That(ex.ErrorCode, Is.EqualTo("ZERROR"));
                Assert.That(ex.ErrorMessage, Is.EqualTo("'Custom Error Code' must not be empty."));
                Assert.That(status.Errors.Count, Is.EqualTo(typeof(CustomValidationErrors).GetProperties().Length));
                
                var fieldError = status.Errors.First(x => x.FieldName == nameof(CustomValidationErrors.CustomErrorCode));
                Assert.That(fieldError.ErrorCode, Is.EqualTo("ZERROR"));
                Assert.That(fieldError.Message, Is.EqualTo("'Custom Error Code' must not be empty."));
                
                fieldError = status.Errors.First(x => x.FieldName == nameof(CustomValidationErrors.CustomErrorCodeAndMessage));
                Assert.That(fieldError.ErrorCode, Is.EqualTo("ZERROR"));
                Assert.That(fieldError.Message, Is.EqualTo("Custom Error Code And Message has to be between 1 and 2, you: 0"));
                
                fieldError = status.Errors.First(x => x.FieldName == nameof(CustomValidationErrors.ErrorCodeRule));
                Assert.That(fieldError.ErrorCode, Is.EqualTo("RuleMessage"));
                Assert.That(fieldError.Message, Is.EqualTo("ErrorCodeMessages for RuleMessage"));
                
                fieldError = status.Errors.First(x => x.FieldName == nameof(CustomValidationErrors.IsOddCondition));
                Assert.That(fieldError.ErrorCode, Is.EqualTo("NotOdd"));
                Assert.That(fieldError.Message, Is.EqualTo("Is Odd Condition must be odd"));
                
                fieldError = status.Errors.First(x => x.FieldName == nameof(CustomValidationErrors.IsOddAndOverTwoDigitsCondition));
                Assert.That(fieldError.ErrorCode, Is.EqualTo("RuleMessage"));
                Assert.That(fieldError.Message, Is.EqualTo("ErrorCodeMessages for RuleMessage"));
                
                fieldError = status.Errors.First(x => x.FieldName == nameof(CustomValidationErrors.IsOddOrOverTwoDigitsCondition));
                Assert.That(fieldError.ErrorCode, Is.EqualTo("ScriptCondition"));
                Assert.That(fieldError.Message, Is.EqualTo("The specified condition was not met for 'Is Odd Or Over Two Digits Condition'."));
            }
        }

        [Test]
        public void Can_satisfy_combined_conditions()
        {
            try
            {
                var response = client.Post(new CustomValidationErrors {
                    IsOddAndOverTwoDigitsCondition = 101
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ResponseStatus.Errors.Count, Is.EqualTo(typeof(CustomValidationErrors).GetProperties().Length - 1));
                Assert.That(ex.ResponseStatus.Errors.All(x => x.FieldName != nameof(CustomValidationErrors.IsOddAndOverTwoDigitsCondition)));
            }
            try
            {
                var response = client.Post(new CustomValidationErrors {
                    IsOddOrOverTwoDigitsCondition = 102
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ResponseStatus.Errors.Count, Is.EqualTo(typeof(CustomValidationErrors).GetProperties().Length - 1));
                Assert.That(ex.ResponseStatus.Errors.All(x => x.FieldName != nameof(CustomValidationErrors.IsOddOrOverTwoDigitsCondition)));
            }
        }

        [Test]
        public void Does_OnlyValidatesRequest()
        {
            try
            {
                var response = client.Post(new OnlyValidatesRequest {
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.ErrorCode, Is.EqualTo("RuleMessage"));
                Assert.That(ex.ErrorMessage, Is.EqualTo("ErrorCodeMessages for RuleMessage"));
                Assert.That(ex.GetFieldErrors().Count, Is.EqualTo(0));
            }
            
            try
            {
                var response = client.Post(new OnlyValidatesRequest {
                    Test = 101
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(401));
                Assert.That(ex.ErrorCode, Is.EqualTo("AssertFailed2"));
                Assert.That(ex.ErrorMessage, Is.EqualTo("2nd Assert Failed"));
                Assert.That(ex.GetFieldErrors().Count, Is.EqualTo(0));
            }
            
            try
            {
                var response = client.Post(new OnlyValidatesRequest {
                    Test = 1001
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.ErrorCode, Is.EqualTo("NotNull"));
                Assert.That(ex.GetFieldErrors().Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void Can_use_custom_Guid_Id_and_DateTimeOffset()
        {
            try
            {
                client.Post(new Authenticate {
                    provider = "credentials",
                    UserName = "admin@email.com",
                    Password = "p@55wOrd",
                    RememberMe = true,
                });
                
                var response = client.Post(new CreateBookmark {
                    Description = "Description", 
                    Slug = "Slug", 
                    Title = "Title", 
                    Url = "Url", 
                });
                
                Assert.That(response.Id, Is.Not.EqualTo(new Guid()));
                Assert.That(response.Result.Id, Is.EqualTo(response.Id));
                Assert.That(response.Result.Description, Is.EqualTo("Description"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Test]
        public void Does_validate_TestAuthValidators()
        {
            try
            {
                var anonClient = new JsonServiceClient(Config.ListeningOn);
                anonClient.Post(new TestAuthValidators());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(401));
                Assert.That(e.ErrorCode, Is.EqualTo("Unauthorized"));
                Assert.That(e.ErrorMessage, Is.EqualTo("Not Authenticated"));
            }

            try
            {
                var employeeClient = new JsonServiceClient(Config.ListeningOn);
                
                employeeClient.Post(new Authenticate {
                    provider = "credentials",
                    UserName = "employee@email.com",
                    Password = "p@55wOrd",
                    RememberMe = true,
                });

                employeeClient.Post(new TestAuthValidators());
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(403));
                Assert.That(e.ErrorCode, Is.EqualTo("Forbidden"));
                Assert.That(e.ErrorMessage, Is.EqualTo("Manager Role Required"));
            }

            try
            {
                var managerClient = new JsonServiceClient(Config.ListeningOn);
                
                managerClient.Post(new Authenticate {
                    provider = "credentials",
                    UserName = "manager",
                    Password = "p@55wOrd",
                    RememberMe = true,
                });

                managerClient.Post(new TestAuthValidators());
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("NotNull"));
            }

            try
            {
                var adminClient = new JsonServiceClient(Config.ListeningOn);
                
                adminClient.Post(new Authenticate {
                    provider = "credentials",
                    UserName = "admin@email.com",
                    Password = "p@55wOrd",
                    RememberMe = true,
                });

                adminClient.Post(new TestAuthValidators());
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("NotNull"));
            }
        }
        

        [Test]
        public void Does_validate_TestMultiAuthValidators()
        {
            try
            {
                var anonClient = new JsonServiceClient(Config.ListeningOn);
                anonClient.Post(new TestMultiAuthValidators());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(401));
                Assert.That(e.ErrorCode, Is.EqualTo("Unauthorized"));
                Assert.That(e.ErrorMessage, Is.EqualTo("Not Authenticated"));
            }

            try
            {
                var employeeClient = new JsonServiceClient(Config.ListeningOn);
                
                employeeClient.Post(new Authenticate {
                    provider = "credentials",
                    UserName = "employee@email.com",
                    Password = "p@55wOrd",
                    RememberMe = true,
                });

                employeeClient.Post(new TestMultiAuthValidators());
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(403));
                Assert.That(e.ErrorCode, Is.EqualTo("Forbidden"));
                Assert.That(e.ErrorMessage, Is.EqualTo("Manager Role Required"));
            }

            try
            {
                var managerClient = new JsonServiceClient(Config.ListeningOn);
                
                managerClient.Post(new Authenticate {
                    provider = "credentials",
                    UserName = "manager",
                    Password = "p@55wOrd",
                    RememberMe = true,
                });

                managerClient.Post(new TestMultiAuthValidators());
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("NotNull"));
            }

            try
            {
                var adminClient = new JsonServiceClient(Config.ListeningOn);
                
                adminClient.Post(new Authenticate {
                    provider = "credentials",
                    UserName = "admin@email.com",
                    Password = "p@55wOrd",
                    RememberMe = true,
                });

                adminClient.Post(new TestMultiAuthValidators());
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("NotNull"));
            }
        }

        [Test]
        public void Does_validate_TestIsAdmin()
        {
            var userNames = new[] { "employee@email.com", "manager" };
            foreach (var userName in userNames)
            {
                var userClient = new JsonServiceClient(Config.ListeningOn);
                if (userName != null)
                {
                    try
                    {
                        var managerClient = new JsonServiceClient(Config.ListeningOn);
                
                        managerClient.Post(new Authenticate {
                            provider = "credentials",
                            UserName = "manager",
                            Password = "p@55wOrd",
                            RememberMe = true,
                        });

                        managerClient.Post(new TestIsAdmin());
                    }
                    catch (WebServiceException e)
                    {
                        Assert.That(e.StatusCode, Is.EqualTo(403));
                        Assert.That(e.ErrorCode, Is.EqualTo("Forbidden"));
                        Assert.That(e.ErrorMessage, Is.EqualTo("Admin Role Required"));
                    }
                }
            }

            try
            {
                var adminClient = new JsonServiceClient(Config.ListeningOn);
                
                adminClient.Post(new Authenticate {
                    provider = "credentials",
                    UserName = "admin@email.com",
                    Password = "p@55wOrd",
                    RememberMe = true,
                });

                adminClient.Post(new TestIsAdmin());
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("NotNull"));
            }
        }

        [Test]
        public void Does_validate_TestDbCondition()
        {
            using var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection();
            db.DropAndCreateTable<RockstarAlbum>();
            
            try
            {
                db.Insert(new RockstarAlbum { Id = 1, Name = "An Album", Genre = "Pop", RockstarId = 1 });
                var response = client.Post(new TestDbCondition {
                    Id = 1,
                });
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("HasForeignKeyReferences"));
            }
            
            try
            {
                db.Delete<RockstarAlbum>(x => x.RockstarId == 1);
                var response = client.Post(new TestDbCondition {
                    Id = 1,
                });
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("NotNull")); //success!
            }
        }

        [Test]
        public void Does_validate_TestDbValidator()
        {
            using var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection();
            db.DropAndCreateTable<RockstarAlbum>();
            
            try
            {
                db.Insert(new RockstarAlbum { Id = 1, Name = "An Album", Genre = "Pop", RockstarId = 1 });
                var response = client.Post(new TestDbValidator {
                    Id = 1,
                });
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("HasForeignKeyReferences"));
            }
            
            try
            {
                db.Delete<RockstarAlbum>(x => x.RockstarId == 1);
                var response = client.Post(new TestDbValidator {
                    Id = 1,
                });
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(400));
                Assert.That(e.ErrorCode, Is.EqualTo("NotNull")); //success!
            }
        }
    }

    public static class ValidationUtils
    {
        public static void AssertTriggerValidators(this WebServiceException ex)
        {
            var errors = ex.ResponseStatus.Errors;
            Assert.That(errors.First(x => x.FieldName == "CreditCard").ErrorCode, Is.EqualTo("CreditCard"));
            Assert.That(errors.First(x => x.FieldName == "Email").ErrorCode, Is.EqualTo("Email"));
            Assert.That(errors.First(x => x.FieldName == "Email").ErrorCode, Is.EqualTo("Email"));
            Assert.That(errors.First(x => x.FieldName == "Empty").ErrorCode, Is.EqualTo("Empty"));
            Assert.That(errors.First(x => x.FieldName == "Equal").ErrorCode, Is.EqualTo("Equal"));
            Assert.That(errors.First(x => x.FieldName == "ExclusiveBetween").ErrorCode, Is.EqualTo("ExclusiveBetween"));
            Assert.That(errors.First(x => x.FieldName == "GreaterThan").ErrorCode, Is.EqualTo("GreaterThan"));
            Assert.That(errors.First(x => x.FieldName == "GreaterThanOrEqual").ErrorCode, Is.EqualTo("GreaterThanOrEqual"));
            Assert.That(errors.First(x => x.FieldName == "InclusiveBetween").ErrorCode, Is.EqualTo("InclusiveBetween"));
            Assert.That(errors.First(x => x.FieldName == "Length").ErrorCode, Is.EqualTo("Length"));
            Assert.That(errors.First(x => x.FieldName == "LessThan").ErrorCode, Is.EqualTo("LessThan"));
            Assert.That(errors.First(x => x.FieldName == "LessThanOrEqual").ErrorCode, Is.EqualTo("LessThanOrEqual"));
            Assert.That(errors.First(x => x.FieldName == "NotEmpty").ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors.First(x => x.FieldName == "NotEqual").ErrorCode, Is.EqualTo("NotEqual"));
            Assert.That(errors.First(x => x.FieldName == "Null").ErrorCode, Is.EqualTo("Null"));
            Assert.That(errors.First(x => x.FieldName == "RegularExpression").ErrorCode, Is.EqualTo("RegularExpression"));
            Assert.That(errors.First(x => x.FieldName == "ScalePrecision").ErrorCode, Is.EqualTo("ScalePrecision"));
        }
    }
}