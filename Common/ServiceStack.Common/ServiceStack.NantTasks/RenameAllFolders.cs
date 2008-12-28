using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NAnt.Core;
using NAnt.Core.Util;
using NAnt.Core.Attributes;
using System.Text.RegularExpressions;

namespace ServiceStack.NantTasks
{
    [TaskName("renameAllFolders")]
    public class RenameAllFolders : ReplaceNantTask
    {
        protected override void OnEachDirectory(DirectoryInfo dirInfo,
            IList<string> oldValues, IList<string> newValues)
        {
            if (dirInfo.Parent == null) return;

            var newDirName = dirInfo.Name;
            for (var i = 0; i < oldValues.Count; i++)
            {
                if (!dirInfo.Name.Contains(oldValues[i])) continue;
                newDirName = newDirName.Replace(oldValues[i], newValues[i]);
            }

            if (dirInfo.Name == newDirName) return;
            base.Log(string.Format("Renaming directory '{0}' to '{1}'", dirInfo.Name, newDirName));
            Directory.Move(dirInfo.FullName, Path.Combine(dirInfo.Parent.FullName, newDirName));
        }
    }
}

