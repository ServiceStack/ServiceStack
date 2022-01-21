#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.Auth;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface;


[Route("/profile-image")]
[Route("/profile-image/{Type}")]
public class GetProfileImage : IReturn<byte[]>
{
    public string Type { get; set; }
}

public class ImageServices : Service
{
    public async Task<object?> Any(GetProfileImage request)
    {
        var authSession = await GetSessionAsync();
        var userAuthId = authSession.UserAuthId;
        var userDetails = await AuthRepositoryAsync.GetUserAuthDetailsAsync(userAuthId);
        var accessToken = userDetails.FirstOrDefault(x => x.Provider == "microsoftgraph")?.AccessToken
            ?? throw HttpError.NotFound("No profile image for userAuthId: " + userAuthId);
        
        async Task<Stream> GetImage() =>
            await "https://graph.microsoft.com/beta/me/photo/$value"
                .GetStreamFromUrlAsync(requestFilter: req => req.AddBearerToken(accessToken)).ConfigAwait();

        if (request.Type == "original")
        {
            await using var imageStream = await GetImage();
            Response.ContentType = MimeTypes.ImagePng;
            await imageStream.CopyToAsync(Response.OutputStream);
        }
        else if (request.Type == "resize")
        {
            await using var imageStream = await GetImage();
            await using var resizedImage = ImageProvider.Instance.Resize(imageStream, 64, 64);
            Response.ContentType = MimeTypes.ImagePng;
            await resizedImage.CopyToAsync(Response.OutputStream);
        }
        else if (request.Type == "gateway")
        {
            var dataUri = await new AuthHttpGateway()
                .CreateMicrosoftPhotoUrlAsync(accessToken, "64x64");
            Response.ContentType = MimeTypes.PlainText;
            await Response.WriteAsync(dataUri);
        }
        else
        {
            Response.ContentType = MimeTypes.PlainText;
            return authSession.ProfileUrl;
        }
        return null;
    }
}
