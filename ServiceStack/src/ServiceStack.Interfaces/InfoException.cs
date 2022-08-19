using System;
using JetBrains.Annotations;

namespace ServiceStack;

/// <summary>
/// When only Exception message is important and StackTrace is irrelevant
/// </summary>
public class InfoException : Exception
{
    public InfoException([CanBeNull] string message) : base(message) {}

    public override string ToString() => Message;
}