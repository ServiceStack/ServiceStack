---
title: Built-In Identity Auth Admin UI
summary: Explore the new Identity Auth Admin UI for creating and managing Identity Auth users in .NET 8  
tags: [servicestack,.net8,auth]
image: https://images.unsplash.com/photo-1563920443079-783e5c786b83?crop=entropy&fit=crop&h=1000&w=2000
author: Lucy Bates
---

With ServiceStack now [deeply integrated into ASP.NET Core Apps](/posts/servicestack-endpoint-routing) we're back to
refocusing on adding value-added features that can benefit all .NET Core Apps.

## Registration

The new Identity Auth Admin UI is an example of this, which can be enabled when registering the `AuthFeature` Plugin:

```csharp
public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddPlugin(new AuthFeature(IdentityAuth.For<ApplicationUser>(
                options => {
                    options.SessionFactory = () => new CustomUserSession();
                    options.CredentialsAuth();
                    options.AdminUsersFeature();
                })));
        });
}
```

Which just like the ServiceStack Auth [Admin Users UI](https://docs.servicestack.net/admin-ui-users) enables a
Admin UI that's only accessible to **Admin** Users for managing **Identity Auth** users at `/admin-ui/users`.

## User Search Results

Which displays a limited view due to the minimal properties on the default `IdentityAuth` model:

<div style="height:580px">
    <img class="absolute left-0 right-0 mx-auto shadow" style="max-width:1496px" 
         src="https://servicestack.net/img/posts/identity-auth-admin-ui/admin-ui-users-default.png">
</div>

### Custom Search Result Properties

These User's search results are customizable by specifying the `ApplicationUser` properties to display instead, e.g:

```csharp
options.AdminUsersFeature(feature =>
{
    feature.QueryIdentityUserProperties =
    [
        nameof(ApplicationUser.Id),
        nameof(ApplicationUser.DisplayName),
        nameof(ApplicationUser.Email),
        nameof(ApplicationUser.UserName),
        nameof(ApplicationUser.LockoutEnd),
    ];
});
```

<div style="height:580px">
    <img class="absolute left-0 right-0 mx-auto shadow" style="max-width:1496px" 
         src="https://servicestack.net/img/posts/identity-auth-admin-ui/admin-ui-users-custom.png">
</div>

### Custom Search Result Behavior

The default display Order of Users is also customizable:

```csharp
feature.DefaultOrderBy = nameof(ApplicationUser.DisplayName);
```

As well as the Search behavior which can be replaced to search any custom fields, e.g:

```csharp
feature.SearchUsersFilter = (q, query) =>
{
    var queryUpper = query.ToUpper();
    return q.Where(x =>
        x.DisplayName!.Contains(query) ||
        x.Id.Contains(queryUpper) ||
        x.NormalizedUserName!.Contains(queryUpper) ||
        x.NormalizedEmail!.Contains(queryUpper));
};
```

## Default Create and Edit Users Forms

The default Create and Edit Admin Users UI are also limited to editing the minimal `IdentityAuth` properties:

<div style="height:640px">
    <img class="absolute left-0 right-0 mx-auto shadow" style="max-width:1200px" 
         src="https://servicestack.net/img/posts/identity-auth-admin-ui/admin-ui-users-create.png">
</div>

Whilst the Edit page includes standard features to lockout users, change user passwords and manage their roles:

<div style="height:650px">
    <img class="absolute left-0 right-0 mx-auto shadow" style="max-width:1200px" 
         src="https://servicestack.net/img/posts/identity-auth-admin-ui/admin-ui-users-edit.png">
</div>

### Custom Create and Edit Forms

By default Users are locked out indefinitely, but this can also be changed to lock users out to a specific date, e.g:

```csharp
feature.ResolveLockoutDate = user => DateTimeOffset.Now.AddDays(7);
```

The forms editable fields can also be customized to include additional properties, e.g:

```csharp
feature.FormLayout =
[
    Input.For<ApplicationUser>(x => x.UserName, c => c.FieldsPerRow(2)),
    Input.For<ApplicationUser>(x => x.Email, c => { 
        c.Type = Input.Types.Email;
        c.FieldsPerRow(2); 
    }),
    Input.For<ApplicationUser>(x => x.FirstName, c => c.FieldsPerRow(2)),
    Input.For<ApplicationUser>(x => x.LastName, c => c.FieldsPerRow(2)),
    Input.For<ApplicationUser>(x => x.DisplayName, c => c.FieldsPerRow(2)),
    Input.For<ApplicationUser>(x => x.PhoneNumber, c =>
    {
        c.Type = Input.Types.Tel;
        c.FieldsPerRow(2); 
    }),
];
```

That can override the new `ApplicationUser` Model that's created and any Validation:

### Custom User Creation

```csharp
feature.CreateUser = () => new ApplicationUser { EmailConfirmed = true };
feature.CreateUserValidation = async (req, createUser) =>
{
    await IdentityAdminUsers.ValidateCreateUserAsync(req, createUser);
    var displayName = createUser.GetUserProperty(nameof(ApplicationUser.DisplayName));
    if (string.IsNullOrEmpty(displayName))
        throw new ArgumentNullException(nameof(AdminUserBase.DisplayName));
    return null;
};
```

<div style="height:640px">
    <img class="absolute left-0 right-0 mx-auto shadow" style="max-width:1200px" 
         src="https://servicestack.net/img/posts/identity-auth-admin-ui/admin-ui-users-create-custom.png">
</div>

### Admin User Events

Should you need to, Admin User Events can use used to execute custom logic before and after creating, updating and 
deleting users, e.g:

```csharp
feature.OnBeforeCreateUser = (request, user) => { ... };
feature.OnAfterCreateUser  = (request, user) => { ... };
feature.OnBeforeUpdateUser = (request, user) => { ... };
feature.OnAfterUpdateUser  = (request, user) => { ... };
feature.OnBeforeDeleteUser = (request, userId) => { ... };
feature.OnAfterDeleteUser  = (request, userId) => { ... };
```
