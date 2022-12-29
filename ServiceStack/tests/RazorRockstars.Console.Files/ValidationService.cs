// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.FluentValidation;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    [Route("/validation")]
    public class Validation
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Route("/AutoValidation")]
    public class AutoValidation
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Route("/ManualValidation")]
    public class ManualValidation
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [DefaultView("Validation")]
    public class ValidationService : Service
    {
        public object Get(Validation request)
        {
            return request;
        }

        public object Post(AutoValidation request)
        {
            return request.ConvertTo<Validation>();
        }

        public object Post(ManualValidation request)
        {
            if (request.Name == null)
                throw new ArgumentNullException("Name");

            if (request.Id < 0)
                throw new ArgumentException("Id must be a positive number", "Id");

            return request.ConvertTo<Validation>();
        }
    }

    public class AutoValidationValidator : AbstractValidator<AutoValidation>
    {
        public AutoValidationValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Id).GreaterThanOrEqualTo(0);
        }
    }


    [TestFixture]
    public class ValidationTests
    {
        public const string ListeningOn = "http://*:1337/";
        public const string Host = "http://localhost:1337";

        //private const string ListeningOn = "http://*:1337/subdir/subdir2/";
        //private const string Host = "http://localhost:1337/subdir/subdir2";

        private const string BaseUri = Host + "/";

        AppHost appHost;

        Stopwatch startedAt;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            startedAt = Stopwatch.StartNew();
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            "Time Taken {0}ms".Fmt(startedAt.ElapsedMilliseconds).Print();
            appHost.Dispose();
        }

        [Ignore("Debug Run")]
        [Test]
        public void RunFor10Mins()
        {
            Process.Start(BaseUri.CombineWith("/validation"));
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }
        
    }
}