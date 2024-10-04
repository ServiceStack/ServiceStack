using BenchmarkDotNet.Running;
using ServiceStack.OrmLite.Tests.Benchmarks;

BenchmarkRunner.Run<SqlServerAsync>();
