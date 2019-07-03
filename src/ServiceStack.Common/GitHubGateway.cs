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
        Gist CreateGist(string description, bool isPublic, Dictionary<string, string> gistFiles);
        Gist GetGist(string gistId);
        Task<Gist> GetGistAsync(string gistId);
        void WriteGistFiles(string gistId, Dictionary<string, string> files);
        void CreateGistFile(string gistId, string filePath, string contents);
        void WriteGistFile(string gistId, string filePath, string contents);
        void DeleteGistFiles(string gistId, params string[] filePaths);
    }
    
    public class GitHubGateway : IGistGateway
    {
        public string UserAgent { get; set; } = nameof(GitHubGateway);
        public string BaseUrl { get; set; } = "https://api.github.com/";
        public bool UseForkParent { get; set; } = true;

        /// <summary>
        /// AccessTokenSecret
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Intercept and override GitHub JSON API requests
        /// </summary>
        public Func<string, string> GetJsonFilter { get; set; }

        public GitHubGateway() {}
        public GitHubGateway(string accessToken) => AccessToken = accessToken;

        public virtual string UnwrapRepoFullName(string orgName, string name)
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

        public virtual Tuple<string,string> FindRepo(string[] orgs, string name)
        {
            foreach (var orgName in orgs)
            {
                var repoFullName = UnwrapRepoFullName(orgName, name);
                if (repoFullName == null)
                    continue;

                var user = repoFullName.LeftPart('/');
                var repo = repoFullName.RightPart('/');
                return Tuple.Create(user, repo);
            }

            throw new Exception($"'{name}' was not found in sources: {orgs.Join(", ")}");
        }

        public virtual string GetSourceZipUrl(string user, string repo)
        {
            var json = GetJson($"repos/{user}/{repo}/releases");
            var response = JSON.parse(json);

            if (response is List<object> releases && releases.Count > 0 &&
                releases[0] is Dictionary<string, object> release &&
                release.TryGetValue("zipball_url", out var zipUrl))
            {
                return (string) zipUrl;
            }

            return $"https://github.com/{user}/{repo}/archive/master.zip";
        }

        public virtual async Task<List<GithubRepo>> GetSourceReposAsync(string orgName)
        {
            var repos = (await GetUserAndOrgReposAsync(orgName))
                .OrderBy(x => x.Name)
                .ToList();
            return repos;
        }

        public virtual async Task<List<GithubRepo>> GetUserAndOrgReposAsync(string githubOrgOrUser)
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

        public virtual List<GithubRepo> GetUserRepos(string githubUser) =>
            StreamJsonCollection<List<GithubRepo>>($"users/{githubUser}/repos").SelectMany(x => x).ToList();

        public virtual List<GithubRepo> GetOrgRepos(string githubOrg) =>
            StreamJsonCollection<List<GithubRepo>>($"orgs/{githubOrg}/repos").SelectMany(x => x).ToList();
        
        public virtual string GetJson(string route)
        {
            var apiUrl = !route.IsUrl()
                ? BaseUrl.CombineWith(route)
                : route;

            if (GetJsonFilter != null)
                return GetJsonFilter(apiUrl);

            return apiUrl.GetJsonFromUrl(ApplyRequestFilters);
        }

        public virtual T GetJson<T>(string route) => GetJson(route).FromJson<T>();

        public virtual async Task<string> GetJsonAsync(string route)
        {
            var apiUrl = !route.IsUrl()
                ? BaseUrl.CombineWith(route)
                : route;

            if (GetJsonFilter != null)
                return GetJsonFilter(apiUrl);

            return await apiUrl.GetJsonFromUrlAsync(ApplyRequestFilters);
        }

        public virtual async Task<T> GetJsonAsync<T>(string route) => 
            (await GetJsonAsync(route)).FromJson<T>();

        public virtual IEnumerable<T> StreamJsonCollection<T>(string route)
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

        public virtual async Task<List<T>> GetJsonCollectionAsync<T>(string route)
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

        public virtual Dictionary<string, string> ParseLinkUrls(string linkHeader)
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

        public virtual void DownloadFile(string downloadUrl, string fileName)
        {
            var webClient = new WebClient();
            webClient.Headers.Add(HttpHeaders.UserAgent, UserAgent);
            webClient.DownloadFile(downloadUrl, fileName);
        }

        public virtual GithubGist GetGithubGist(string gistId)
        {
            var json = GetJson($"/gists/{gistId}");
            var response = json.FromJson<GithubGist>();
            return response;
        }

        public virtual Gist GetGist(string gistId)
        {
            var response = GetGithubGist(gistId);
            return PopulateGist(response);
        }

        public async Task<Gist> GetGistAsync(string gistId)
        {
            var response = await GetJsonAsync<GithubGist>($"/gists/{gistId}");
            return PopulateGist(response);
        }

        private GithubGist PopulateGist(GithubGist response)
        {
            if (response != null)
                response.UserId = response.Owner?.Login;

            return response;
        }

        public virtual Gist CreateGist(string description, bool isPublic, Dictionary<string, string> gistFiles) =>
            CreateGithubGist(description, isPublic, gistFiles);

        public virtual GithubGist CreateGithubGist(string description, bool isPublic, Dictionary<string, string> files)
        {
            AssertAccessToken();
            
            var sb = StringBuilderCache.Allocate()
                .Append("{\"description\":")
                .Append(description.ToJson())
                .Append(",\"public\":")
                .Append(isPublic ? "true" : "false")
                .Append(",\"files\":{");
            
            var i = 0;
            foreach (var entry in files)
            {
                if (i++ > 0)
                    sb.Append(",");
                
                var jsonFile = entry.Key.ToJson();
                sb.Append(jsonFile)
                    .Append(":{\"content\":")
                    .Append(entry.Value.ToJson())
                    .Append("}");
            }
            sb.Append("}}");

            var json = StringBuilderCache.ReturnAndFree(sb);
            var responseJson = BaseUrl.CombineWith($"/gists")
                .PostJsonToUrl(json, requestFilter: ApplyRequestFilters);

            var response = responseJson.FromJson<GithubGist>();
            return response;
        }
        
        /// <summary>
        /// Create or Write Gist Text Files. Requires AccessToken
        /// </summary>
        public virtual void WriteGistFiles(string gistId, Dictionary<string,string> files)
        {
            AssertAccessToken();

            var i = 0;
            var sb = StringBuilderCache.Allocate().Append("{\"files\":{");
            foreach (var entry in files)
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
        public virtual void CreateGistFile(string gistId, string filePath, string contents)
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
        public virtual void WriteGistFile(string gistId, string filePath, string contents)
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

        protected virtual void AssertAccessToken()
        {
            if (string.IsNullOrEmpty(AccessToken))
                throw new NotSupportedException("An AccessToken is required to modify gist");
        }

        public virtual void DeleteGistFiles(string gistId, params string[] filePaths)
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

        public virtual void ApplyRequestFilters(HttpWebRequest req)
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
        public string Id { get; set; }
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
        public string Node_Id { get; set; }
        public string Git_Pull_Url { get; set; }
        public string Git_Push_Url { get; set; }
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