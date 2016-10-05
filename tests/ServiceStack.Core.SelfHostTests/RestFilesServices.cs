using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RestFiles.ServiceModel;
using RestFiles.ServiceModel.Types;
using ServiceStack;
using ServiceStack.VirtualPath;

namespace RestFiles.ServiceModel
{
    [Route("/files")]
    [Route("/files/{Path*}")]
    public class Files
    {
        public string Path { get; set; }
        public string TextContents { get; set; }
        public bool ForDownload { get; set; }
    }

    /// <summary>
    /// Define your ServiceStack web service response (i.e. Response DTO).
    /// </summary>
    public class FilesResponse : IHasResponseStatus
    {
        public FolderResult Directory { get; set; }
        public FileResult File { get; set; }

        /// <summary>
        /// Gets or sets the ResponseStatus. The built-in IoC used with ServiceStack autowires this property.
        /// </summary>		 
        public ResponseStatus ResponseStatus { get; set; }
    }
}

namespace RestFiles.ServiceModel.Types
{
    public class File
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsTextFile { get; set; }
    }

    public class FileResult
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsTextFile { get; set; }
        public string Contents { get; set; }
    }
    public class Folder
    {
        public string Name { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int FileCount { get; set; }
    }
    public class FolderResult
    {
        public FolderResult()
        {
            Folders = new List<Folder>();
            Files = new List<File>();
        }

        public List<Folder> Folders { get; set; }
        public List<File> Files { get; set; }
    }
}

namespace RestFiles.ServiceInterface
{
    public class AppConfig
    {
        public string RootDirectory { get; set; }

        public List<string> TextFileExtensions { get; set; }

        public List<string> ExcludeDirectories { get; set; }
    }

    /// <summary>
    /// Define your ServiceStack web service request (i.e. Request DTO).
    /// </summary>
    public class FilesService : Service
    {
        public AppConfig Config { get; set; }

        public object Get(Files request)
        {
            var targetPath = GetAndValidateExistingPath(request);

            var isDirectory = VirtualFiles.IsDirectory(targetPath);

            if (!isDirectory && request.ForDownload)
                return new HttpResult(VirtualFiles.GetFile(targetPath), asAttachment: true);

            var response = isDirectory
                ? new FilesResponse { Directory = GetFolderResult(targetPath) }
                : new FilesResponse { File = GetFileResult(targetPath) };

            return response;
        }

        public object Post(Files request)
        {
            var targetDir = GetPath(request);

            if (VirtualFiles.IsFile(targetDir))
                throw new NotSupportedException(
                "POST only supports uploading new files. Use PUT to replace contents of an existing file");

            foreach (var uploadedFile in base.Request.Files)
            {
                var newFilePath = targetDir.CombineWith(uploadedFile.FileName);
                VirtualFiles.WriteFile(newFilePath, uploadedFile.InputStream);
            }

            return new FilesResponse();
        }

        public void Put(Files request)
        {
            var targetFile = VirtualFiles.GetFile(GetAndValidateExistingPath(request));

            if (!Config.TextFileExtensions.Contains(targetFile.Extension))
                throw new NotSupportedException("PUT Can only update text files, not: " + targetFile.Extension);

            if (request.TextContents == null)
                throw new ArgumentNullException("TextContents");

            VirtualFiles.WriteFile(targetFile.VirtualPath, request.TextContents);
        }

        public void Delete(Files request)
        {
            var targetFile = GetAndValidateExistingPath(request);
            VirtualFiles.DeleteFile(targetFile);
        }

        private FolderResult GetFolderResult(string targetPath)
        {
            var result = new FolderResult();

            var dir = VirtualFiles.GetDirectory(targetPath);
            foreach (var subDir in dir.Directories)
            {
                if (Config.ExcludeDirectories.Contains(subDir.Name)) continue;

                result.Folders.Add(new Folder
                {
                    Name = subDir.Name,
                    ModifiedDate = subDir.LastModified,
                    FileCount = subDir.GetFiles().Count(),
                });
            }

            foreach (var fileInfo in dir.GetFiles())
            {
                result.Files.Add(new ServiceModel.Types.File
                {
                    Name = fileInfo.Name,
                    Extension = fileInfo.Extension,
                    FileSizeBytes = fileInfo.Length,
                    ModifiedDate = fileInfo.LastModified,
                    IsTextFile = Config.TextFileExtensions.Contains(fileInfo.Extension),
                });
            }

            return result;
        }

        private string GetPath(Files request)
        {
            return Config.RootDirectory.CombineWith(GetSafePath(request.Path));
        }

        private string GetAndValidateExistingPath(Files request)
        {
            var targetPath = GetPath(request);
            if (!VirtualFiles.IsFile(targetPath) && !VirtualFiles.IsDirectory(targetPath))
                throw new HttpError(HttpStatusCode.NotFound, new FileNotFoundException("Could not find: " + request.Path));

            return targetPath;
        }

        private FileResult GetFileResult(string filePath)
        {
            var file = VirtualFiles.GetFile(filePath);
            var isTextFile = Config.TextFileExtensions.Contains(file.Extension);

            return new FileResult
            {
                Name = file.Name,
                Extension = file.Extension,
                FileSizeBytes = file.Length,
                IsTextFile = isTextFile,
                Contents = isTextFile ? VirtualFiles.GetFile(file.VirtualPath).ReadAllText() : null,
                ModifiedDate = file.LastModified,
            };
        }

        public static string GetSafePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return string.Empty;

            //Strip invalid chars
            foreach (var invalidChar in Path.GetInvalidPathChars())
            {
                filePath = filePath.Replace(invalidChar.ToString(), string.Empty);
            }

            return filePath
                .TrimStart('.', '/', '\\')                  //Remove illegal chars at the start
                .Replace('\\', '/')                         //Switch all to use the same seperator
                .Replace("../", string.Empty)               //Remove access to top-level directories anywhere else 
                .Replace('/', Path.DirectorySeparatorChar); //Switch all to use the OS seperator
        }
    }
}