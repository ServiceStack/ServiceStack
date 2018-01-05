/* Options:
Date: 2017-11-08 03:06:38
Version: 4.50
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://stackapis.servicestack.net

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
using StackApis.ServiceModel.Types;
using StackApis.ServiceModel;


namespace StackApis.ServiceModel
{

    ///<summary>
    ///Get a list of Answers for a Question
    ///</summary>
    [Route("/answers/{QuestionId}")]
    public partial class GetAnswers
        : IReturn<GetAnswersResponse>
    {
        public virtual int QuestionId { get; set; }
    }

    public partial class GetAnswersResponse
    {
        public virtual Answer Ansnwer { get; set; }
    }

    [Route("/questions/search")]
    public partial class SearchQuestions
        : IReturn<SearchQuestionsResponse>
    {
        public SearchQuestions()
        {
            Tags = new List<string>{};
        }

        public virtual List<string> Tags { get; set; }
        public virtual string UserId { get; set; }
    }

    public partial class SearchQuestionsResponse
    {
        public SearchQuestionsResponse()
        {
            Results = new List<Question>{};
        }

        public virtual List<Question> Results { get; set; }
    }

    [Route("/questions")]
    [AutoQueryViewer(Title="Explore StackOverflow Questions", Description="Find ServiceStack Questions on StackOverflow", IconUrl="material-icons:cast", DefaultSearchField="Title", DefaultSearchType="Contains", DefaultSearchText="ServiceStack")]
    public partial class StackOverflowQuery
        : QueryDb<Question>, IReturn<QueryResponse<Question>>
    {
        public virtual int? ScoreGreaterThan { get; set; }
    }
}

namespace StackApis.ServiceModel.Types
{

    public partial class Answer
    {
        public virtual int AnswerId { get; set; }
        public virtual User Owner { get; set; }
        public virtual bool IsAccepted { get; set; }
        public virtual int Score { get; set; }
        public virtual int LastActivityDate { get; set; }
        public virtual int LastEditDate { get; set; }
        public virtual int CreationDate { get; set; }
        public virtual int QuestionId { get; set; }
    }

    public partial class Question
    {
        public Question()
        {
            Tags = new string[]{};
        }

        public virtual int QuestionId { get; set; }
        public virtual string Title { get; set; }
        public virtual int Score { get; set; }
        public virtual int ViewCount { get; set; }
        public virtual bool IsAnswered { get; set; }
        public virtual int AnswerCount { get; set; }
        public virtual string Link { get; set; }
        public virtual string[] Tags { get; set; }
        public virtual User Owner { get; set; }
        public virtual int LastActivityDate { get; set; }
        public virtual int CreationDate { get; set; }
        public virtual int LastEditDate { get; set; }
        public virtual int? AcceptedAnswerId { get; set; }
    }

    public partial class User
    {
        public virtual int Reputation { get; set; }
        public virtual int Userid { get; set; }
        public virtual string UserType { get; set; }
        public virtual int AcceptRate { get; set; }
        public virtual string ProfileImage { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string Link { get; set; }
    }
}

