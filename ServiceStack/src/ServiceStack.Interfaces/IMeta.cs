﻿#nullable enable
// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;

namespace ServiceStack;

public interface IMeta
{
    Dictionary<string, string>? Meta { get; set; }
}

public interface IHasQueryParams
{
    Dictionary<string, string>? QueryParams { get; set; }
}

public interface IHasSessionId
{
    string? SessionId { get; set; }
}

public interface IHasBearerToken
{
    string? BearerToken { get; set; }
}

public interface IHasRefreshToken
{
    string? RefreshToken { get; set; }
}
public interface IHasRefreshTokenExpiry : IHasRefreshToken
{
    DateTime? RefreshTokenExpiry { get; set; }
}

public interface IHasAuthSecret
{
    string? AuthSecret { get; set; }
}

public interface IHasClaimsPrincipal
{
    System.Security.Claims.ClaimsPrincipal? User { get; }
}

public interface IHasVersion
{
    int Version { get; set; }
}

public interface IRequireRefreshToken
{
    string? RefreshToken { get; set; }
    DateTime? RefreshTokenExpiry { get; set; }
}
