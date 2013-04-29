using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ServiceStack.Razor.Compilation
{
    internal static class DirectivesParser
    {
        private const string GlobalDirectivesFileName = "razorgenerator.directives";

        public static Dictionary<string, string> ParseDirectives(string baseDirectory, string fullPath)
        {
            var directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string directivesPath;
            if (TryFindGlobalDirectivesFile(baseDirectory, fullPath, out directivesPath))
            {
                ParseGlobalDirectives(directives, directivesPath);
            }
            ParseFileDirectives(directives, fullPath);

            return directives;
        }

        /// <summary>
        /// Attempts to locate the nearest global directive file by 
        /// </summary>
        private static bool TryFindGlobalDirectivesFile(string baseDirectory, string fullPath, out string path)
        {
            baseDirectory = baseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var directivesDirectory = Path.GetDirectoryName(fullPath).TrimEnd(Path.DirectorySeparatorChar);
            while (directivesDirectory != null && directivesDirectory.Length >= baseDirectory.Length)
            {
                path = Path.Combine(directivesDirectory, GlobalDirectivesFileName);
                if (File.Exists(path))
                {
                    return true;
                }
                directivesDirectory = Path.GetDirectoryName(directivesDirectory).TrimEnd(Path.DirectorySeparatorChar);
            }
            path = null;
            return false;
        }

        private static void ParseGlobalDirectives(Dictionary<string, string> directives, string directivesPath)
        {
            var fileContent = File.ReadAllText(directivesPath);
            ParseKeyValueDirectives(directives, fileContent);
        }

        private static void ParseFileDirectives(Dictionary<string, string> directives, string filePath)
        {
            var inputFileContent = File.ReadAllText(filePath);
            int index = inputFileContent.IndexOf("*@", StringComparison.OrdinalIgnoreCase);
            if (inputFileContent.TrimStart().StartsWith("@*") && index != -1)
            {
                string directivesLine = inputFileContent.Substring(0, index).TrimStart('*', '@');
                ParseKeyValueDirectives(directives, directivesLine);
            }
        }

        private static void ParseKeyValueDirectives(Dictionary<string, string> directives, string directivesLine)
        {
            // TODO: Make this better.
            var regex = new Regex(@"\b(?<Key>\w+)\s*:\s*(?<Value>[~\\\/\w\.]+)\b", RegexOptions.ExplicitCapture);
            foreach (Match item in regex.Matches(directivesLine))
            {
                var key = item.Groups["Key"].Value;
                var value = item.Groups["Value"].Value;

                directives.Add(key, value);
            }
        }
    }
}
