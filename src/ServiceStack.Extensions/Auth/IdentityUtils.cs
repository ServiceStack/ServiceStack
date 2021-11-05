#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ServiceStack.Auth;

public class IdentityException : Exception
{
    public string Code { get; }
    private List<IdentityError> Errors { get; }
    
    public IdentityException(List<IdentityError> errors) 
        : base(errors[0].Description)
    {
        Code = errors[0].Code;
        Errors = errors;
    }
}

public static class IdentityUtils
{
    public static void AssertSucceeded(this IdentityResult result)
    {
        if (!result.Succeeded)
            throw new IdentityException(result.Errors.ToList());
    }

    public static void AssertSucceededSync(this Task<IdentityResult> taskResult)
        => taskResult.GetAwaiter().GetResult().AssertSucceeded();
}