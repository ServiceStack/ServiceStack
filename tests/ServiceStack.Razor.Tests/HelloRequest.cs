using ServiceStack.Web;

namespace ServiceStack.Razor.Tests
{
    public class HelloRequest
    {
        public string Name { get; set; }
    }
    public class HelloResponse
    {
        public string Result { get; set; }
    }

    //https://github.com/ServiceStack/ServiceStack/wiki/New-Api
    public class HelloService : Service, IAny<HelloRequest>
    {
        //public HelloResponse Any( Hello h )
        //{
        //    //return new HelloResponse { Result = "Hello, " + h.Name };
        //    return h;
        //}


        public object Any(HelloRequest request)
        {
            //return new HelloResponse { Result = "Hello, " + request.Name };
            return new { Foo = "foo", Password = "pwd", Pw2 = "222", FooMasta = new { Charisma = 10, Mula = 10000000000, Car = "911Turbo" } };
        }
    }


    public class FooRequest
    {
        public string WhatToSay { get; set; }
    }

    public class FooResponse
    {
        public string FooSaid { get; set; }
    }

    public class DefaultViewFooRequest
    {
        public string WhatToSay { get; set; }
    }

    public class DefaultViewFooResponse
    {
        public string FooSaid { get; set; }
    }

    public class FooController : Service, IGet<FooRequest>, IPost<FooRequest>
    {
        public object Get(FooRequest request)
        {
            return new FooResponse { FooSaid = string.Format("GET: {0}", request.WhatToSay) };
        }

        public object Post(FooRequest request)
        {
            return new FooResponse { FooSaid = string.Format("POST: {0}", request.WhatToSay) };
        }

        [DefaultView("DefaultViewFoo")]
        public object Get(DefaultViewFooRequest request)
        {
            if (request.WhatToSay == "redirect")
            {
                return HttpResult.Redirect("/");
            }
            return new DefaultViewFooResponse { FooSaid = string.Format("GET: {0}", request.WhatToSay) };
        }
    }
}
