using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack
{
    public interface IGistGateway
    {
        Gist CreateGist(string description, bool isPublic, Dictionary<string, object> files);
        Gist CreateGist(string description, bool isPublic, Dictionary<string, string> textFiles);
        Gist GetGist(string gistId);
        Gist GetGist(string gistId, string version);
        Task<Gist> GetGistAsync(string gistId);
        Task<Gist> GetGistAsync(string gistId, string version);
        void WriteGistFiles(string gistId, Dictionary<string, object> files, string description=null, bool deleteMissing=false);
        void WriteGistFiles(string gistId, Dictionary<string, string> textFiles, string description=null, bool deleteMissing=false);
        void CreateGistFile(string gistId, string filePath, string contents);
        void WriteGistFile(string gistId, string filePath, string contents);
        void DeleteGistFiles(string gistId, params string[] filePaths);
    }

    public interface IGitHubGateway : IGistGateway 
    {
        Tuple<string,string> FindRepo(string[] orgs, string name, bool useFork=false);
        string GetSourceZipUrl(string user, string repo);
        Task<string> GetSourceZipUrlAsync(string user, string repo);
        Task<List<GithubRepo>> GetSourceReposAsync(string orgName);
        Task<List<GithubRepo>> GetUserAndOrgReposAsync(string githubOrgOrUser);
        GithubRepo GetRepo(string userOrOrg, string repo);
        Task<GithubRepo> GetRepoAsync(string userOrOrg, string repo);
        List<GithubRepo> GetUserRepos(string githubUser);
        Task<List<GithubRepo>> GetUserReposAsync(string githubUser);
        List<GithubRepo> GetOrgRepos(string githubOrg);
        Task<List<GithubRepo>> GetOrgReposAsync(string githubOrg);
        string GetJson(string route);
        T GetJson<T>(string route);
        Task<string> GetJsonAsync(string route);
        Task<T> GetJsonAsync<T>(string route);
        IEnumerable<T> StreamJsonCollection<T>(string route);
        Task<List<T>> GetJsonCollectionAsync<T>(string route);
        void DownloadFile(string downloadUrl, string fileName);
    }

    public class GithubRateLimit
    {
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public long Reset { get; set; }
        public int Used { get; set; }
    }
    public class GithubResourcesRateLimits
    {
        public GithubRateLimit Core { get; set; }
        public GithubRateLimit Graphql { get; set; }
        public GithubRateLimit Integration_Manifest { get; set; }
        public GithubRateLimit Search { get; set; }
    }
    public class GithubRateLimits
    {
        public GithubResourcesRateLimits Resources { get; set; }
    }

    public class GitHubGateway : IGistGateway, IGitHubGateway
    {
        public string UserAgent { get; set; } = nameof(GitHubGateway);
        public const string ApiBaseUrl = "https://api.github.com/";
        public string BaseUrl { get; set; } = ApiBaseUrl;

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

        public async Task<GithubRateLimits> GetRateLimitsAsync()
            => await GetJsonAsync<GithubRateLimits>("/rate_limit").ConfigAwait();

        public GithubRateLimits GetRateLimits()
            => GetJson<GithubRateLimits>("/rate_limit");

        internal string UnwrapRepoFullName(string orgName, string name, bool useFork=false)
        {
            try
            {
                var repo = GetJson<GithubRepo>($"/repos/{orgName}/{name}");
                if (useFork && repo.Fork)
                {
                    if (repo.Parent != null)
                        return repo.Parent.Full_Name;
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

        public virtual Tuple<string,string> AssertRepo(string[] orgs, string name, bool useFork=false) =>
            FindRepo(orgs, name, useFork)
            ?? throw new Exception($"'{name}' was not found in sources: {orgs.Join(", ")}");

        public virtual Tuple<string,string> FindRepo(string[] orgs, string name, bool useFork=false)
        {
            foreach (var orgName in orgs)
            {
                var repoFullName = UnwrapRepoFullName(orgName, name, useFork);
                if (repoFullName == null)
                    continue;

                var user = repoFullName.LeftPart('/');
                var repo = repoFullName.RightPart('/');
                return Tuple.Create(user, repo);
            }
            return null;
        }

        public virtual string GetSourceZipUrl(string user, string repo) => 
            GetSourceZipReleaseUrl(user, repo, GetJson($"repos/{user}/{repo}/releases"));

        public virtual string GetSourceZipUrl(string user, string repo, string tag) => 
            $"https://github.com/{user}/{repo}/archive/refs/tags/{tag}.zip";

        public virtual async Task<string> GetSourceZipUrlAsync(string user, string repo) => 
            GetSourceZipReleaseUrl(user, repo, await GetJsonAsync($"repos/{user}/{repo}/releases").ConfigAwait());

        private static string GetSourceZipReleaseUrl(string user, string repo, string json)
        {
            var response = JSON.parse(json);

            if (response is List<object> releases && 
                releases.Count > 0 &&
                releases[0] is Dictionary<string, object> release &&
                release.TryGetValue("zipball_url", out var zipUrl))
            {
                return (string) zipUrl;
            }

            return $"https://github.com/{user}/{repo}/archive/master.zip";
        }

        public virtual async Task<List<GithubRepo>> GetSourceReposAsync(string orgName)
        {
            var repos = (await GetUserAndOrgReposAsync(orgName).ConfigAwait())
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
                foreach (var repos in await userRepos.ConfigAwait())
                foreach (var repo in repos)
                    map[repo.Name] = repo;
            }
            catch (Exception e)
            {
                if (!e.IsNotFound()) throw;
            }

            try
            {
                foreach (var repos in await orgRepos.ConfigAwait())
                foreach (var repo in repos)
                    map[repo.Name] = repo;
            }
            catch (Exception e)
            {
                if (!e.IsNotFound()) throw;
            }

            return map.Values.ToList();
        }
        
        public virtual GithubRepo GetRepo(string userOrOrg, string repo) =>
            GetJson<GithubRepo>($"/{userOrOrg}/{repo}");

        public virtual Task<GithubRepo> GetRepoAsync(string userOrOrg, string repo) =>
            GetJsonAsync<GithubRepo>($"/{userOrOrg}/{repo}");

        public virtual List<GithubRepo> GetUserRepos(string githubUser) =>
            StreamJsonCollection<List<GithubRepo>>($"users/{githubUser}/repos").SelectMany(x => x).ToList();

        public virtual async Task<List<GithubRepo>> GetUserReposAsync(string githubUser) =>
            (await GetJsonCollectionAsync<List<GithubRepo>>($"users/{githubUser}/repos").ConfigAwait()).SelectMany(x => x).ToList();

        public virtual List<GithubRepo> GetOrgRepos(string githubOrg) =>
            StreamJsonCollection<List<GithubRepo>>($"orgs/{githubOrg}/repos").SelectMany(x => x).ToList();
        
        public virtual async Task<List<GithubRepo>> GetOrgReposAsync(string githubOrg) =>
            (await GetJsonCollectionAsync<List<GithubRepo>>($"orgs/{githubOrg}/repos").ConfigAwait()).SelectMany(x => x).ToList();
        
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

            return await apiUrl.GetJsonFromUrlAsync(ApplyRequestFilters).ConfigAwait();
        }

        public virtual async Task<T> GetJsonAsync<T>(string route) => 
            (await GetJsonAsync(route).ConfigAwait()).FromJson<T>();

        public virtual IEnumerable<T> StreamJsonCollection<T>(string route)
        {
            List<T> results;
            var nextUrl = BaseUrl.CombineWith(route);

            do
            {
                results = nextUrl.GetJsonFromUrl(ApplyRequestFilters,
                        responseFilter: res => {
                            var links = ParseLinkUrls(res.GetHeader("Link"));
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
                            var links = ParseLinkUrls(res.GetHeader("Link"));
                            links.TryGetValue("next", out nextUrl);
                        }).ConfigAwait())
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
            downloadUrl.DownloadFileTo(fileName, headers:new() {
                new(HttpHeaders.UserAgent, UserAgent),
                new(HttpHeaders.Authorization, "token " + AccessToken),
            });
        }

        public virtual GithubGist GetGithubGist(string gistId)
        {
            var json = GetJson($"/gists/{gistId}");
            return json.FromJson<GithubGist>();
        }

        public virtual GithubGist GetGithubGist(string gistId, string version)
        {
            var json = GetJson($"/gists/{gistId}/{version}");
            return json.FromJson<GithubGist>();
        }

        public virtual Gist GetGist(string gistId)
        {
            var response = GetGithubGist(gistId);
            return PopulateGist(response);
        }

        public Gist GetGist(string gistId, string version)
        {
            var response = GetGithubGist(gistId, version);
            return PopulateGist(response);
        }

        public async Task<Gist> GetGistAsync(string gistId)
        {
            var response = await GetJsonAsync<GithubGist>($"/gists/{gistId}").ConfigAwait();
            return PopulateGist(response);
        }

        public async Task<Gist> GetGistAsync(string gistId, string version)
        {
            var response = await GetJsonAsync<GithubGist>($"/gists/{gistId}/{version}").ConfigAwait();
            return PopulateGist(response);
        }

        private GithubGist PopulateGist(GithubGist response)
        {
            if (response != null)
                response.UserId = response.Owner?.Login;

            return response;
        }
        
        public virtual Gist CreateGist(string description, bool isPublic, Dictionary<string, object> files) =>
            CreateGithubGist(description, isPublic, files);
        
        public virtual Gist CreateGist(string description, bool isPublic, Dictionary<string, string> textFiles) =>
            CreateGithubGist(description, isPublic, textFiles);

        public virtual GithubGist CreateGithubGist(string description, bool isPublic, Dictionary<string, object> files) =>
            CreateGithubGist(description, isPublic, ToTextFiles(files));

        public virtual GithubGist CreateGithubGist(string description, bool isPublic, Dictionary<string, string> textFiles)
        {
            AssertAccessToken();
            
            var sb = StringBuilderCache.Allocate()
                .Append("{\"description\":")
                .Append(description.ToJson())
                .Append(",\"public\":")
                .Append(isPublic ? "true" : "false")
                .Append(",\"files\":{");
            
            var i = 0;
            foreach (var entry in textFiles)
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

        public static bool IsDirSep(char c) => c == '\\' || c == '/';

        internal static string SanitizePath(string filePath)
        {
            var sanitizedPath = string.IsNullOrEmpty(filePath)
                ? null
                : (IsDirSep(filePath[0]) ? filePath.Substring(1) : filePath);

            return sanitizedPath?.Replace('/', '\\');
        }

        internal static string ToBase64(ReadOnlyMemory<byte> bytes) => MemoryProvider.Instance.ToBase64(bytes);

        internal static string ToBase64(byte[] bytes) => Convert.ToBase64String(bytes);
        internal static string ToBase64(Stream stream)
        {
            var base64 = stream is MemoryStream ms
                ? MemoryProvider.Instance.ToBase64(ms.GetBufferAsMemory())
                : Convert.ToBase64String(stream.ReadFully());
            return base64;
        }

        public static Dictionary<string, string> ToTextFiles(Dictionary<string, object> files)
        {
            string ToBase64ThenDispose(Stream stream)
            {
                using (stream)
                    return ToBase64(stream);
            }

            var gistFiles = new Dictionary<string, string>();
            foreach (var entry in files)
            {
                if (entry.Value == null)
                    continue;

                var filePath = SanitizePath(entry.Key);

                var base64 = entry.Value is string || entry.Value is ReadOnlyMemory<char>
                    ? null
                    : entry.Value is byte[] bytes
                        ? ToBase64(bytes)
                        : entry.Value is ReadOnlyMemory<byte> romBytes
                        ? ToBase64(romBytes)
                        : entry.Value is Stream stream
                            ? ToBase64ThenDispose(stream)
                            : entry.Value is IVirtualFile file &&
                              MimeTypes.IsBinary(MimeTypes.GetMimeType(file.Extension))
                                ? ToBase64(file.ReadAllBytes())
                                : null;

                if (base64 != null)
                    filePath += "|base64";

                var textContents = base64 ??
                   (entry.Value is string text
                       ? text
                       : entry.Value is ReadOnlyMemory<char> romChar 
                           ? romChar.ToString()
                           : throw CreateContentNotSupportedException(entry.Value));

                gistFiles[filePath] = textContents;
            }
            return gistFiles;
        }

        internal static NotSupportedException CreateContentNotSupportedException(object value) =>
            new($"Could not write '{value?.GetType().Name ?? "null"}' value. Only string, byte[], Stream or IVirtualFile content is supported.");

        public virtual void WriteGistFiles(string gistId, Dictionary<string, object> files, string description=null, bool deleteMissing=false) =>
            WriteGistFiles(gistId, ToTextFiles(files), description, deleteMissing);
        
        /// <summary>
        /// Create or Write Gist Text Files. Requires AccessToken
        /// </summary>
        public virtual void WriteGistFiles(string gistId, Dictionary<string,string> textFiles, string description=null, bool deleteMissing=false)
        {
            AssertAccessToken();

            var i = 0;
            var sb = StringBuilderCache.Allocate().Append("{\"files\":{");
            foreach (var entry in textFiles)
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

            if (deleteMissing)
            {
                var gist = GetGist(gistId);
                foreach (var existingFile in gist.Files.Keys)
                {
                    if (textFiles.ContainsKey(existingFile)) 
                        continue;
                    
                    if (i++ > 0)
                        sb.Append(",");
                
                    sb.Append(existingFile.ToJson())
                        .Append(":null");
                }
            }
            sb.Append("}");

            if (!string.IsNullOrEmpty(description))
            {
                if (i++ > 0)
                    sb.Append(",");
                sb.Append("\"description\":").Append(description.ToJson());
            }
            sb.Append("}");
                
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

#if NET6_0_OR_GREATER
        public virtual void ApplyRequestFilters(System.Net.Http.HttpRequestMessage req)
        {
            req.With(c => {
                c.UserAgent = UserAgent;
                if (!string.IsNullOrEmpty(AccessToken))
                    c.Authorization = new("token", AccessToken);
            });
        }
#else
        public virtual void ApplyRequestFilters(HttpWebRequest req)
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                req.Headers["Authorization"] = "token " + AccessToken;
            }
            req.UserAgent = UserAgent;
        }
#endif
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
        public DateTime Created_At { get; set; }
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
        public DateTime Created_At { get; set; }
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
        public GistHistory[] History { get; set; }
    }

    public class GistHistory
    {
        public GithubUser User { get; set; }
        public string Version { get; set; }
        public DateTime Committed_At { get; set; }
        public GistChangeStatus Change_Status { get; set; }
        public string Url { get; set; }
    }

    public class GistChangeStatus
    {
        public int Total { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
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
    
    public class GistLink
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string User { get; set; }
        public string To { get; set; }
        public string Description { get; set; }

        public string[] Tags { get; set; }

        public string GistId { get; set; }

        public string Repo { get; set; }
        
        public Dictionary<string,object> Modifiers { get; set; }

        public string ToTagsString() => Tags == null ? "" : $"[" + string.Join(",", Tags) + "]";

        public override string ToString() => $"{Name.PadRight(18, ' ')} {Description} {ToTagsString()}";

        public string ToListItem()
        {
            var sb = new StringBuilder(" - [")
                .Append(Name)
                .Append("](")
                .Append(Url)
                .Append(") {")
                .Append(!string.IsNullOrEmpty(To) ? "to:" + To.ToJson() : "")
                .Append("} `")
                .Append(Tags != null ? string.Join(",", Tags) : "")
                .Append("` ")
                .Append(Description);

            return sb.ToString();
        }

        public static string RenderLinks(List<GistLink> links)
        {
            var sb = new StringBuilder();
            foreach (var link in links)
            {
                sb.AppendLine(link.ToListItem());
            }
            return sb.ToString();
        }

        public static List<GistLink> Parse(string md)
        {
            var to = new List<GistLink>();

            if (!string.IsNullOrEmpty(md))
            {
                foreach (var strLine in md.ReadLines())
                {
                    var line = strLine.AsSpan();
                    if (!line.TrimStart().StartsWith("- ["))
                        continue;

                    line.SplitOnFirst('[', out _, out var startName);
                    startName.SplitOnFirst(']', out var name, out var endName);
                    endName.SplitOnFirst('(', out _, out var startUrl);
                    startUrl.SplitOnFirst(')', out var url, out var endUrl);

                    var afterModifiers = endUrl.ParseJsToken(out var token);
                    
                    var modifiers = new Dictionary<string, object>();
                    if (token is JsObjectExpression obj)
                    {
                        foreach (var jsProperty in obj.Properties)
                        {
                            if (jsProperty.Key is JsIdentifier id)
                            {
                                modifiers[id.Name] = (jsProperty.Value as JsLiteral)?.Value;
                            }
                        }
                    }

                    var toPath = modifiers.TryGetValue("to", out var oValue)
                        ? oValue.ToString()
                        : null;

                    string tags = null;
                    afterModifiers = afterModifiers.TrimStart();
                    if (afterModifiers.StartsWith("`"))
                    {
                        afterModifiers = afterModifiers.Advance(1);
                        var pos = afterModifiers.IndexOf('`');
                        if (pos >= 0)
                        {
                            tags = afterModifiers.Substring(0, pos);
                            afterModifiers = afterModifiers.Advance(pos + 1);
                        }
                    }

                    if (name == null || url == null)
                        continue;

                    var link = new GistLink
                    {
                        Name = name.ToString(),
                        Url = url.ToString(),
                        Modifiers = modifiers,
                        To = toPath,
                        Description = afterModifiers.Trim().ToString(),
                        User = url.Substring("https://".Length).RightPart('/').LeftPart('/'),
                        Tags = tags?.Split(',').Map(x => x.Trim()).ToArray(),
                    };

                    if (TryParseGitHubUrl(link.Url, out var gistId, out var user, out var repo))
                    {
                        link.GistId = gistId;
                        if (user != null)
                        {
                            link.User = user;
                            link.Repo = repo;
                        }
                    }

                    if (link.User == "gistlyn" || link.User == "mythz")
                        link.User = "ServiceStack";

                    to.Add(link);
                }
            }

            return to;
        }

        public static bool TryParseGitHubUrl(string url, out string gistId, out string user, out string repo)
        {
            gistId = user = repo = null;

            if (url.StartsWith("https://gist.github.com"))
            {
                gistId = url.LastRightPart('/');
                return true;
            }

            if (url.StartsWith("https://github.com/"))
            {
                var pathInfo = url.Substring("https://github.com/".Length);
                user = pathInfo.LeftPart('/');
                repo = pathInfo.RightPart('/').LeftPart('/');
                return true;
            }

            return false;
        }

        public static GistLink Get(List<GistLink> links, string gistAlias)
        {
            var sanitizedAlias = gistAlias.Replace("-", "");
            var gistLink = links.FirstOrDefault(x => x.Name.Replace("-", "").EqualsIgnoreCase(sanitizedAlias));
            return gistLink;
        }

        public bool MatchesTag(string tagName)
        {
            if (Tags == null)
                return false;

            var searchTags = tagName.Split(',').Map(x => x.Trim());
            return searchTags.Count == 1
                ? Tags.Any(x => x.EqualsIgnoreCase(tagName))
                : Tags.Any(x => searchTags.Any(x.EqualsIgnoreCase));
        }
    }
    
}