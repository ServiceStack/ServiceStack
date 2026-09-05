using System.Collections.Generic;
using System.IO;

namespace ServiceStack;

public static class DirectoryInfoExtensions
{
    public static IEnumerable<string> GetMatchingFiles(this DirectoryInfo rootDirPath, string fileSearchPattern)
    {
        if (rootDirPath == null)
            return System.Array.Empty<string>();

        return GetMatchingFiles(rootDirPath.FullName, fileSearchPattern);
    }

    public static IEnumerable<string> GetMatchingFiles(string rootDirPath, string fileSearchPattern)
    {
        if (string.IsNullOrEmpty(rootDirPath) || !Directory.Exists(rootDirPath))
            yield break;

        var pending = new Queue<string>();
        pending.Enqueue(rootDirPath);

        while (pending.Count > 0)
        {
            rootDirPath = pending.Dequeue();
            string[] paths;
            try
            {
                paths = Directory.GetFiles(rootDirPath, fileSearchPattern);
            }
            catch (System.Exception)
            {
                continue;
            }

            foreach (var filePath in paths)
            {
                yield return filePath;
            }

            try
            {
                paths = Directory.GetDirectories(rootDirPath);
            }
            catch (System.Exception)
            {
                continue;
            }

            foreach (var dirPath in paths)
            {
                try
                {
                    var dirAttrs = File.GetAttributes(dirPath);
                    var isRecurseSymLink = (dirAttrs & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

                    if (!isRecurseSymLink)
                    {
                        pending.Enqueue(dirPath);
                    }
                }
                catch (System.Exception)
                {
                    // Ignore inaccessible directories
                }
            }
        }
    }
}
