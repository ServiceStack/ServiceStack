using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ServiceStack.IO;

namespace ServiceStack
{
    public class ServerHtmlFeature : IPlugin
    {
        public string PageExtension { get; set; } = ".htm";

        static readonly ConcurrentDictionary<string, byte> catchAllPathsNotFound = new ConcurrentDictionary<string, byte>();

        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) =>
            {
                if (catchAllPathsNotFound.ContainsKey(pathInfo))
                    return null;

                

                return null;
            });
        }
    }

    public class ServerHtmlFile
    {
        private IVirtualFile file;

        public ServerHtmlFile(IVirtualFile file)
        {
            this.file = file;
        }
    }

    public class ServerHtmlHandler
    {
        
    }

    public static class ServerHtmlUtils
    {
        static char[] VarDelimiters = { '|', '}' };

        public static List<ServerHtmlFragment> ParseServerHtml(string html)
        {
            var to = new List<ServerHtmlFragment>();

            if (string.IsNullOrEmpty(html))
                return to;

            int pos;
            var lastPos = 0;
            while ((pos = html.IndexOf("{{", lastPos, StringComparison.Ordinal)) != -1)
            {
                var block = html.Substring(lastPos, pos - lastPos);
                to.Add(new ServerHtmlStringFragment(block));

                var varStartPos = pos + 2;
                var varEndPos = html.IndexOfAny(VarDelimiters, varStartPos);
                var varName = html.Substring(varStartPos, varEndPos - varStartPos).Trim();
                if (varEndPos == -1)
                    throw new ArgumentException($"Invalid Server HTML Template at '{html.SafeSubstring(50)}...'", nameof(html));

                List<Command> filterCommands = null;
                
                var isFilter = html[varEndPos] == '|';
                if (isFilter)
                {
                    filterCommands = html.Substring(varEndPos + 1).ParseCommands(
                        separator: '|',
                        atEndIndex: (str, strPos) =>
                        {
                            while (str.Length > strPos && char.IsWhiteSpace(str[strPos]))
                                strPos++;

                            if (str.Length > strPos + 1 && str[strPos] == '}' && str[strPos + 1] == '}')
                            {
                                varEndPos = varEndPos + 1 + strPos + 1;
                                return strPos;
                            }
                            return null;
                        });
                }
                else
                {
                    varEndPos += 1;
                }

                to.Add(new ServerHtmlVariableFragment(varName, filterCommands));

                lastPos = varEndPos + 1;
            }

            if (lastPos != html.Length - 1)
            {
                var lastBlock = lastPos == 0 ? html : html.Substring(lastPos);
                to.Add(new ServerHtmlStringFragment(lastBlock));
            }

            return to;
        }
    }
}