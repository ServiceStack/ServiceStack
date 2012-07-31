using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Razor.VirtualPath
{
    public class ResourceVirtualDirectory : AbstractVirtualDirectoryBase
    {
        #region Fields

        protected Assembly backingAssembly;
        protected String directoryName;

        protected List<ResourceVirtualDirectory> subDirectories;
        protected List<ResourceVirtualFile> subFiles;

        #endregion

        public ResourceVirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDir, Assembly backingAsm)
            : this(owningProvider, parentDir, backingAsm, backingAsm.GetName().Name, backingAsm.GetManifestResourceNames())
        {   }

        public ResourceVirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDir, Assembly backingAsm, String directoryName, IEnumerable<String> manifestResourceNames) 
            : base(owningProvider, parentDir)
        {
            if (backingAsm == null)
                throw new ArgumentNullException("backingAsm");

            if (String.IsNullOrEmpty(directoryName))
                throw new ArgumentException("directoryName");

            this.backingAssembly = backingAsm;
            this.directoryName = directoryName;

            InitializeDirectoryStructure(manifestResourceNames);
        }

        protected void InitializeDirectoryStructure(IEnumerable<String> manifestResourceNames)
        {
            subDirectories = new List<ResourceVirtualDirectory>();
            subFiles = new List<ResourceVirtualFile>();

            var rootNamespace = backingAssembly.GetName().Name;
            var resourceNames = manifestResourceNames.Select(n => n.Replace(rootNamespace, "")
                                                                   .TrimStart('.'));

            subFiles.AddRange(resourceNames.Where(n => n.Count(c => c == '.') <= 1)
                                           .Select(CreateVirtualFile)
                                           .OrderBy(f => f.Name));

            subDirectories.AddRange(resourceNames.Where(n => n.Count(c => c == '.') > 1)
                                                 .GroupByFirstToken(pathSeparator: '.')
                                                 .Select(CreateVirtualDirectory)
                                                 .OrderBy(d => d.Name));
        }

        protected virtual ResourceVirtualDirectory CreateVirtualDirectory(IGrouping<string, string[]> subResources)
        {
            var remainingResourceNames = subResources.Select(g => g[1]);
            var subDir = new ResourceVirtualDirectory(virtualPathProvider, this,
                                                      backingAssembly,
                                                      subResources.Key,
                                                      remainingResourceNames);

            return subDir;
        }

        protected virtual ResourceVirtualFile CreateVirtualFile(String resourceName)
        {
            Contract.Requires(! String.IsNullOrEmpty(resourceName));

            var fullResourceName = String.Concat(RealPath, virtualPathProvider.RealPathSeparator, resourceName);
            var mrInfo = backingAssembly.GetManifestResourceInfo(fullResourceName);
            if (mrInfo == null)
                throw new FileNotFoundException("Virtual file not found", fullResourceName);

            return new ResourceVirtualFile(virtualPathProvider, this, resourceName);
        }

        protected virtual ResourceVirtualDirectory ConsumeTokensForVirtualDir(Stack<string> resourceTokens)
        {
            Contract.Requires(resourceTokens.Count > 1);
            var subDirName = resourceTokens.Pop();

            throw new NotImplementedException();
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            return Enumerable.Union<IVirtualNode>(Directories, Files)
                             .GetEnumerator();
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

        #region Properties

        public override IEnumerable<IVirtualFile> Files { get { return subFiles; } }
        public override IEnumerable<IVirtualDirectory> Directories { get { return subDirectories; } }

        public override string Name { get { return directoryName; } }

        internal Assembly BackingAssembly { get { return backingAssembly; } }

        #endregion

    }
}
