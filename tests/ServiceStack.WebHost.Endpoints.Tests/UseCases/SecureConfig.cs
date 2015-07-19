// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using ServiceStack.Auth;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases
{
    public class SecureConfig
    {
        public static string PublicKeyXml = "<RSAKeyValue><Modulus>s1/rrg2UxchL5O4yFKCHTaDQgr8Bfkr1kmPf8TCXUFt4WNgAxRFGJ4ap1Kc22rt/k0BRJmgC3xPIh7Z6HpYVzQroXuYI6+q66zyk0DRHG7ytsoMiGWoj46raPBXRH9Gj5hgv+E3W/NRKtMYXqq60hl1DvtGLUs2wLGv15K9NABc=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static string PrivateKeyXml = "<RSAKeyValue><Modulus>s1/rrg2UxchL5O4yFKCHTaDQgr8Bfkr1kmPf8TCXUFt4WNgAxRFGJ4ap1Kc22rt/k0BRJmgC3xPIh7Z6HpYVzQroXuYI6+q66zyk0DRHG7ytsoMiGWoj46raPBXRH9Gj5hgv+E3W/NRKtMYXqq60hl1DvtGLUs2wLGv15K9NABc=</Modulus><Exponent>AQAB</Exponent><P>6CiNjgn8Ov6nodG56rCOXBoSGksYUf/2C8W23sEBfwfLtKyqTbTk3WolBj8sY8QptjwFBF4eaQiFdVLt3jg08w==</P><Q>xcuu4OGTcSOs5oYqyzsQrOAys3stMauM2RYLIWqw7JGEF1IV9LBwbaW/7foq2dG8saEI48jxcskySlDgq5dhTQ==</Q><DP>KqzhsH13ZyTOjblusox37shAEaNCOjiR8wIKJpJWAxLcyD6BI72f4G+VlLtiHoi9nikURwRCFM6jMbjnztSILw==</DP><DQ>H4CvW7XRy+VItnaL/k5r+3zB1oA51H1kM3clUq8xepw6k5RJVu17GpuZlAeSJ5sWGJxzVAQ/IG8XCWsUPYAgyQ==</DQ><InverseQ>vTLuAT3rSsoEdNwZeH2/JDEWmQ1NGa5PUq1ak1UbDD0snhsfJdLo6at3isRqEtPVsSUK6I07Nrfkd6okGhzGDg==</InverseQ><D>M8abO9lVuSVQqtsKf6O6inDB3wuNPcwbSE8l4/O3qY1Nlq96wWd0DZK0UNqXXdnDQFjPU7uwIH4QYwQMCeoejl3dZlllkyvKVa3jihImDD++qgswX2DmHGDqTIkVABf1NF730gqTmt1kqXoVp5Y+VcO7CZPEygIQyTK4WwYlRjk=</D></RSAKeyValue>";
    }

    public class HelloSecure : IReturn<HelloSecureResponse>
    {
        public string Name { get; set; }
    }

    public class HelloSecureResponse
    {
        public string Result { get; set; }
    }

    public class GetSecure : IReturn<GetSecureResponse>
    {
        public string Name { get; set; }
    }

    public class GetSecureResponse
    {
        public string Result { get; set; }
    }

    public class HelloAuthenticated : IReturn<HelloAuthenticatedResponse>, IHasSessionId, IHasVersion
    {
        public string SessionId { get; set; }
        public int Version { get; set; }
    }

    public class LargeMessage : IReturn<LargeMessage>
    {
        public List<HelloSecure> Messages { get; set; }
    }

    [Authenticate]
    public class HelloAuthSecure : IReturn<HelloAuthSecureResponse>
    {
        public string Name { get; set; }
    }

    public class HelloAuthSecureResponse
    {
        public string Result { get; set; }
    }

    public class HelloAuthenticatedResponse
    {
        public int Version { get; set; }
        public string SessionId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsAuthenticated { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SecureServices : Service
    {
        public object Get(GetSecure request)
        {
            if (request.Name == null)
                throw new ArgumentNullException("Name");

            return new GetSecureResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

        public object Any(HelloSecure request)
        {
            if (request.Name == null)
                throw new ArgumentNullException("Name");

            return new HelloSecureResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

        public object Any(HelloAuthSecure request)
        {
            if (request.Name == null)
                throw new ArgumentNullException("Name");

            return new HelloAuthSecureResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

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

        public object Any(LargeMessage request)
        {
            return request;
        }
    }
}