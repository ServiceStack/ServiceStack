namespace ServiceStack.OpenApi.Tests.Services
{
    public class NativeTypesTestService : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse
            {
                Result = "Hello, {0}{1}!".Fmt(
                    request.Title != null ? request.Title + ". " : "",
                    request.Name)
            };
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

        public object Any(HelloArray request)
        {
            return request.Names.Map(name => new ArrayResult { Result = name });
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

        public object Any(HelloAllTypes request)
        {
            return new HelloAllTypesResponse
            {
                AllTypes = request.AllTypes,
                AllCollectionTypes = request.AllCollectionTypes,
                Result = request.Name
            };
        }

        public object Any(HelloAllTypesWithResult request)
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

        public object Any(HelloDateTime request)
        {
            return request;
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

        //public object Any(HelloWithListInheritance request)
        //{
        //    return request;
        //}

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

        public object Any(HelloInterface request)
        {
            return request;
        }

        public object Any(HelloInnerTypes request)
        {
            return new HelloInnerTypesResponse();
        }

        //Uncomment to generate SS.Client built-in types
        //public object Any(GenerateBuiltInTypes request)
        //{
        //    return request;
        //}

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

        public object Any(HelloTypes request)
        {
            return request;
        }

        public object Any(HelloZip request)
        {
            return request.Test == null
                ? new HelloZipResponse { Result = $"Hello, {request.Name} {base.Request.ContentLength}" }
                : new HelloZipResponse { Result = $"Hello, {request.Name} ({request.Test?.Count}) {base.Request.ContentLength}" };
        }
    }
}