using System;
using System.Data;
using System.Diagnostics;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

internal static class ApiUtilExtensions
{
    public static string PrintLastSql(this IDbConnection dbConn)
    {
        dbConn.GetLastSql().Print();
        "".Print();
        return dbConn.GetLastSql();
    }

    //[Explicit]
    //public void Benchmark()
    //{
    //    Measure(() => db.SingleById<Person>(1), times: 1).ToString().Print("Test 2: {0}");
    //    Measure(() => db.SingleById<Person>(1), times: 1).ToString().Print("Test 1: {0}");
    //}

    static double MeasureFor(Action fn, int timeMinimum)
    {
        int iter = 0;
        Stopwatch watch = new Stopwatch();
        watch.Start();
        long elapsed = 0;
        while (elapsed < timeMinimum)
        {
            fn();
            elapsed = watch.ElapsedMilliseconds;
            iter++;
        }
        return 1000.0 * elapsed / iter;
    }

    static double Measure(Action fn, int times = 10, int runfor = 2000, Action setup = null, Action warmup = null, Action teardown = null)
    {
        setup?.Invoke();

        // Warmup for at least 100ms. Discard result.
        if (warmup == null)
            warmup = fn;

        MeasureFor(() => { warmup(); }, 100);

        // Run the benchmark for at least 2000ms.
        double result = MeasureFor(() =>
        {
            for (var i = 0; i < times; i++)
                fn();
        }, runfor);

        teardown?.Invoke();

        return result;
    }
}