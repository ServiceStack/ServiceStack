// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.Collections.Generic;
using System.IO;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    public class TestProgress : IReturn<List<Movie>> { }
    public class TestProgressString : IReturn<string> { }
    public class TestProgressBytes : IReturn<byte[]> { }

    public class TestProgressBytesHttpResult : IReturn<byte[]> { }
    public class TestProgressBinaryFile : IReturn<byte[]> { }
    public class TestProgressTextFile : IReturn<string> { }

    public class DownloadProgressService : Service
    {
        public object Any(TestProgress request)
        {
            return ResetMoviesService.Top5Movies;
        }

        public string Any(TestProgressString request)
        {
            return ResetMoviesService.Top5Movies.ToJson();
        }

        public object Any(TestProgressBytes request)
        {
            return ResetMoviesService.Top5Movies.ToJson().ToUtf8Bytes();
        }

        public object Any(TestProgressBytesHttpResult request)
        {
            return new HttpResult(ResetMoviesService.Top5Movies.ToJson().ToUtf8Bytes(), "application/octet-stream");
        }

        public object Any(TestProgressBinaryFile request)
        {
            var path = Path.GetTempFileName();
            File.WriteAllBytes(path, ResetMoviesService.Top5Movies.ToJson().ToUtf8Bytes());
            return new HttpResult(new FileInfo(path), "application/octet-stream");
        }

        public object Any(TestProgressTextFile request)
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, ResetMoviesService.Top5Movies.ToJson());
            return new HttpResult(new FileInfo(path), "application/json");
        }
    }
}