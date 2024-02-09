using System;

namespace ServiceStack;

/// <summary>
/// Ignore Auto Registering this Service in the IOC
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IgnoreServicesAttribute : AttributeBase {}
