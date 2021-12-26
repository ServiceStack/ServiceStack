using System.Security.Cryptography;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases;

public class JwtAuthProviderRsa256ReaderTests
{
    readonly RSAParameters privateKey;
    readonly RSAParameters publicKey;
    readonly string privateKeyXml;
    readonly string publicKeyXml;

    public const string Username = "rsa256reader";
    public const string Password = "p@55word";
    private readonly ServiceStackHost appHost;
    class AppHost : AppSelfHostBase
    {
        public AppHost()
            : base(nameof(JwtAuthProviderTests), typeof(JwtServices).Assembly) { }

        public virtual JwtAuthProviderReader JwtAuthProviderReader { get; set; }

        public override void Configure(Container container)
        {
            // just for testing, create a privateKeyXml on every instance
            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[]
                {
                    new BasicAuthProvider(),
                    new CredentialsAuthProvider(),
                    JwtAuthProviderReader,
                }));

            Plugins.Add(new RegistrationFeature());

            container.Register<IAuthRepository>(x => new InMemoryAuthRepository());

            var authRepo = GetAuthRepository();
            authRepo.CreateUserAuth(new UserAuth
            {
                Id = 1,
                UserName = Username,
                FirstName = "First",
                LastName = "Last",
                DisplayName = "Display",
            }, Password);
        }
    }
        
    public JwtAuthProviderRsa256ReaderTests()
    {
        privateKey = RsaUtils.CreatePrivateKeyParams(RsaKeyLengths.Bit2048);
        publicKey = privateKey.ToPublicRsaParameters();
        privateKeyXml = privateKey.ToPrivateKeyXml();
        publicKeyXml = privateKey.ToPublicKeyXml();

        appHost = new AppHost
            {
                JwtAuthProviderReader = CreateJwtAuthProviderReader()
            }
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    protected JwtAuthProvider CreateJwtAuthProvider()
    {
        return new() {
            HashAlgorithm = "RS256",
            RequireSecureConnection = false,
            AllowInQueryString = true,
            AllowInFormData = true,
            PrivateKeyXml = privateKeyXml,
        };
    }

    protected JwtAuthProviderReader CreateJwtAuthProviderReader()
    {
        return new() {
            HashAlgorithm = "RS256",
            RequireSecureConnection = false,
            AllowInQueryString = true,
            AllowInFormData = true,
            PublicKeyXml = publicKeyXml,
        };
    }
    protected virtual IJsonServiceClient GetClient() => new JsonServiceClient(Config.ListeningOn);

    private string CreateJwtToken()
    {
        var jwtProvider = CreateJwtAuthProvider();

        var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
        var body = JwtAuthProvider.CreateJwtPayload(new AuthUserSession {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com",
                IsAuthenticated = true,
            },
            issuer: jwtProvider.Issuer,
            expireIn: jwtProvider.ExpireTokensIn,
            audiences: jwtProvider.Audiences,
            roles: new[] {"TheRole"},
            permissions: new[] {"ThePermission"});

        var jwtToken = JwtAuthProvider.CreateJwt(header, body, jwtProvider.GetHashAlgorithm());
        return jwtToken;
    }
 
    [Test]
    public void Can_create_JWT_RSA_Signed_Token_validated_with_Reader()
    {
        var jwtToken = CreateJwtToken();
        jwtToken.Print();

        var jwtAuth = CreateJwtAuthProviderReader();
            
        // JWT Signature is Verified
        var jwtBody = jwtAuth.GetVerifiedJwtPayload(null, jwtToken.Split('.'));
        Assert.That(jwtBody, Is.Not.Null);
        Assert.That(jwtBody["sub"], Is.EqualTo("1"));
            
        // JWT is Valid
        var invalidError = jwtAuth.GetInvalidJwtPayloadError(jwtBody);
        Assert.That(invalidError, Is.Null);

        Assert.That(jwtAuth.IsJwtValid(jwtToken));
    }

    [Test]
    public void Can_Authenticate_with_Reader()
    {
        var authClient = GetClient();
        authClient.BearerToken = CreateJwtToken();

        var response = authClient.Send(new HelloJwt { Name = "from auth service" });
        Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));
    }
}