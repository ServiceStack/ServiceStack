namespace ServiceStack.OrmLite.Oracle
{
    /// <summary>
    /// Oracle 18 specific provider
    /// </summary>
    public class Oracle18OrmLiteDialectProvider : Oracle11OrmLiteDialectProvider
    {
        public new static Oracle18OrmLiteDialectProvider Instance = new Oracle18OrmLiteDialectProvider();

        // JSON enhancements - https://docs.oracle.com/en/database/oracle/oracle-database/18/newft/new-features.html#GUID-82224A33-A394-4C21-ADB1-712092A15F62
        // GEO SPACIAL enhancements - 
    }
}