#if !SILVERLIGHT 
using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Common
{
    public static class DirectoryInfoExtensions
    {
        public static IEnumerable<string> GetMatchingFiles(this DirectoryInfo rootDirPath, string fileSearchPattern)
        {
            return GetMatchingFiles(rootDirPath.FullName, fileSearchPattern);
        }

        public static IEnumerable<string> GetMatchingFiles(string rootDirPath, string fileSearchPattern)
        {
            var pending = new Queue<string>();
            pending.Enqueue(rootDirPath);
            string[] paths; 

            while (pending.Count > 0)
            {
                rootDirPath = pending.Dequeue();
                paths = Directory.GetFiles(rootDirPath, fileSearchPattern);
                foreach (var filePath in paths) {
                    yield return filePath;
                }
                paths = Directory.GetDirectories(rootDirPath);
                foreach (var dirPath in paths)
                {
                    var dirAttrs = File.GetAttributes(dirPath);
                    var isRecurseSymLink = (dirAttrs & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

                    if (!isRecurseSymLink)
                    {
                        pending.Enqueue(dirPath);
                    }
                }
            }
        }		 

    }

}
#endif