// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.Collections.Generic;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    public class TestProgress : IReturn<List<Movie>> { }

    public class DownloadProgressService : Service
    {
        public List<Movie> Any(TestProgress request)
        {
            return ResetMoviesService.Top5Movies;
        }
    }
}