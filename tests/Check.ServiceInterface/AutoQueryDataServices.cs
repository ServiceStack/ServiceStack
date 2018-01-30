using System;
using System.Collections.Generic;
using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/query/requestlogs")]
    [Route("/query/requestlogs/{Date}")]
    public class QueryRequestLogs : QueryData<RequestLogEntry>
    {
        public DateTime? Date { get; set; }
        public bool ViewErrors { get; set; }
    }

    [AutoQueryViewer(Name = "Today's Logs", Title = "Logs from Today")]
    public class TodayLogs : QueryData<RequestLogEntry> { }
    public class TodayErrorLogs : QueryData<RequestLogEntry> { }

    public class YesterdayLogs : QueryData<RequestLogEntry> { }
    public class YesterdayErrorLogs : QueryData<RequestLogEntry> { }

    public class AutoQueryDataServices : Service
    {
        public IAutoQueryData AutoQuery { get; set; }

        public object Any(QueryRequestLogs query)
        {
            var date = query.Date.GetValueOrDefault(DateTime.UtcNow);
            var logSuffix = query.ViewErrors ? "-errors" : "";
            var csvLogsFile = VirtualFileSources.GetFile("requestlogs/{0}-{1}/{0}-{1}-{2}{3}.csv".Fmt(
                date.Year.ToString("0000"),
                date.Month.ToString("00"),
                date.Day.ToString("00"),
                logSuffix));

            if (csvLogsFile == null)
                throw HttpError.NotFound("No logs found on " + date.ToShortDateString());

            var logs = csvLogsFile.ReadAllText().FromCsv<List<RequestLogEntry>>();

            var q = AutoQuery.CreateQuery(query, Request,
                db: new MemoryDataSource<RequestLogEntry>(logs, query, Request));

            return AutoQuery.Execute(query, q);
        }

        public object Any(TodayLogs request)
        {
            return Any(new QueryRequestLogs { Date = DateTime.UtcNow });
        }
        public object Any(TodayErrorLogs request)
        {
            return Any(new QueryRequestLogs { Date = DateTime.UtcNow, ViewErrors = true });
        }

        public object Any(YesterdayLogs request)
        {
            return Any(new QueryRequestLogs { Date = DateTime.UtcNow.AddDays(-1) });
        }
        public object Any(YesterdayErrorLogs request)
        {
            return Any(new QueryRequestLogs { Date = DateTime.UtcNow.AddDays(-1), ViewErrors = true });
        }
    }
}