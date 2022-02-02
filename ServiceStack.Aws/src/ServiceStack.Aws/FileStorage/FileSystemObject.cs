using System;
using System.Globalization;
using System.IO;

namespace ServiceStack.Aws.FileStorage
{
    public class FileSystemObject : IEquatable<FileSystemObject>
    {
        public FileSystemObject Clone()
        {
            var cloned = (FileSystemObject)MemberwiseClone();
            return cloned;
        }

        private readonly string directorySeparatorCharacter;

        public FileSystemObject() { }

        public FileSystemObject(string path, string fileName) : this(Path.Combine(path ?? string.Empty, fileName)) { }

        public FileSystemObject(string filePathAndName)
        {   // Figure out if there are mixed directory markers and adjust to the appropriate one across the board - If we have 
            // both path separators, use only the first. If we have only one, use that
            var slashPartIndex = filePathAndName.IndexOf("/", StringComparison.OrdinalIgnoreCase);
            var backslashPartIndex = filePathAndName.IndexOf("\\", StringComparison.OrdinalIgnoreCase);

            var useBackslashDirectorySeparator = (slashPartIndex >= 0 && backslashPartIndex >= 0)
                ? backslashPartIndex < slashPartIndex
                : backslashPartIndex >= 0;

            // Set the character separator to use
            directorySeparatorCharacter = useBackslashDirectorySeparator
                ? "\\"
                : "/";

            Init(filePathAndName);
        }

        public FileSystemObject(string filePathAndName, char dirSeparatorCharacter)
        {
            directorySeparatorCharacter = dirSeparatorCharacter.ToString();
            Init(filePathAndName);
        }

        private void Init(string filePathAndName)
        {
            if (filePathAndName.EndsWith("\\") || filePathAndName.EndsWith("/"))
            {   // Ends with a path marker - check to see if this is actually a FILE ending with a terminator - if so, remove it
                var isFileTestPath = filePathAndName.TrimEnd(new[] { '\\', '/' });

                var isFileTestExtension = Path.GetExtension(isFileTestPath);

                if (!String.IsNullOrEmpty(isFileTestExtension) &&
                    isFileTestPath.EndsWith(isFileTestExtension, StringComparison.OrdinalIgnoreCase))
                {
                    filePathAndName = isFileTestPath;
                }
            }

            Func<string, string> pathScrubber = (f) => f.Replace(
                directorySeparatorCharacter.Equals("\\", StringComparison.Ordinal)
                    ? "/"
                    : "\\",
                directorySeparatorCharacter.Equals("\\", StringComparison.Ordinal)
                    ? "\\"
                    : "/");
            var normalizedName = Path.DirectorySeparatorChar == '/' ? filePathAndName.Replace('\\', '/') : filePathAndName;

            FileName = pathScrubber(Path.GetFileNameWithoutExtension(normalizedName));
            FolderName = pathScrubber(Path.GetDirectoryName(normalizedName));

            var fileExtension = pathScrubber(Path.GetExtension(normalizedName));

            FileExtension = fileExtension.StartsWith(".", StringComparison.OrdinalIgnoreCase)
                ? fileExtension.Substring(1)
                : fileExtension;
        }

        public string FileName { get; private set; }
        public string FolderName { get; private set; }
        public string FileExtension { get; private set; }

        private string Combine(params string[] paths)
        {
            var appendSeparator = false;
            var returnVal = string.Empty;

            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                returnVal = string.Concat(returnVal,
                    appendSeparator
                        ? directorySeparatorCharacter
                        : string.Empty,
                    path);

                appendSeparator = !path.EndsWith(directorySeparatorCharacter, StringComparison.OrdinalIgnoreCase);
            }

            return returnVal;
        }

        public string FullName => Combine(string.IsNullOrEmpty(FolderName) ? string.Empty : FolderName, FileNameAndExtension);

        public string FileNameAndExtension => string.IsNullOrEmpty(FileExtension)
            ? FileName
            : string.Concat(FileName, ".", FileExtension);

        public bool Equals(FileSystemObject other)
        {   // Purpsely always use case-insensitive comparison for our purposes, seems safest to me
            return other != null &&
                   FullName.Equals(other.FullName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            var fsoObject = obj as FileSystemObject;

            return fsoObject != null && Equals(fsoObject);
        }

        public override string ToString()
        {
            return FullName;
        }
        
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}