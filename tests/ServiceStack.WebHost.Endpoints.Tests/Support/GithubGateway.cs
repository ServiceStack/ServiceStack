using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.DataAnnotations;

namespace ServiceStack.WebHost.Endpoints.Tests.Support
{
    public class GithubRepo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Homepage { get; set; }
        public string Language { get; set; }
        public int Watchers_Count { get; set; }
        public int Stargazes_Count { get; set; }
        public int Forks_Count { get; set; }
        public int Open_Issues_Count { get; set; }
        public int Size { get; set; }
        public string Full_Name { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Pushed_At { get; set; }
        public DateTime? Updated_At { get; set; }

        public bool Has_issues { get; set; }
        public bool Has_Downloads { get; set; }
        public bool Has_Wiki { get; set; }
        public bool Has_Pages { get; set; }
        public bool Fork { get; set; }

        public GithubUser Owner { get; set; }
        public string Svn_Url { get; set; }
        public string Mirror_Url { get; set; }
        public string Url { get; set; }
        public string Ssh_Url { get; set; }
        public string Html_Url { get; set; }
        public string Clone_Url { get; set; }
        public string Git_Url { get; set; }
        public bool Private { get; set; }
    }

    public abstract class GithubUser
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Avatar_Url { get; set; }
        public string Url { get; set; }
        public int? Followers { get; set; }
        public int? Following { get; set; }
        public string Type { get; set; }
        public int? Public_Gists { get; set; }
        public string Location { get; set; }
        public string Company { get; set; }
        public string Html_Url { get; set; }
        public int? Public_Repos { get; set; }
        public DateTime? Created_At { get; set; }
        public string Blog { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public bool? Hireable { get; set; }
        public string Gravatar_Id { get; set; }
        public string Bio { get; set; }
    }

    public class GithubOrg
    {
        public int Id { get; set; }
        public string Avatar_Url { get; set; }
        public string Url { get; set; }
        public string Login { get; set; }
    }

    public class GithubByUser
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
    }

    public class GithubCommitResult
    {
        public string Sha { get; set; }
        public GithubCommit Commit { get; set; }
        public GithubUser Author { get; set; }
        public GithubUser Committer { get; set; }
    }

    public class GithubCommit
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public int Comment_Count { get; set; }
        public GithubByUser Committer { get; set; }
        public GithubByUser Author { get; set; }

        public bool? ShouldSerialize(string fieldName)
        {
            return fieldName != "Committer" && fieldName != "Author";
        }
    }

    public class GithubContent
    {
        [PrimaryKey]
        public string Sha { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
        public string Download_Url { get; set; }
    }

    public class GithubContributor
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public int Contributions { get; set; }
        public string Type { get; set; }

        public string Avatar_Url { get; set; }
        public string Gravatar_Id { get; set; }
        public string Url { get; set; }
        public bool? Site_Admin { get; set; }
    }

    public class GithubSubscriber
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public int Contributions { get; set; }
        public string Type { get; set; }

        public string Avatar_Url { get; set; }
        public string Gravatar_Id { get; set; }
        public string Url { get; set; }
        public bool? Site_Admin { get; set; }
    }

    public class GithubComment
    {
        public int Id { get; set; }
        public string Body { get; set; }
        public DateTime Created_At { get; set; }
        public DateTime Updated_At { get; set; }
        public GithubUser User { get; set; }
        public string Url { get; set; }
        public string Commit_Id { get; set; }

        public string Position { get; set; }
        public string Line { get; set; }
        public string Path { get; set; }
    }

    public class GithubRelease
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Tag_Name { get; set; }
        public string Target_Commitish { get; set; }
        public string Body { get; set; }

        public bool Draft { get; set; }
        public bool PreRelease { get; set; }

        public DateTime Created_At { get; set; }
        public DateTime Published_At { get; set; }
        public GithubUser Author { get; set; }
        public string Url { get; set; }
        public string Tarball_Url { get; set; }
        public string Zipball_Url { get; set; }
    }

    public partial class GithubGateway
    {
        public const string GithubApiBaseUrl = "https://api.github.com/";
        public static string UserAgent = typeof(GithubGateway).Namespace.SplitOnFirst('.').First();

        public string Username { get; set; }
        public string Password { get; set; }

        public List<GithubOrg> GetUserOrgs(string githubUsername)
        {
            return GetJson<List<GithubOrg>>("users/{0}/orgs", githubUsername);
        }

        public List<GithubRepo> GetUserRepos(string githubUsername)
        {
            return GetJson<List<GithubRepo>>("users/{0}/repos", githubUsername);
        }

        public List<GithubRepo> GetOrgRepos(string githubOrgName)
        {
            return GetJson<List<GithubRepo>>("orgs/{0}/repos", githubOrgName);
        }

        public List<GithubRepo> GetAllUserAndOrgsReposFor(string githubUsername)
        {
            var map = new Dictionary<int, GithubRepo>();
            GetUserRepos(githubUsername).ForEach(x => map[x.Id] = x);
            GetUserOrgs(githubUsername).ForEach(org =>
                GetOrgRepos(org.Login)
                    .ForEach(repo => map[repo.Id] = repo));

            return map.Values.ToList();
        }

        public IEnumerable<GithubCommitResult> GetRepoCommits(string githubUser, string githubRepo)
        {
            return StreamJsonCollection<GithubCommitResult>("repos/{0}/{1}/commits", githubUser, githubRepo);
        }

        public List<GithubContent> GetRepoContents(string githubUser, string githubRepo)
        {
            return GetJson<List<GithubContent>>("repos/{0}/{1}/contents", githubUser, githubRepo);
        }

        public List<GithubContributor> GetRepoContributors(string githubUser, string githubRepo)
        {
            return GetJson<List<GithubContributor>>("repos/{0}/{1}/contributors", githubUser, githubRepo);
        }

        public List<GithubSubscriber> GetRepoSubscribers(string githubUser, string githubRepo)
        {
            return GetJson<List<GithubSubscriber>>("repos/{0}/{1}/subscribers", githubUser, githubRepo);
        }

        public List<GithubComment> GetRepoComments(string githubUser, string githubRepo)
        {
            return GetJson<List<GithubComment>>("repos/{0}/{1}/comments", githubUser, githubRepo);
        }

        public List<GithubRelease> GetRepoReleases(string githubUser, string githubRepo)
        {
            return GetJson<List<GithubRelease>>("repos/{0}/{1}/releases", githubUser, githubRepo);
        }

        protected virtual void RequestFilter(HttpWebRequest req)
        {
            req.UserAgent = UserAgent;

            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                req.Headers.Add("Authorization", "Basic " +
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(Username + ":" + Password)));
            }
        }

        public T GetJson<T>(string route, params object[] routeArgs)
        {
            return GithubApiBaseUrl.CombineWith(route.Fmt(routeArgs))
                .GetJsonFromUrl(RequestFilter)
                .FromJson<T>();
        }

        public IEnumerable<T> StreamJsonCollection<T>(string route, params object[] routeArgs)
        {
            List<T> results;
            var nextUrl = GithubApiBaseUrl.CombineWith(route.Fmt(routeArgs));

            do
            {
                results = nextUrl
                    .GetJsonFromUrl(
                        RequestFilter,
                        responseFilter: res => {
                            var links = ParseLinkUrls(res.Headers["Link"]);
                            links.TryGetValue("next", out nextUrl);
                        })
                    .FromJson<List<T>>();

                foreach (var result in results)
                {
                    yield return result;
                }

            } while (results.Count > 0 && nextUrl != null);
        }

        public static Dictionary<string, string> ParseLinkUrls(string linkHeader)
        {
            var map = new Dictionary<string, string>();
            var links = linkHeader;

            while (!string.IsNullOrEmpty(links))
            {
                var urlStartPos = links.IndexOf('<');
                var urlEndPos = links.IndexOf('>');

                if (urlStartPos == -1 || urlEndPos == -1)
                    break;

                var url = links.Substring(urlStartPos + 1, urlEndPos - urlStartPos - 1);
                var parts = links.Substring(urlEndPos).SplitOnFirst(',');

                var relParts = parts[0].Split(';');
                foreach (var relPart in relParts)
                {
                    var keyValueParts = relPart.SplitOnFirst('=');
                    if (keyValueParts.Length < 2)
                        continue;

                    var name = keyValueParts[0].Trim();
                    var value = keyValueParts[1].Trim().Trim('"');

                    if (name == "rel")
                    {
                        map[value] = url;
                    }
                }

                links = parts.Length > 1 ? parts[1] : null;
            }

            return map;
        }
    }
}