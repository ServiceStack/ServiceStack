using System;
using System.Security.Cryptography;

namespace ServiceStack.Text;

public class TextConfig
{
    public static Func<SHA1> CreateSha { get; set; } = SHA1.Create;
}
