using ServiceStack;

namespace MyApp.ServiceModel;

[ValidateApiKey]
public class GetAccount : IGet, IReturn<GetAccountResponse>
{
}
public class GetAccountResponse
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public string[] Roles { get; set; }
}

public class GetKey : IGet, IReturn<string>
{
}