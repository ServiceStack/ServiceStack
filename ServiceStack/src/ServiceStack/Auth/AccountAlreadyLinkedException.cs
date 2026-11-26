using System;

namespace ServiceStack.Auth;

/// <summary>
/// Thrown by Auth Repositories when an OAuth provider identity is already linked to a different user account
/// </summary>
public class AccountAlreadyLinkedException : InvalidOperationException
{
    public string Provider { get; }

    public AccountAlreadyLinkedException(string provider)
        : base($"OAuth provider '{provider}' account is already linked to another user.")
    {
        Provider = provider;
    }
}
