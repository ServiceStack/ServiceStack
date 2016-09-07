using System;
using ServiceStack.Host;
using ServiceStack.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack
{
    public class CsvRequestLogger : InMemoryRollingRequestLogger
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CsvRequestLogger));

        readonly object semaphore = new object();
        private List<RequestLogEntry> logs = new List<RequestLogEntry>();
        private List<RequestLogEntry> errorLogs = new List<RequestLogEntry>();

        private readonly IVirtualFiles files;
        private readonly string requestLogsPattern;
        private readonly string errorLogsPattern;
        private readonly int appendEverySecs;
        private readonly Timer timer;

        public CsvRequestLogger(IVirtualFiles files = null, string requestLogsPattern = null, string errorLogsPattern = null, TimeSpan? appendEvery = null)
        {
            this.files = files ?? new FileSystemVirtualPathProvider(HostContext.AppHost, HostContext.Config.WebHostPhysicalPath);
            this.requestLogsPattern = requestLogsPattern ?? "requestlogs/{year}-{month}/{year}-{month}-{day}.csv";
            this.errorLogsPattern = errorLogsPattern ?? "requestlogs/{year}-{month}/{year}-{month}-{day}-errors.csv";
            this.appendEverySecs = (int)appendEvery.GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalSeconds;

            var lastEntry = ReadLastEntry(GetLogFilePath(this.requestLogsPattern, DateTime.UtcNow));
            if (lastEntry != null)
                requestId = lastEntry.Id;

            timer = new Timer(OnFlush, null, this.appendEverySecs, Timeout.Infinite);
        }

        private RequestLogEntry ReadLastEntry(string logFile)
        {
            try
            {
                if (this.files.FileExists(logFile))
                {
                    var file = this.files.GetFile(logFile);
                    using (var reader = file.OpenText())
                    {
                        string first = null, last = null, line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (first == null)
                                first = line;

                            last = line;
                        }
                        if (last != null)
                        {
                            var entry = (first + "\n" + last).FromCsv<RequestLogEntry>();
                            if (entry.Id > 0)
                                return entry;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Could not read last entry from '{log}'", ex);
            }
            return null;
        }

        protected virtual void OnFlush(object state)
        {
            if (logs.Count + errorLogs.Count > 0)
            {
                List<RequestLogEntry> logsSnapshot = null;
                List<RequestLogEntry> errorLogsSnapshot = null;

                lock (semaphore)
                {
                    if (logs.Count > 0)
                    {
                        logsSnapshot = this.logs;
                        this.logs = new List<RequestLogEntry>();
                    }
                    if (errorLogs.Count > 0)
                    {
                        errorLogsSnapshot = this.errorLogs;
                        this.errorLogs = new List<RequestLogEntry>();
                    }
                }

                var now = DateTime.UtcNow;
                if (logsSnapshot != null)
                {
                    var logFile = GetLogFilePath(requestLogsPattern, now);
                    WriteLogs(logsSnapshot, logFile);
                }
                if (errorLogsSnapshot != null)
                {
                    var logFile = GetLogFilePath(errorLogsPattern, now);
                    WriteLogs(errorLogsSnapshot, logFile);
                }
            }
            timer.Change(appendEverySecs, Timeout.Infinite);
        }

        public string GetLogFilePath(string logFilePattern, DateTime forDate)
        {
            var year = forDate.Year.ToString("0000");
            var month = forDate.Month.ToString("00");
            var day = forDate.Day.ToString("00");
            return logFilePattern.Replace("{year}", year).Replace("{month}", month).Replace("{day}", day);
        }

        public virtual void WriteLogs(List<RequestLogEntry> logs, string logFile)
        {
            try
            {
                var csv = logs.ToCsv();
                if (!files.FileExists(logFile))
                {
                    files.WriteFile(logFile, csv);
                }
                else
                {
                    var csvRows = csv.Substring(csv.IndexOf('\n') + 1);
                    files.AppendFile(logFile, csvRows);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public override void Log(IRequest request, object requestDto, object response, TimeSpan requestDuration)
        {
            var requestType = requestDto?.GetType();

            if (ExcludeRequestType(requestType))
                return;

            var entry = CreateEntry(request, requestDto, response, requestDuration, requestType);
            lock (semaphore)
            {
                logs.Add(entry);
                if (response.IsErrorResponse())
                {
                    errorLogs.Add(entry);
                }
            }
        }

        public override List<RequestLogEntry> GetLatestLogs(int? take)
        {
            var logFile = files.GetFile(GetLogFilePath(this.requestLogsPattern, DateTime.UtcNow));
            if (logFile.Exists())
            {
                using (var reader = logFile.OpenText())
                {
                    var results = CsvSerializer.DeserializeFromReader<List<RequestLogEntry>>(reader);
                    return take.HasValue
                        ? results.Take(take.Value).ToList()
                        : results;
                }
            }

            return base.GetLatestLogs(take);
        }
    }
}