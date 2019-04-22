using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.IO;

namespace ServiceStack.Script
{
    public class GithubPlugin : IScriptPlugin
    {
        public void Register(ScriptContext context)
        {
            context.ScriptMethods.Add(new GithubScripts());
        }
    }
    
    public class GithubScripts : ScriptMethods
    {
        public GistVirtualFiles gistVirtualFiles(string gistId) => new GistVirtualFiles(gistId);

        public GistVirtualFiles gistVirtualFiles(string gistId, string accessToken) =>
            new GistVirtualFiles(gistId, accessToken);

        public GithubGateway githubGateway() => new GithubGateway();
        public GithubGateway githubGateway(string accessToken) => new GithubGateway(accessToken);

        public string githubSourceZipUrl(GithubGateway gateway, string orgNames, string name) =>
            gateway.GetSourceZipUrl(orgNames, name);

        public Task<object> githubSourceRepos(GithubGateway gateway, string orgName) =>
            Task.FromResult<object>(gateway.GetSourceReposAsync(orgName));
        
        public Task<object> githubUserAndOrgRepos(GithubGateway gateway, string githubOrgOrUser) =>
            Task.FromResult<object>(gateway.GetUserAndOrgReposAsync(githubOrgOrUser));

        public List<GithubRepo> githubUserRepos(GithubGateway gateway, string githubUser) =>
            gateway.GetUserRepos(githubUser);
        
        public List<GithubRepo> githubOrgRepos(GithubGateway gateway, string githubOrg) =>
            gateway.GetOrgRepos(githubOrg);

        public GithubGist githubCreateGist(GithubGateway gateway, string description, Dictionary<string, string> files) => 
            gateway.CreateGithubGist(description:description, isPublic:true, files:files);

        public GithubGist githubCreatePrivateGist(GithubGateway gateway, string description, Dictionary<string, string> files) => 
            gateway.CreateGithubGist(description:description, isPublic:false, files:files);

        public GithubGist githubGist(GithubGateway gateway, string gistId) =>
            gateway.GetGithubGist(gistId);

        public IgnoreResult githubWriteGistFiles(GithubGateway gateway, string gistId, Dictionary<string, string> gistFiles)
        {
            gateway.WriteGistFiles(gistId, gistFiles);
            return IgnoreResult.Value;
        }

        public IgnoreResult githubWriteGistFile(GithubGateway gateway, string gistId, string filePath, string contents)
        {
            gateway.WriteGistFile(gistId, filePath, contents);
            return IgnoreResult.Value;
        }
        
        public IgnoreResult githuDeleteGistFiles(GithubGateway gateway, string gistId, string filePath)
        {
            gateway.DeleteGistFiles(gistId, filePath);
            return IgnoreResult.Value;
        }
        
        public IgnoreResult githuDeleteGistFiles(GithubGateway gateway, string gistId, IEnumerable<string> filePaths)
        {
            gateway.DeleteGistFiles(gistId, filePaths.ToArray());
            return IgnoreResult.Value;
        }
    }
}