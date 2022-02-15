#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace ServiceStack;

/// <summary>
/// Avoid polluting extension methods on every type with a 'X.*' short-hand
/// </summary>
public static class X
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static To? Map<From, To>(From from, Func<From, To> fn) => from == null ? default : fn(from);
}
