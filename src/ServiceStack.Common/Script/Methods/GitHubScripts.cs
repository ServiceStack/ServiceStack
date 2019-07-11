using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.IO;

namespace ServiceStack.Script
{
    public class GitHubPlugin : IScriptPlugin
    {
        public void Register(ScriptContext context)
        {
            context.ScriptMethods.Add(new GitHubScripts());
        }
    }
    
    public class GitHubScripts : ScriptMethods
    {
        public GistVirtualFiles gistVirtualFiles(string gistId) => new GistVirtualFiles(gistId);

        public GistVirtualFiles gistVirtualFiles(string gistId, string accessToken) =>
            new GistVirtualFiles(gistId, accessToken);

        public GitHubGateway githubGateway() => new GitHubGateway();
        public GitHubGateway githubGateway(string accessToken) => new GitHubGateway(accessToken);

        public string githubSourceZipUrl(GitHubGateway gateway, string orgNames, string name) =>
            gateway.GetSourceZipUrl(orgNames, name);

        public Task<object> githubSourceRepos(GitHubGateway gateway, string orgName) =>
            Task.FromResult<object>(gateway.GetSourceReposAsync(orgName));
        
        public Task<object> githubUserAndOrgRepos(GitHubGateway gateway, string githubOrgOrUser) =>
            Task.FromResult<object>(gateway.GetUserAndOrgReposAsync(githubOrgOrUser));

        public List<GithubRepo> githubUserRepos(GitHubGateway gateway, string githubUser) =>
            gateway.GetUserRepos(githubUser);
        
        public List<GithubRepo> githubOrgRepos(GitHubGateway gateway, string githubOrg) =>
            gateway.GetOrgRepos(githubOrg);

        public GithubGist githubCreateGist(GitHubGateway gateway, string description, Dictionary<string, string> files) => 
            gateway.CreateGithubGist(description:description, isPublic:true, textFiles:files);

        public GithubGist githubCreatePrivateGist(GitHubGateway gateway, string description, Dictionary<string, string> files) => 
            gateway.CreateGithubGist(description:description, isPublic:false, textFiles:files);

        public GithubGist githubGist(GitHubGateway gateway, string gistId) =>
            gateway.GetGithubGist(gistId);

        public IgnoreResult githubWriteGistFiles(GitHubGateway gateway, string gistId, Dictionary<string, string> gistFiles)
        {
            gateway.WriteGistFiles(gistId, gistFiles);
            return IgnoreResult.Value;
        }

        public IgnoreResult githubWriteGistFile(GitHubGateway gateway, string gistId, string filePath, string contents)
        {
            gateway.WriteGistFile(gistId, filePath, contents);
            return IgnoreResult.Value;
        }
        
        public IgnoreResult githuDeleteGistFiles(GitHubGateway gateway, string gistId, string filePath)
        {
            gateway.DeleteGistFiles(gistId, filePath);
            return IgnoreResult.Value;
        }
        
        public IgnoreResult githuDeleteGistFiles(GitHubGateway gateway, string gistId, IEnumerable<string> filePaths)
        {
            gateway.DeleteGistFiles(gistId, filePaths.ToArray());
            return IgnoreResult.Value;
        }
    }
}