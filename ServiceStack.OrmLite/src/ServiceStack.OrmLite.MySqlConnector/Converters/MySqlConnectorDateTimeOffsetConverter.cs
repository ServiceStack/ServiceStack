using System;
using System.Globalization;

namespace ServiceStack.OrmLite.MySql.Converters;

/// <summary>
/// DateTimeOffsets are stored in VARCHAR columns. MySqlConnector serializes DateTimeOffset params as UTC date times
/// without their offset (which are read back as local times), so explicitly send them as ISO 8601 strings.
/// </summary>
public class MySqlConnectorDateTimeOffsetConverter : MySqlDateTimeOffsetConverter
{
    public override object ToDbValue(Type fieldType, object value) => value is DateTimeOffset dateTimeOffset
        ? dateTimeOffset.ToString("o", CultureInfo.InvariantCulture)
        : base.ToDbValue(fieldType, value);
}
