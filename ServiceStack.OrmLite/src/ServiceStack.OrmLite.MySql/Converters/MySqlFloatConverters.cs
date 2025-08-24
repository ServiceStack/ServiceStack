using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.MySql.Converters;

public class MySqlDecimalConverter() : DecimalConverter(38, 6);