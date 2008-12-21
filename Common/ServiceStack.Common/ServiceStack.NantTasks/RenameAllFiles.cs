using System.Collections.Generic;
using System.IO;
using NAnt.Core.Attributes;

namespace ServiceStack.NantTasks
{
    [TaskName("renameAllFiles")]
    public class RenameAllFiles : ReplaceNantTask
    {
        protected override void OnEachDirectory(DirectoryInfo dirInfo,
            IList<string> oldValues, IList<string> newValues)
        {
            var fileInfos = dirInfo.GetFiles();
            foreach (var fileInfo in fileInfos)
            {
                var newFileName = fileInfo.Name;
                for (var i = 0; i < oldValues.Count; i++)
                {
                    if (!fileInfo.Name.Contains(oldValues[i])) continue;
                    newFileName = newFileName.Replace(oldValues[i], newValues[i]);
                }

                if (fileInfo.Name == newFileName) continue;
                base.Log(string.Format("Renaming file '{0}' to '{1}'", fileInfo.Name, newFileName));
                File.Move(fileInfo.FullName, Path.Combine(dirInfo.FullName, newFileName));
            }
        }
    }
}

