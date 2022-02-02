using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.DynamicModels
{
    [TestFixture]
    public class ODataTests
    {
        const string json = @"{  
    ""odata.metadata"":""http://server.ca/Mediasite/Api/v1/$metadata#UserProfiles"",
    ""value"":[
        {  
            ""odata.id"":""http://server.ca/Mediasite/Api/v1/UserProfiles('111111111111111')"",
                    ""QuotaPolicy@odata.navigationLinkUrl"":""http://server.ca/Mediasite/Api/v1/UserProfiles('111111111111111')/QuotaPolicy"",
                    ""#SetQuotaPolicyFromLevel"":{  
            ""target"":""http://server.ca/Mediasite/Api/v1/UserProfiles('111111111111111')/SetQuotaPolicyFromLevel""
            },
            ""Id"":""111111111111111"",
            ""UserName"":""testuser"",
            ""DisplayName"":""testuser Large"",
            ""Email"":""testuser@testuser.ca"",
            ""Activated"":true,
            ""HomeFolderId"":""312dcf4890df4b129e248a0c9a57869714"",
            ""ModeratorEmail"":""testuser@testuserlarge.ca"",
            ""ModeratorEmailOptOut"":false,
            ""DisablePresentationContentCompleteEmails"":false,
            ""DisablePresentationContentFailedEmails"":false,
            ""DisablePresentationChangeOwnerEmails"":false,
            ""TimeZone"":26,
            ""PresenterFirstName"":null,
            ""PresenterMiddleName"":null,
            ""PresenterLastName"":null,
            ""PresenterEmail"":null,
            ""PresenterPrefix"":null,
            ""PresenterSuffix"":null,
            ""PresenterAdditionalInfo"":null,
            ""PresenterBio"":null,
            ""TrustDirectoryEntry"":null
        }
    ]
}";

        public class ODataUser
        {
            public List<User> Value { get; set; }
        }

        public class User
        {
            public long Id { get; set; }
            public string UserName { get; set; }
            public string DisplayName { get; set; }
            public string Email { get; set; }
            public bool Activated { get; set; }
            public string HomeFolderId { get; set; }
            public string ModeratorEmail { get; set; }
            public bool ModeratorEmailOptOut { get; set; }
            public bool DisablePresentationContentCompleteEmails { get; set; }
            public bool DisablePresentationContentFailedEmails { get; set; }
            public bool DisablePresentationChangeOwnerEmails { get; set; }
            public int TimeZone { get; set; }
        }

        private static void AssertUser(User user)
        {
            Assert.That(user.Id, Is.EqualTo(111111111111111));
            Assert.That(user.UserName, Is.EqualTo("testuser"));
            Assert.That(user.DisplayName, Is.EqualTo("testuser Large"));
            Assert.That(user.Email, Is.EqualTo("testuser@testuser.ca"));
            Assert.That(user.Activated);
        }

        [Test]
        public void Can_extract_model_in_OData_with_JsonObject()
        {
            var users = JsonObject.Parse(json).ArrayObjects("value")
                .Map(x => new User
                {
                    Id = x.Get<long>("Id"),
                    UserName = x["UserName"],
                    DisplayName = x["DisplayName"],
                    Email = x["Email"],
                    Activated = x.Get<bool>("Activated"),
                });

            users.PrintDump();
            AssertUser(users[0]);

            users = JsonObject.Parse(json).ArrayObjects("value")
                .Map(x => x.ConvertTo<User>());

            users.PrintDump();
            AssertUser(users[0]);
        }

        [Test]
        public void Can_Deserialize_OData_into_POCOs()
        {
            var odata = json.FromJson<ODataUser>();
            AssertUser(odata.Value[0]);
        }
    }
}