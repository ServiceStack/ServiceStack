#nullable enable
using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public class SynthesizeAttribute : AttributeBase {}