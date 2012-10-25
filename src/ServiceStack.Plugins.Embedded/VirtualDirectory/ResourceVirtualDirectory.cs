﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ServiceStack.Common.Extensions;
using ServiceStack.VirtualPath;
using ServiceStack.Text;

namespace ServiceStack.Plugins.Embedded.VirtualPath
{
    public class ResourceVirtualDirectory : AbstractVirtualDirectoryBase
    {
        protected Assembly backingAssembly;

        protected List<ResourceVirtualDirectory> SubDirectories;
        protected List<ResourceVirtualFile> SubFiles;

        public override IEnumerable<IVirtualFile> Files { get { return SubFiles.Cast<IVirtualFile>(); } }
        public override IEnumerable<IVirtualDirectory> Directories
        {
            get
            {
                return SubDirectories.Cast<IVirtualDirectory>();
            }
        }
        
        public override string Name { get { return DirectoryName; } }

        public string DirectoryName { get; set; }

        public override DateTime LastModified
        {
            get { return GetLastWriteTimeOfBackingAsm(); }
        }

        internal Assembly BackingAssembly { get { return backingAssembly; } }

        public ResourceVirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDir, Assembly backingAsm)
            : this(owningProvider, parentDir, backingAsm, backingAsm.GetName().Name, backingAsm.GetManifestResourceNames()) { }

        public ResourceVirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDir, Assembly backingAsm, String directoryName, IEnumerable<String> manifestResourceNames)
            : base(owningProvider, parentDir)
        {
            if (backingAsm == null)
                throw new ArgumentNullException("backingAsm");

            if (string.IsNullOrEmpty(directoryName))
                throw new ArgumentException("directoryName");

            this.backingAssembly = backingAsm;
            this.DirectoryName = directoryName;

            InitializeDirectoryStructure(manifestResourceNames);
        }

        protected void InitializeDirectoryStructure(IEnumerable<String> manifestResourceNames)
        {
            SubDirectories = new List<ResourceVirtualDirectory>();
            SubFiles = new List<ResourceVirtualFile>();

            var rootNamespace = backingAssembly.GetName().Name;
            var resourceNames = manifestResourceNames
                .ConvertAll(n => n.Replace(rootNamespace, "").TrimStart('.'));

            SubFiles.AddRange(resourceNames
                .Where(n => n.Count(c => c == '.') <= 1)
                .Select(CreateVirtualFile)
                .OrderBy(f => f.Name));

            SubDirectories.AddRange(resourceNames
                .Where(n => n.Count(c => c == '.') > 1)
                .GroupByFirstToken(pathSeparator: '.')
                .Select(CreateVirtualDirectory)
                .OrderBy(d => d.Name));
        }

        private DateTime GetLastWriteTimeOfBackingAsm()
        {
            var fInfo = new FileInfo(BackingAssembly.Location);
            return fInfo.LastWriteTime;
        }

        protected virtual ResourceVirtualDirectory CreateVirtualDirectory(IGrouping<string, string[]> subResources)
        {
            var remainingResourceNames = subResources.Select(g => g[1]);
            var subDir = new ResourceVirtualDirectory(
                VirtualPathProvider, this, backingAssembly, subResources.Key, remainingResourceNames);

            return subDir;
        }

        protected virtual ResourceVirtualFile CreateVirtualFile(String resourceName)
        {
#if NET_40
            Contract.Requires(!String.IsNullOrEmpty(resourceName));
#endif

            try
            {
                var fullResourceName = String.Concat(RealPath, VirtualPathProvider.RealPathSeparator, resourceName);
                var mrInfo = backingAssembly.GetManifestResourceInfo(fullResourceName);
                if (mrInfo == null)
                    throw new FileNotFoundException("Virtual file not found", fullResourceName);

                return new ResourceVirtualFile(VirtualPathProvider, this, resourceName);
            }
            catch (Exception ex)
            {
                ex.Message.Print();
                throw;
            }
        }

        protected virtual ResourceVirtualDirectory ConsumeTokensForVirtualDir(Stack<string> resourceTokens)
        {
#if NET_40
            Contract.Requires(resourceTokens.Count > 1);
#endif
            var subDirName = resourceTokens.Pop();
            throw new NotImplementedException();
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            return Directories.Cast<IVirtualNode>().Union<IVirtualNode>(Files.Cast<IVirtualNode>()).GetEnumerator();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
        {
            return Files.FirstOrDefault(f => f.Name == fileName);
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(String globPattern)
        {
            return Files.Where(f => f.Name.Glob(globPattern));
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return Directories.FirstOrDefault(d => d.Name == directoryName);
        }

        protected override string GetRealPathToRoot()
        {
            var path = base.GetRealPathToRoot();
            return path.TrimStart('.');
        }
    }
}
