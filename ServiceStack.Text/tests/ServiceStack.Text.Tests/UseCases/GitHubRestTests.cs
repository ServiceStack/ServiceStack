using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.UseCases
{
    [TestFixture]
    public class GitHubRestTests
    {
        private const string JsonGitResponse = @"{
  ""pulls"": [
    {
      ""state"": ""open"",
      ""base"": {
        ""label"": ""technoweenie:master"",
        ""ref"": ""master"",
        ""sha"": ""53397635da83a2f4b5e862b5e59cc66f6c39f9c6"",
      },
      ""head"": {
        ""label"": ""smparkes:synchrony"",
        ""ref"": ""synchrony"",
        ""sha"": ""83306eef49667549efebb880096cb539bd436560"",
      },
      ""discussion"": [
        {
          ""type"": ""IssueComment"",
          ""gravatar_id"": ""821395fe70906c8290df7f18ac4ac6cf"",
          ""created_at"": ""2010/10/07 07:38:35 -0700"",
          ""body"": ""Did you intend to remove net/http?  Otherwise, this looks good.  Have you tried running the LIVE tests with it?\r\n\r\n    ruby test/live_server.rb # start the demo server\r\n    LIVE=1 rake"",
          ""updated_at"": ""2010/10/07 07:38:35 -0700"",
          ""id"": 453980,
        },
        {
          ""type"": ""Commit"",
          ""created_at"": ""2010-11-04T16:27:45-07:00"",
          ""sha"": ""83306eef49667549efebb880096cb539bd436560"",
          ""author"": ""Steven Parkes"",
          ""subject"": ""add em_synchrony support"",
          ""email"": ""smparkes@smparkes.net""
        }
      ],
      ""title"": ""Synchrony"",
      ""body"": ""Here's the pull request.\r\n\r\nThis isn't generic EM: require's Ilya's synchrony and needs to be run on its own fiber, e.g., via synchrony or rack-fiberpool.\r\n\r\nI thought about a \""first class\"" em adapter, but I think the faraday api is sync right now, right? Interesting idea to add something like rack's async support to faraday, but that's an itch I don't have right now."",
      ""position"": 4.0,
      ""number"": 15,
      ""votes"": 0,
      ""comments"": 4,
      ""diff_url"": ""https://github.com/technoweenie/faraday/pull/15.diff"",
      ""patch_url"": ""https://github.com/technoweenie/faraday/pull/15.patch"",
      ""labels"": [],
      ""html_url"": ""https://github.com/technoweenie/faraday/pull/15"",
      ""issue_created_at"": ""2010-10-04T12:39:18-07:00"",
      ""issue_updated_at"": ""2010-11-04T16:35:04-07:00"",
      ""created_at"": ""2010-10-04T12:39:18-07:00"",
      ""updated_at"": ""2010-11-04T16:30:14-07:00""
    }
  ]
}";

        public class Discussion
        {
            public string Type { get; set; }
            public string GravatarId { get; set; }
            public string CreatedAt { get; set; }
            public string Body { get; set; }
            public string UpdatedAt { get; set; }

            public int? Id { get; set; }
            public string Sha { get; set; }
            public string Author { get; set; }
            public string Subject { get; set; }
            public string Email { get; set; }
        }

        [Test]
        public void Can_Parse_Discussion_using_JsonObject()
        {
            List<Discussion> discussions = JsonObject.Parse(JsonGitResponse)
            .ArrayObjects("pulls")[0]
            .ArrayObjects("discussion")
            .ConvertAll(x => new Discussion
            {
                Type = x.Get("type"),
                GravatarId = x.Get("gravatar_id"),
                CreatedAt = x.Get("created_at"),
                Body = x.Get("body"),
                UpdatedAt = x.Get("updated_at"),

                Id = x.JsonTo<int?>("id"),
                Sha = x.Get("sha"),
                Author = x.Get("author"),
                Subject = x.Get("subject"),
                Email = x.Get("email"),
            });

            Console.WriteLine(discussions.Dump()); //See what's been parsed
            Assert.That(discussions.ConvertAll(x => x.Type), Is.EquivalentTo(new[] { "IssueComment", "Commit" }));
        }

        [Test]
        public void Can_Parse_Discussion_using_only_NET_collection_classes()
        {
            var jsonObj = JsonSerializer.DeserializeFromString<List<JsonObject>>(JsonGitResponse);
            var jsonPulls = JsonSerializer.DeserializeFromString<List<JsonObject>>(jsonObj[0].Child("pulls"));
            var discussions = JsonSerializer.DeserializeFromString<List<JsonObject>>(jsonPulls[0].Child("discussion"))
                .ConvertAll(x => new Discussion
                {
                    Type = x.Get("type"),
                    GravatarId = x.Get("gravatar_id"),
                    CreatedAt = x.Get("created_at"),
                    Body = x.Get("body"),
                    UpdatedAt = x.Get("updated_at"),

                    Id = x.JsonTo<int?>("id"),
                    Sha = x.Get("sha"),
                    Author = x.Get("author"),
                    Subject = x.Get("subject"),
                    Email = x.Get("email"),
                });

            Console.WriteLine(discussions.Dump()); //See what's been parsed
            Assert.That(discussions.ConvertAll(x => x.Type), Is.EquivalentTo(new[] { "IssueComment", "Commit" }));
        }


        public class GitHubResponse
        {
            public List<Pull> pulls { get; set; }
        }

        public class Pull
        {
            public List<discussion> discussion { get; set; }
            public string title { get; set; }
            public string body { get; set; }
            public double position { get; set; }
            public int number { get; set; }
            public int votes { get; set; }
            public int comments { get; set; }
            public string diff_url { get; set; }
            public string patch_url { get; set; }
            public string html_url { get; set; }
            public DateTime issue_created_date { get; set; }
            public DateTime issue_updated_at { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
        }

        public class discussion
        {
            public string type { get; set; }
            public string gravatar_id { get; set; }
            public string created_at { get; set; }
            public string body { get; set; }
            public string updated_at { get; set; }

            public int? id { get; set; }
            public string sha { get; set; }
            public string author { get; set; }
            public string subject { get; set; }
            public string email { get; set; }
        }

        [Test]
        public void Can_Parse_Discussion_using_custom_client_DTOs()
        {
            var gitHubResponse = JsonSerializer.DeserializeFromString<GitHubResponse>(JsonGitResponse);

            Console.WriteLine(gitHubResponse.Dump()); //See what's been parsed
            Assert.That(gitHubResponse.pulls.SelectMany(p => p.discussion).Map(x => x.type),
                Is.EquivalentTo(new[] { "IssueComment", "Commit" }));
        }

    }
}