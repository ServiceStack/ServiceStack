using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Create an RDBMS Check Constraint that only allows the values of the property's Enum, e.g:
/// <para>CHECK (Status IN ('New','Shipped','Cancelled'))</para>
/// Adding a value to the Enum needs a migration to replace the constraint of existing tables.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CheckEnumAttribute : AttributeBase;
