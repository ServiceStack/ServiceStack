using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack
{
    public interface IGistGateway
    {
        Gist GetGist(string gistId);
        void WriteGistFiles(string gistId, Dictionary<string, string> gistFiles);
        void CreateGistFile(string gistId, string filePath, string contents);
        void WriteGistFile(string gistId, string filePath, string contents);
        void DeleteGistFiles(string gistId, params string[] filePaths);
    }
    
    public class GithubGateway : IGistGateway
    {
        public string UserAgent { get; set; } = nameof(GithubGateway);
        public string BaseUrl { get; set; } = "https://api.github.com/";
        public bool UseForkParent { get; set; } = true;

        /// <summary>
        /// AccessTokenSecret
        /// </summary>
        public string AccessToken { get; set; }

        public GithubGateway() {}
        public GithubGateway(string accessToken) => AccessToken = accessToken;

        public string UnwrapRepoFullName(string orgName, string name)
        {
            try
            {
                var repo = GetJson<GithubRepo>($"/repos/{orgName}/{name}");
                if (repo.Fork)
                {
                    if (repo.Fork && UseForkParent)
                    {
                        if (repo.Parent != null)
                            return repo.Parent.Full_Name;
                    }
                }

                return repo.Full_Name;
            }
            catch (WebException ex)
            {
                if (ex.IsNotFound())
                    return null;
                throw;
            }
        }

        public string GetSourceZipUrl(string orgNames, string name)
        {
            var orgs = orgNames.Split(';')
                .Map(x => x.LeftPart(' '));
            foreach (var orgName in orgs)
            {
                var repoFullName = UnwrapRepoFullName(orgName, name);
                if (repoFullName == null)
                    continue;

                var json = GetJson($"repos/{repoFullName}/releases");
                var response = JSON.parse(json);

                if (response is List<object> releases && releases.Count > 0 &&
                    releases[0] is Dictionary<string, object> release &&
                    release.TryGetValue("zipball_url", out var zipUrl))
                {
                    return (string) zipUrl;
                }

                return $"https://github.com/{repoFullName}/archive/master.zip";
            }

            throw new Exception($"'{name}' was not found in sources: {orgs.Join(", ")}");
        }

        public async Task<List<GithubRepo>> GetSourceReposAsync(string orgName)
        {
            var repos = (await GetUserAndOrgReposAsync(orgName))
                .OrderBy(x => x.Name)
                .ToList();
            return repos;
        }

        public async Task<List<GithubRepo>> GetUserAndOrgReposAsync(string githubOrgOrUser)
        {
            var map = new Dictionary<string, GithubRepo>();

            var userRepos = GetJsonCollectionAsync<List<GithubRepo>>($"users/{githubOrgOrUser}/repos");
            var orgRepos = GetJsonCollectionAsync<List<GithubRepo>>($"orgs/{githubOrgOrUser}/repos");

            try
            {
                foreach (var repos in await userRepos)
                foreach (var repo in repos)
                    map[repo.Name] = repo;
            }
            catch (Exception e)
            {
                if (!e.IsNotFound()) throw;
            }

            try
            {
                foreach (var repos in await userRepos)
                foreach (var repo in repos)
                    map[repo.Name] = repo;
            }
            catch (Exception e)
            {
                if (!e.IsNotFound()) throw;
            }

            return map.Values.ToList();
        }

        public List<GithubRepo> GetUserRepos(string githubUser) =>
            StreamJsonCollection<List<GithubRepo>>($"users/{githubUser}/repos").SelectMany(x => x).ToList();

        public List<GithubRepo> GetOrgRepos(string githubOrg) =>
            StreamJsonCollection<List<GithubRepo>>($"orgs/{githubOrg}/repos").SelectMany(x => x).ToList();

        public string GetJson(string route)
        {
            var apiUrl = !route.IsUrl()
                ? BaseUrl.CombineWith(route)
                : route;

            return apiUrl.GetJsonFromUrl(ApplyRequestFilters);
        }

        public T GetJson<T>(string route) => GetJson(route).FromJson<T>();

        public IEnumerable<T> StreamJsonCollection<T>(string route)
        {
            List<T> results;
            var nextUrl = BaseUrl.CombineWith(route);

            do
            {
                results = nextUrl.GetJsonFromUrl(ApplyRequestFilters,
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

        public async Task<List<T>> GetJsonCollectionAsync<T>(string route)
        {
            var to = new List<T>();
            List<T> results;
            var nextUrl = BaseUrl.CombineWith(route);

            do
            {
                results = (await nextUrl.GetJsonFromUrlAsync(ApplyRequestFilters,
                        responseFilter: res => {
                            var links = ParseLinkUrls(res.Headers["Link"]);
                            links.TryGetValue("next", out nextUrl);
                        }))
                    .FromJson<List<T>>();

                to.AddRange(results);
            } while (results.Count > 0 && nextUrl != null);

            return to;
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

        public void DownloadFile(string downloadUrl, string fileName)
        {
            var webClient = new WebClient();
            webClient.Headers.Add(HttpHeaders.UserAgent, UserAgent);
            webClient.DownloadFile(downloadUrl, fileName);
        }

        public GithubGist GetGithubGist(string gistId)
        {
            var json = GetJson($"/gists/{gistId}");
            var response = json.FromJson<GithubGist>();
            return response;
        }

        public Gist GetGist(string gistId)
        {
            var result = GetGithubGist(gistId);
            if (result != null)
            {
                result.UserId = result.Owner?.Login;
            }
            return result;
        }

        /// <summary>
        /// Create or Write Gist Text Files. Requires AccessToken
        /// </summary>
        public void WriteGistFiles(string gistId, Dictionary<string,string> gistFiles)
        {
            AssertAccessToken();

            var i = 0;
            var sb = StringBuilderCache.Allocate().Append("{\"files\":{");
            foreach (var entry in gistFiles)
            {
                if (i++ > 0)
                    sb.Append(",");
                
                var jsonFile = entry.Key.ToJson();
                sb.Append(jsonFile)
                    .Append(":{\"filename\":")
                    .Append(jsonFile)
                    .Append(",\"content\":")
                    .Append(entry.Value.ToJson())
                    .Append("}");
            }
            sb.Append("}}");
                
            var json = StringBuilderCache.ReturnAndFree(sb);
            BaseUrl.CombineWith($"/gists/{gistId}")
                .PatchJsonToUrl(json, requestFilter: ApplyRequestFilters);
        }

        /// <summary>
        /// Create new Gist File. Requires AccessToken
        /// </summary>
        public void CreateGistFile(string gistId, string filePath, string contents)
        {            
            AssertAccessToken();
            var jsonFile = filePath.ToJson();
            var sb = StringBuilderCache.Allocate().Append("{\"files\":{")
                .Append(jsonFile)
                .Append(":{")
                .Append("\"content\":")
                .Append(contents.ToJson())
                .Append("}}}");

            var json = StringBuilderCache.ReturnAndFree(sb);
            BaseUrl.CombineWith($"/gists/{gistId}")
                .PatchJsonToUrl(json, requestFilter: ApplyRequestFilters);
        }

        /// <summary>
        /// Create or Write Gist File. Requires AccessToken
        /// </summary>
        public void WriteGistFile(string gistId, string filePath, string contents)
        {
            AssertAccessToken();
            var jsonFile = filePath.ToJson();
            var sb = StringBuilderCache.Allocate().Append("{\"files\":{")
                .Append(jsonFile)
                .Append(":{\"filename\":")
                .Append(jsonFile)
                .Append(",\"content\":")
                .Append(contents.ToJson())
                .Append("}}}");

            var json = StringBuilderCache.ReturnAndFree(sb);
            BaseUrl.CombineWith($"/gists/{gistId}")
                .PatchJsonToUrl(json, requestFilter: ApplyRequestFilters);
        }

        private void AssertAccessToken()
        {
            if (string.IsNullOrEmpty(AccessToken))
                throw new NotSupportedException("An AccessToken is required to modify gist");
        }

        public void DeleteGistFiles(string gistId, params string[] filePaths)
        {
            AssertAccessToken();

            var i = 0;
            var sb = StringBuilderCache.Allocate().Append("{\"files\":{");
            foreach (var filePath in filePaths)
            {
                if (i++ > 0)
                    sb.Append(",");
                
                sb.Append(filePath.ToJson())
                  .Append(":null");
            }
            sb.Append("}}");

            var json = StringBuilderCache.ReturnAndFree(sb);
            BaseUrl.CombineWith($"/gists/{gistId}")
                .PatchJsonToUrl(json, requestFilter: ApplyRequestFilters);
        }

        public void ApplyRequestFilters(HttpWebRequest req)
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                req.Headers["Authorization"] = "token " + AccessToken;
            }
            req.UserAgent = UserAgent;
        }
    }
    
    public class GithubRepo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Homepage { get; set; }
        public int Watchers_Count { get; set; }
        public int Stargazers_Count { get; set; }
        public int Size { get; set; }
        public string Full_Name { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Updated_At { get; set; }

        public bool Has_Downloads { get; set; }
        public bool Fork { get; set; }

        public string Url { get; set; } // https://api.github.com/repos/NetCoreWebApps/bare
        public string Html_Url { get; set; }
        public bool Private { get; set; }

        public GithubRepo Parent { get; set; } // only on single result, e.g: /repos/NetCoreWebApps/bare
    }

    public class Gist
    {
        public string Url { get; set; }
        public string Html_Url { get; set; }
        public Dictionary<string, GistFile> Files { get; set; }
        public bool Public { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Updated_At { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
    }

    public class GistFile
    {
        public string Filename { get; set; }
        public string Type { get; set; }
        public string Language { get; set; }
        public string Raw_Url { get; set; }
        public int Size { get; set; }
        public bool Truncated { get; set; }
        public string Content { get; set; }
    }

    public class GithubGist : Gist
    {
        public string Forks_Url { get; set; }
        public string Commits_Url { get; set; }
        public int Comments { get; set; }
        public string Comments_Url { get; set; }
        public bool Truncated { get; set; }
        public GithubUser Owner { get; set; }
    }

    public class GistUser
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Avatar_Url { get; set; }
        public string Gravatar_Id { get; set; }
        public string Url { get; set; }
        public string Html_Url { get; set; }
        public string Gists_Url { get; set; }
        public string Type { get; set; }
        public bool Site_Admin { get; set; }
    }

    public class GithubUser : GistUser
    {
        public string Node_Id { get; set; }
        public string Followers_Url { get; set; }
        public string Following_Url { get; set; }
        public string Starred_Url { get; set; }
        public string Subscriptions_Url { get; set; }
        public string Organizations_Url { get; set; }
        public string Repos_Url { get; set; }
        public string Events_Url { get; set; }
        public string Received_Events_Url { get; set; }
    }

    internal static class GithubGatewayExtensions
    {
        public static bool IsUrl(this string gistId) => gistId.IndexOf("://", StringComparison.Ordinal) >= 0;
    }
}