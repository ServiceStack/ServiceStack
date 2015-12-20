using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using RazorRockstars.Web.Tests;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.MsgPack;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace RazorRockstars.Web
{
    /// New Proposal, keeping ServiceStack's message-based semantics:
    /// Inspired by Ivan's proposal: http://korneliuk.blogspot.com/2012/08/servicestack-reusing-dtos.html
    /// 
    /// To align with ServiceStack's message-based design, an "Action": 
    ///   - is public and only supports a single argument the typed Request DTO 
    ///   - method name matches a HTTP Method or "Any" which is used as a fallback (for all methods) if it exists
    ///   - only returns object or void
    ///
    /// Notes: 
    /// Content Negotiation built-in, i.e. by default each method/route is automatically available in every registered Content-Type (HTTP Only).
    /// New API are also available in ServiceStack's typed service clients (they're actually even more succinct :)
    /// Any Views rendered is based on Request or Returned DTO type, see: http://razor.servicestack.net/#unified-stack

    public class ReqstarsService : Service
    {
        [EnableCors]
        public void Options(Reqstar reqstar) { }

        public void Any(ResetReqstar request)
        {
            Db.DeleteAll<Reqstar>();
            Db.Insert(SeedData.Reqstars);
        }

        public object Get(SearchReqstars request)
        {
            if (request.Age.HasValue && request.Age <= 0)
                throw new ArgumentException("Invalid Age");

            return new ReqstarsResponse //matches ReqstarsResponse.cshtml razor view
            {
                Aged = request.Age,
                Total = Db.Scalar<int>("select count(*) from Reqstar"),
                Results = request.Age.HasValue
                    ? Db.Select<Reqstar>(q => q.Age == request.Age.Value)
                    : Db.Select<Reqstar>()
            };
        }

        public object Any(AllReqstars request)
        {
            return Db.Select<Reqstar>();
        }

        [ClientCanSwapTemplates] //allow action-level filters
        public object Get(GetReqstar request)
        {
            return Db.SingleById<Reqstar>(request.Id);
        }

        public object Post(Reqstar request)
        {
            if (!request.Age.HasValue)
                throw new ArgumentException("Age is required");

            Db.Insert(request.ConvertTo<Reqstar>());
            return Db.Select<Reqstar>();
        }

        public object Patch(UpdateReqstar request)
        {
            Db.Update<Reqstar>(request, x => x.Id == request.Id);
            return Db.SingleById<Reqstar>(request.Id);
        }

        public void Any(DeleteReqstar request)
        {
            Db.DeleteById<Reqstar>(request.Id);
        }

        public object Any(RoutelessReqstar request)
        {
            return request;
        }

        public object Any(Throw request)
        {
            if (request.StatusCode.HasValue)
            {
                throw new HttpError(
                    request.StatusCode.Value,
                    typeof(NotImplementedException).Name,
                    request.Message);
            }
            throw new NotImplementedException(request.Message + " is not implemented");
        }

        public ViewStateResponse Get(ViewState request)
        {
            return new ViewStateResponse();
        }

        public Annotated Any(Annotated request)
        {
            return request;
        }

        public void Any(Headers request)
        {
            base.Response.AddHeader("X-Response", request.Text);
        }

        public string Any(Strings request)
        {
            return "Hello, " + (request.Text ?? "World!");
        }

        public byte[] Any(Bytes request)
        {
            return Guid.Parse(request.Text).ToByteArray();
        }

        public byte[] Any(Streams request)
        {
            return Guid.Parse(request.Text).ToByteArray();
        }
    }



}