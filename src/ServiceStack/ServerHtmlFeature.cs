using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;

#if NETSTANDARD1_6
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack
{
    public class ServerHtmlFeature : IPlugin
    {
        public string PageExtension { get; set; } = ".html";

        static readonly ConcurrentDictionary<string, byte> catchAllPathsNotFound = new ConcurrentDictionary<string, byte>();

        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) =>
            {
                if (catchAllPathsNotFound.ContainsKey(pathInfo))
                    return null;



                if (catchAllPathsNotFound.Count > 10000) //prevent DDOS
                    catchAllPathsNotFound.Clear();

                catchAllPathsNotFound[pathInfo] = 1;
                return null;
            });
        }
    }

    public class ServerHtmlHandler
    {
        
    }

    public class ServerHtmlPage
    {
        public IVirtualFile File { get; set; }
        public StringSegment ServerHtml { get; set; }
        public StringSegment PageHtml { get; set; }
        public Dictionary<string, string> PageVars { get; set; }
        public List<ServerHtmlFragment> PageFragments { get; set; }
        public DateTime LastModified { get; set; }

        public ServerHtmlPage(IVirtualFile file)
        {
            File = file;
        }

        public async Task<ServerHtmlPage> Load()
        {
            LastModified = File.LastModified;

            using (var stream = File.OpenRead())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                ServerHtml = (await reader.ReadToEndAsync()).ToStringSegment();

                bool inBlockComments = false;
                PageVars = new Dictionary<string, string>();

                int pos = 0;
                while (char.IsWhiteSpace(ServerHtml.GetChar(pos)))
                    pos++;

                ServerHtml.TryReadLine(out StringSegment line, ref pos);
                if (line.StartsWith("<!--"))
                {
                    while (ServerHtml.TryReadLine(out line, ref pos))
                    {
                        if (line.Trim().Length == 0)
                            continue;


                        if (line.StartsWith("-->"))
                            break;

                        var kvp = line.SplitOnFirst(':');
                        PageVars[kvp[0].ToString()] = kvp.Length > 1 ? kvp[1].ToString().Trim() : "";
                    }
                }
                else
                {
                    pos = 0;
                }

                PageHtml = ServerHtml.Subsegment(pos);
                PageFragments = ServerHtmlUtils.ParseServerHtml(PageHtml);
            }

            return this;
        }
    }

    public static class ServerHtmlUtils
    {
        static char[] VarDelimiters = { '|', '}' };

        public static List<ServerHtmlFragment> ParseServerHtml(string htmlString)
        {
            return ParseServerHtml(new StringSegment(htmlString));
        }

        public static List<ServerHtmlFragment> ParseServerHtml(StringSegment html)
        {
            var to = new List<ServerHtmlFragment>();

            if (html.IsNullOrWhiteSpace())
                return to;
            
            int pos;
            var lastPos = 0;
            while ((pos = html.IndexOf("{{", lastPos)) != -1)
            {
                var block = html.Subsegment(lastPos, pos - lastPos);
                to.Add(new ServerHtmlStringFragment(block));

                var varStartPos = pos + 2;
                var varEndPos = html.IndexOfAny(VarDelimiters, varStartPos);
                var varName = html.Subsegment(varStartPos, varEndPos - varStartPos).Trim();
                if (varEndPos == -1)
                    throw new ArgumentException($"Invalid Server HTML Template at '{html.SafeSubsegment(50)}...'", nameof(html));

                List<Command> filterCommands = null;
                
                var isFilter = html.GetChar(varEndPos) == '|';
                if (isFilter)
                {
                    filterCommands = html.Subsegment(varEndPos + 1).ParseCommands(
                        separator: '|',
                        atEndIndex: (str, strPos) =>
                        {
                            while (str.Length > strPos && char.IsWhiteSpace(str.GetChar(strPos)))
                                strPos++;

                            if (str.Length > strPos + 1 && str.GetChar(strPos) == '}' && str.GetChar(strPos + 1) == '}')
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

                lastPos = varEndPos + 1;
                var originalText = html.Subsegment(pos, lastPos - pos);

                to.Add(new ServerHtmlVariableFragment(originalText, varName, filterCommands));
            }

            if (lastPos != html.Length - 1)
            {
                var lastBlock = lastPos == 0 ? html : html.Subsegment(lastPos);
                to.Add(new ServerHtmlStringFragment(lastBlock));
            }

            return to;
        }
    }
}