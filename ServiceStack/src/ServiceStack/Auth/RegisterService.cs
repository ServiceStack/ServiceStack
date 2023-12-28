using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Auth;

[ErrorView(nameof(Register.ErrorView))]
[DefaultRequest(typeof(Register))]
public class RegisterService : RegisterUserAuthServiceBase
{
    public static ValidateFn ValidateFn { get; set; }
        
    public static bool AllowUpdates { get; set; }

    /// <summary>
    /// Update an existing registration
    /// </summary>
    [Obsolete("Use PostAsync")]
    public Task<object> PutAsync(Register request)
    {
        return PostAsync(request);
    }

    /// <summary>
    /// Create new Registration
    /// </summary>
    [Obsolete("Use PostAsync")]
    public object Post(Register request)
    {
        try
        {
            var task = PostAsync(request);
            var response = task.GetResult();
            return response;
        }
        catch (Exception e)
        {
            throw e.UnwrapIfSingleException();
        }
    }
        
    /// <summary>
    /// Create new Registration
    /// </summary>
    public async Task<object> PostAsync(Register request)
    {
        var authFeature = GetPlugin<AuthFeature>();
        if (authFeature != null)
        {
            if (authFeature.SaveUserNamesInLowerCase)
            {
                if (request.UserName != null)
                    request.UserName = request.UserName.ToLower();
                if (request.Email != null)
                    request.Email = request.Email.ToLower();
            }
        }
            
        var validateResponse = ValidateFn?.Invoke(this, HttpMethods.Post, request);
        if (validateResponse != null)
            return validateResponse;

        var session = await this.GetSessionAsync().ConfigAwait();
        var newUserAuth = ToUser(request);

        var existingUser = session.IsAuthenticated 
            ? await AuthRepositoryAsync.GetUserAuthAsync(session, null).ConfigAwait() 
            : null;
        var registerNewUser = existingUser == null;

        if (!registerNewUser && !AllowUpdates)
            throw new NotSupportedException(ErrorMessages.RegisterUpdatesDisabled.Localize(Request));

        var runValidation = !HostContext.AppHost.GlobalRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync) //Already gets run
                            && RegistrationValidator != null;

        if (registerNewUser)
        {
            if (runValidation)
                await ValidateAndThrowAsync(request);
            var user = await AuthRepositoryAsync.CreateUserAuthAsync(newUserAuth, request.Password).ConfigAwait();
            await RegisterNewUserAsync(session, user).ConfigAwait();
        }
        else
        {
            if (runValidation)
                await RegistrationValidator.ValidateAndThrowAsync(request, ApplyTo.Put).ConfigAwait();
            var user = await AuthRepositoryAsync.UpdateUserAuthAsync(existingUser, newUserAuth, request.Password).ConfigAwait();
        }

        var response = await CreateRegisterResponse(session, 
            request.UserName ?? request.Email, request.Password, request.AutoLogin);
        return response;
    }

    /// <summary>
    /// Logic to update UserAuth from Registration info, not enabled on PUT because of security.
    /// </summary>
    public object UpdateUserAuth(Register request)
    {
        if (!HostContext.AppHost.GlobalRequestFiltersAsyncArray.Contains(ValidationFilters.RequestFilterAsync)) //Already gets run
        {
            RegistrationValidator?.ValidateAndThrow(request, ApplyTo.Put);
        }

        var response = ValidateFn?.Invoke(this, HttpMethods.Put, request);
        if (response != null)
            return response;

        var session = this.GetSession();

        var authRepo = HostContext.AppHost.GetAuthRepository(base.Request);
        using (authRepo as IDisposable)
        {
            var existingUser = authRepo.GetUserAuth(session, null);
            if (existingUser == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

            var newUserAuth = ToUser(request);
            authRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password);

            return new RegisterResponse
            {
                UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
            };
        }
    }

    /// <summary>
    /// Logic to update UserAuth from Registration info, not enabled on PUT because of security.
    /// </summary>
    public async Task<object> UpdateUserAuthAsync(Register request)
    {
        if (!HostContext.AppHost.GlobalRequestFiltersAsyncArray.Contains(ValidationFilters.RequestFilterAsync)) //Already gets run
        {
            await RegistrationValidator.ValidateAndThrowAsync(request, ApplyTo.Put).ConfigAwait();
        }

        var response = ValidateFn?.Invoke(this, HttpMethods.Put, request);
        if (response != null)
            return response;

        var session = await this.GetSessionAsync().ConfigAwait();

        var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(base.Request);
        await using (authRepo as IAsyncDisposable)
        {
            var existingUser = await authRepo.GetUserAuthAsync(session, null).ConfigAwait();
            if (existingUser == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

            var newUserAuth = ToUser(request);
            await authRepo.UpdateUserAuthAsync(existingUser, newUserAuth, request.Password).ConfigAwait();

            return new RegisterResponse
            {
                UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
            };
        }
    }
}