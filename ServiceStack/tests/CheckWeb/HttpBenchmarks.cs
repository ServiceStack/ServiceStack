/* Options:
Date: 2017-11-09 21:58:38
Version: 4.50
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://httpbenchmarks.servicestack.net

//GlobalNamespace: 
//MakePartial: True
//MakeVirtual: True
//MakeInternal: False
//MakeDataContractsExtensible: False
//AddReturnMarker: True
//AddDescriptionAsComments: True
//AddDataContractAttributes: False
//AddIndexesToDataMembers: False
//AddGeneratedCodeAttributes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: True
//IncludeTypes: 
//ExcludeTypes: 
//AddNamespaces: 
//AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using BenchmarksAnalyzer.ServiceModel.Types;
using BenchmarksAnalyzer.ServiceModel;


namespace BenchmarksAnalyzer.ServiceModel
{

    [Route("/testplans/{TestPlanId}/testresults", "POST")]
    [Route("/testplans/{TestPlanId}/testruns/{TestRunId}/testresults", "POST")]
    public partial class AddTestResults
        : IReturn<List<TestResult>>
    {
        public virtual int TestPlanId { get; set; }
        public virtual int? TestRunId { get; set; }
        public virtual string Contents { get; set; }
    }

    [Route("/testplans", "POST")]
    public partial class CreateTestPlan
        : IReturn<TestPlan>
    {
        public virtual string Name { get; set; }
        public virtual string Slug { get; set; }
    }

    [Route("/testplans/{TestPlanId}/testruns", "POST")]
    public partial class CreateTestRun
        : IReturn<TestRun>
    {
        public virtual int TestPlanId { get; set; }
        public virtual string SeriesId { get; set; }
    }

    [Route("/testplans/{Id}/delete", "POST DELETE")]
    public partial class DeleteTestPlan
    {
        public virtual int Id { get; set; }
    }

    [Route("/testruns/{Id}/delete", "POST DELETE")]
    public partial class DeleteTestRun
    {
        public virtual int Id { get; set; }
    }

    [Route("/testplans/{Id}/edit")]
    [Route("/testplans/{Id}/testruns/{TestRunId}/edit")]
    public partial class EditTestPlan
        : IReturn<TestPlan>
    {
        public virtual int Id { get; set; }
        public virtual int? TestRunId { get; set; }
    }

    [Route("/testplans", "GET")]
    public partial class FindTestPlans
        : IReturn<List<TestPlan>>
    {
    }

    [Route("/testplans/{TestPlanId}/testruns", "GET")]
    public partial class FindTestRuns
        : IReturn<List<TestRun>>
    {
        public virtual int TestPlanId { get; set; }
    }

    [Route("/testplans/{Id}")]
    public partial class GetTestPlan
        : IReturn<TestPlan>
    {
        public virtual int Id { get; set; }
    }

    [Route("/myinfo")]
    public partial class MyInfo
        : IReturn<UserInfo>
    {
    }

    [Route("/ping")]
    public partial class Ping
        : IReturn<PingResponse>
    {
    }

    public partial class PingResponse
    {
        public virtual string Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/reset")]
    public partial class Reset
    {
    }

    [Route("/testplans/{TestPlanId}/results", "GET")]
    [Route("/testplans/{TestPlanId}/testruns/{TestRunId}/results", "GET")]
    public partial class SearchTestResults
        : IReturn<SearchTestResultsResponse>
    {
        public virtual int TestPlanId { get; set; }
        public virtual int? TestRunId { get; set; }
        public virtual int? Skip { get; set; }
        public virtual int? Take { get; set; }
        public virtual string Host { get; set; }
        public virtual int? Port { get; set; }
        public virtual string RequestPath { get; set; }
    }

    public partial class SearchTestResultsResponse
    {
        public SearchTestResultsResponse()
        {
            Results = new List<DisplayResult>{};
        }

        public virtual int TestPlanId { get; set; }
        public virtual int? TestRunId { get; set; }
        public virtual int? Skip { get; set; }
        public virtual int? Take { get; set; }
        public virtual string Host { get; set; }
        public virtual int? Port { get; set; }
        public virtual string RequestPath { get; set; }
        public virtual int Total { get; set; }
        public virtual List<DisplayResult> Results { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/testplans/{Id}/labels", "POST")]
    public partial class UpdateTestPlanLabels
        : IReturn<TestPlan>
    {
        public virtual int Id { get; set; }
        public virtual string ServerLabels { get; set; }
        public virtual string TestLabels { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/testplans/{TestPlanId}/upload", "POST")]
    [Route("/testplans/{TestPlanId}/testruns/{TestRunId}/upload", "POST")]
    public partial class UploadTestResults
        : IReturn<List<TestResult>>
    {
        public virtual int TestPlanId { get; set; }
        public virtual int? TestRunId { get; set; }
        public virtual bool CreateNewTestRuns { get; set; }
    }

    [Route("/{Slug}")]
    public partial class ViewTestPlan
        : IReturn<ViewTestPlanResponse>
    {
        public virtual string Slug { get; set; }
        public virtual int? Id { get; set; }
    }

    public partial class ViewTestPlanResponse
    {
        public ViewTestPlanResponse()
        {
            Results = new List<DisplayResult>{};
        }

        public virtual TestPlan TestPlan { get; set; }
        public virtual TestRun TestRun { get; set; }
        public virtual List<DisplayResult> Results { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }
}

namespace BenchmarksAnalyzer.ServiceModel.Types
{

    public partial class DisplayResult
    {
        public virtual int Id { get; set; }
        public virtual string Software { get; set; }
        public virtual string Host { get; set; }
        public virtual int Port { get; set; }
        public virtual string RequestPath { get; set; }
        public virtual int RequestLength { get; set; }
        public virtual int Concurrency { get; set; }
        public virtual double TimeTaken { get; set; }
        public virtual int TotalRequests { get; set; }
        public virtual int FailedRequests { get; set; }
        public virtual int TotalTransferred { get; set; }
        public virtual int HtmlTransferred { get; set; }
        public virtual double RequestsPerSec { get; set; }
        public virtual double TimePerRequest { get; set; }
        public virtual double TransferRate { get; set; }
    }

    public partial class TestPlan
    {
        public TestPlan()
        {
            ServerLabels = new Dictionary<string, string>{};
            TestLabels = new Dictionary<string, string>{};
        }

        public virtual int Id { get; set; }
        public virtual int UserAuthId { get; set; }
        public virtual string Name { get; set; }
        public virtual string Slug { get; set; }
        public virtual Dictionary<string, string> ServerLabels { get; set; }
        public virtual Dictionary<string, string> TestLabels { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }

    public partial class TestResult
    {
        public virtual int Id { get; set; }
        public virtual int UserAuthId { get; set; }
        public virtual int TestPlanId { get; set; }
        public virtual int TestRunId { get; set; }
        public virtual string Software { get; set; }
        public virtual string Hostname { get; set; }
        public virtual int Port { get; set; }
        public virtual string RequestPath { get; set; }
        public virtual int RequestLength { get; set; }
        public virtual int Concurrency { get; set; }
        public virtual double TimeTaken { get; set; }
        public virtual int TotalRequests { get; set; }
        public virtual int FailedRequests { get; set; }
        public virtual string FailedReasons { get; set; }
        public virtual int TotalTransferred { get; set; }
        public virtual int HtmlTransferred { get; set; }
        public virtual double RequestsPerSec { get; set; }
        public virtual double TimePerRequest { get; set; }
        public virtual double TransferRate { get; set; }
        public virtual string RawData { get; set; }
    }

    public partial class TestRun
    {
        public virtual int Id { get; set; }
        public virtual int UserAuthId { get; set; }
        public virtual int TestPlanId { get; set; }
        public virtual string SeriesId { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        [Ignore]
        public virtual int TestResultsCount { get; set; }
    }

    public partial class UserInfo
    {
        public virtual int Id { get; set; }
        public virtual int UserAuthId { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string ProfileUrl64 { get; set; }
    }
}


