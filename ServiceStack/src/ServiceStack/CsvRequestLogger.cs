using System;
using ServiceStack.Host;
using ServiceStack.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack;

public class CsvRequestLogger : InMemoryRollingRequestLogger, IDisposable
{
    private static readonly ILog log = LogManager.GetLogger(typeof(CsvRequestLogger));

    readonly object semaphore = new();
    private List<RequestLogEntry> logs = new();
    private List<RequestLogEntry> errorLogs = new();

    private readonly IVirtualFiles files;
    private readonly string requestLogsPattern;
    private readonly string errorLogsPattern;
    private readonly TimeSpan appendEverySecs;
    private readonly Timer timer;
        
    public Action<List<RequestLogEntry>, Exception> OnWriteLogsError { get; set; }
    public Action<string, Exception> OnReadLastEntryError { get; set; }

    public CsvRequestLogger(IVirtualFiles files = null, string requestLogsPattern = null, string errorLogsPattern = null, TimeSpan? appendEvery = null)
    {
        this.files = files ?? new FileSystemVirtualFiles(HostContext.Config?.WebHostPhysicalPath ?? ".");
        this.requestLogsPattern = requestLogsPattern ?? "requestlogs/{year}-{month}/{year}-{month}-{day}.csv";
        this.errorLogsPattern = errorLogsPattern ?? "requestlogs/{year}-{month}/{year}-{month}-{day}-errors.csv";
        this.appendEverySecs = appendEvery ?? TimeSpan.FromSeconds(1);

        var lastEntry = ReadLastEntry(GetLogFilePath(this.requestLogsPattern, CurrentDateFn()));
        if (lastEntry != null)
            requestId = lastEntry.Id;

        timer = new Timer(OnFlush, null, this.appendEverySecs, Timeout.InfiniteTimeSpan);
    }

    private RequestLogEntry ReadLastEntry(string logFile)
    {
        try
        {
            if (this.files != null && this.files.FileExists(logFile))
            {
                var file = this.files.GetFile(logFile);
                if (file != null)
                {
                    using var reader = file.OpenText();
                    string first = null, last = null;
                    while (reader.ReadLine() is { } line)
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
            OnReadLastEntryError?.Invoke(logFile, ex);
            log.Error($"Could not read last entry from '{logFile}'", ex);
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

            var now = CurrentDateFn();
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
        try
        {
            timer?.Change(appendEverySecs, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException) {}
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
        if (logs == null || logs.Count == 0 || files == null)
            return;

        try
        {
            var csv = logs.ToCsv();
            if (string.IsNullOrEmpty(csv))
                return;

            if (!files.FileExists(logFile))
            {
                files.WriteFile(logFile, csv);
            }
            else
            {
                var idx = csv.IndexOf('\n');
                var csvRows = idx >= 0 ? csv.Substring(idx + 1) : "";
                if (!string.IsNullOrEmpty(csvRows))
                    files.AppendFile(logFile, csvRows);
            }
        }
        catch (Exception ex)
        {
            OnWriteLogsError?.Invoke(logs, ex);
            log.Error(ex);
        }
    }

    public override void Log(IRequest request, object requestDto, object response, TimeSpan requestDuration)
    {
        if (request == null)
            return;

        if (ShouldSkip(request, requestDto))
            return;
        
        if (response is Task)
        {
            LogManager.GetLogger(GetType()).WarnFormat("Ignoring nested Task response returned from '{0}' API", requestDto?.GetType().Name ?? "null");
            return;
        }

        var requestType = requestDto?.GetType();

        var entry = CreateEntry(request, requestDto, response, requestDuration, requestType);

        RequestLogFilter?.Invoke(request, entry);

        lock (semaphore)
        {
            logs.Add(entry);
            if (entry.ErrorResponse != null)
            {
                errorLogs.Add(entry);
            }
        }
    }

    public override List<RequestLogEntry> GetLatestLogs(int? take)
    {
        if (files == null)
            return base.GetLatestLogs(take);

        var logFile = files.GetFile(GetLogFilePath(this.requestLogsPattern, CurrentDateFn()));
        if (logFile == null || !logFile.Exists()) 
            return base.GetLatestLogs(take);
            
        using var reader = logFile.OpenText();
        var results = CsvSerializer.DeserializeFromReader<List<RequestLogEntry>>(reader) ?? [];
        return take.HasValue
            ? results.Take(Math.Max(0, take.Value)).ToList()
            : results;
    }

    public void Flush() => OnFlush(null);

    public void Dispose()
    {
        timer?.Dispose();
        Flush();
    }
}