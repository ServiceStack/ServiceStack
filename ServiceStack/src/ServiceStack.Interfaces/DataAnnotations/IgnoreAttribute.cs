
using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Ignore property from consideration as an RDBMS column.
/// Properties with this attribute are ignored in all SQL.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreAttribute : AttributeBase {}

/// <summary>
/// Ignore this property in SELECT statements
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreOnSelectAttribute : AttributeBase { }

/// <summary>
/// Ignore this property in UPDATE statements
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreOnUpdateAttribute : AttributeBase { }

/// <summary>
/// Ignore this property in INSERT statements
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreOnInsertAttribute : AttributeBase { }

/// <summary>
/// Ignore Auto Registering this Service in the IOC
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IgnoreServicesAttribute : AttributeBase {}
