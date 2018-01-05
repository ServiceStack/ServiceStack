﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using Check.ServiceModel.Operations;
using Check.ServiceModel.Types;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class NativeTypesTestService : Service
    {
        public class HelloInService : IReturn<HelloResponse>
        {
            public string Name { get; set; }
        }

        public object Any(HelloACodeGenTest request)
        {
            return new HelloACodeGenTestResponse { FirstResult = request.FirstField };
        }

        public object Any(HelloInService request)
        {
            return new HelloResponse { Result = request.Name };
        }

        public object Any(Hello request)
        {
            return new HelloResponse { Result = request.Name };
        }

        public object Any(HelloAnnotated request)
        {
            return new HelloAnnotatedResponse { Result = request.Name };
        }

        public object Any(HelloWithNestedClass request)
        {
            return new HelloResponse { Result = request.Name };
        }

        public object Any(HelloList request)
        {
            return request.Names.Map(name => new ListResult { Result = name });
        }

        public object Any(HelloReturnList request)
        {
            return request.Names.Map(name => new OnlyInReturnListArg { Result = name });
        }

        public object Any(HelloArray request)
        {
            return request.Names.Map(name => new ArrayResult { Result = name });
        }

        public object Any(HelloExisting request)
        {
            return new HelloExistingResponse
            {
                ArrayResults = request.Names.Map(x => new ArrayResult { Result = x }).ToArray(),
                ListResults = request.Names.Map(x => new ListResult { Result = x }),
            };
        }

        public object Any(HelloWithEnum request)
        {
            return request;
        }

        public object Any(HelloExternal request)
        {
            return request;
        }

        public object Any(RestrictedAttributes request)
        {
            return request;
        }

        public object Any(AllowedAttributes request)
        {
            return request;
        }

        public object Any(HelloMultiline request)
        {
            return request;
        }

        public object Any(HelloAllTypes request)
        {
            return new HelloAllTypesResponse
            {
                AllTypes = request.AllTypes,
                AllCollectionTypes = request.AllCollectionTypes, 
                Result = request.Name
            };
        }

        public object Any(AllTypes request)
        {
            return request;
        }

        public object Any(HelloString request)
        {
            return request.Name;
        }

        public void Any(HelloVoid request)
        {
        }

        public object Any(HelloWithDataContract request)
        {
            return new HelloWithDataContractResponse { Result = request.Name };
        }

        public object Any(HelloWithDescription request)
        {
            return new HelloWithDescriptionResponse { Result = request.Name };
        }

        public object Any(HelloWithInheritance request)
        {
            return new HelloWithInheritanceResponse { Result = request.Name };
        }

        public object Any(HelloWithGenericInheritance request)
        {
            return request;
        }

        public object Any(HelloWithGenericInheritance2 request)
        {
            return request;
        }

        public object Any(HelloWithNestedInheritance request)
        {
            return request;
        }

        public object Any(HelloWithListInheritance request)
        {
            return request;
        }

        public object Any(HelloWithReturn request)
        {
            return new HelloWithAlternateReturnResponse { Result = request.Name };
        }

        public object Any(HelloWithRoute request)
        {
            return new HelloWithRouteResponse { Result = request.Name };
        }

        public object Any(HelloWithType request)
        {
            return new HelloWithTypeResponse
            {
                Result = new HelloType { Result = request.Name }
            };
        }

        public object Any(HelloStruct request)
        {
            return request;
        }

        public object Any(HelloSession request)
        {
            return new HelloSessionResponse
            {
                Result = base.SessionAs<AuthUserSession>()
            };
        }

        public object Any(HelloInterface request)
        {
            return request;
        }

        public object Any(HelloImplementsInterface request) => request;
    
        public object Get(Request1 request)
        {
            return new Request1Response();
        }

        public object Get(Request2 request)
        {
            return new Request2Response();
        }

        public object Any(HelloInnerTypes request)
        {
            return new HelloInnerTypesResponse();
        }

        public object Any(GetUserSession request)
        {
            return new CustomUserSession();
        }

        public object Any(QueryTemplate request)
        {
            return new QueryResponseTemplate<Poco>();
        }

        public object Any(HelloReserved request)
        {
            return request;
        }

        public object Any(HelloDictionary request)
        {
            return new Dictionary<string,string> { { request.Key ?? "key", request.Value } };
        }

        public object Any(HelloBuiltin request)
        {
            return request;
        }

        public object Any(HelloGet request)
        {
            return new HelloVerbResponse { Result = HttpMethods.Get };
        }

        public object Any(HelloPost request)
        {
            return new HelloVerbResponse { Result = HttpMethods.Post };
        }

        public object Any(HelloPut request)
        {
            return new HelloVerbResponse { Result = HttpMethods.Put };
        }

        public object Any(HelloDelete request)
        {
            return new HelloVerbResponse { Result = HttpMethods.Delete };
        }

        public object Any(HelloPatch request)
        {
            return new HelloVerbResponse { Result = HttpMethods.Patch };
        }

        public void Any(HelloReturnVoid request)
        {
        }

        public object Any(EnumRequest request)
        {
            return new EnumResponse { Operator = request.Operator };
        }

        public object Any(ExcludeTest1 request)
        {
            return new ExcludeTestNested { Id = 1 };
        }

        public object Any(ExcludeTest2 request)
        {
            return request.GetType().Name;
        }

        public object Any(ExcludeMetadata request)
        {
            return request;
        }

        public object Any(RestrictLocalhost request)
        {
            return request;
        }

        public object Any(RestrictInternal request)
        {
            return request;
        }

        public object Any(RestrictExternal request)
        {
            return request;
        }

        public object Any(IgnoreInMetadataConfig request)
        {
            return request;
        }

        public object Any(HelloTuple request) => request;

        [Authenticate]
        public object Any(HelloAuthenticated request)
        {
            var session = GetSession();

            return new HelloAuthenticatedResponse
            {
                Version = request.Version,
                SessionId = session.Id,
                UserName = session.UserName,
                Email = session.Email,
                IsAuthenticated = session.IsAuthenticated,
            };
        }
    }

    public class GetUserSession : IReturn<CustomUserSession>
    {
    }

    public partial class CustomUserSession
        : AuthUserSession
    {
        [DataMember]
        public virtual string CustomName { get; set; }

        [DataMember]
        public virtual string CustomInfo { get; set; }
    }
}