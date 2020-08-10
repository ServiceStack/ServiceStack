using System;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests
{
    public class GrpcIntegrationTests
    {
        [Route("/hello")]
        [Route("/hello/{Name}")]
        [DataContract]
        public partial class Hello
            : IReturn<HelloResponse>
        {
            [DataMember(Order = 1)]
            public virtual string Name { get; set; }
        }

        [DataContract]
        public partial class HelloResponse
        {
            [DataMember(Order = 1)]
            public virtual string Result { get; set; }

            [DataMember(Order = 2)]
            public virtual ResponseStatus ResponseStatus { get; set; }
        }

        // [Test] // Integration Test
        public async Task Can_call_external_secure_service_using_remote_certificate()
        {
            try
            {
                // File.WriteAllBytes("grpc.crt", "https://todoworld.servicestack.net/grpc.crt".GetBytesFromUrl());
                // var cert = new X509Certificate2("grpc.crt");
                var cert = new X509Certificate2("https://todoworld.servicestack.net/grpc.crt".GetBytesFromUrl());

                var client = new GrpcServiceClient("https://todoworld.servicestack.net:50051",
                    cert, GrpcUtils.AllowSelfSignedCertificatesFrom("todoworld.servicestack.net"));

                var response = await client.GetAsync(new Hello {Name = "gRPC SSL C# 50051"});
                response.Result.Print();

                client = new GrpcServiceClient("https://todoworld.servicestack.net:5051",
                    cert, GrpcUtils.AllowSelfSignedCertificatesFrom("todoworld.servicestack.net"));

                response = await client.GetAsync(new Hello {Name = "gRPC SSL C# 5051"});

                response.Result.Print();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // [Test] // Integration Test
        public async Task Can_call_external_plaintext_service()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            var client = new GrpcServiceClient("http://todoworld.servicestack.net:50054");
            var response = await client.GetAsync(new Hello {Name = "gRPC Text C# 50054"});
            response.Result.Print();

            client = new GrpcServiceClient("http://todoworld.servicestack.net:5054");
            response = await client.GetAsync(new Hello {Name = "gRPC Text C# 5054"});
            response.Result.Print();
        }
    }
}