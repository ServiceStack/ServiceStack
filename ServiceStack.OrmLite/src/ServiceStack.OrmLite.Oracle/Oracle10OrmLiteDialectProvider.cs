namespace ServiceStack.OrmLite.Oracle
{
    using System;
    using ServiceStack.DataAnnotations;
    using ServiceStack.Model;
    using ServiceStack.OrmLite.Dapper;
    using ServiceStack.OrmLite.Oracle.Converters;

    /// <summary>
    /// Oracle v11 specific provider
    /// </summary>
    public class Oracle11OrmLiteDialectProvider : OracleOrmLiteDialectProvider
    {
        public new static Oracle11OrmLiteDialectProvider Instance = new Oracle11OrmLiteDialectProvider();

        protected new const int MaxNameLength = 128;

        public Oracle11OrmLiteDialectProvider()
        {
            NamingStrategy = new OracleNamingStrategy(MaxNameLength);

            RegisterConverter<String>(new Oracle12StringConverter());
        }


        // Column level collation (case-insensitive)
        // Case insensitive db (collation)
        // AL32UTF8 default char set

        // Change to VARCHAR2, NVARCHAR2, and RAW Datatypes to max string size 32767 
        // Json support
        // GeoJson - https://docs.oracle.com/en/database/oracle/oracle-database/12.2/spatl/spatial-concepts.html#GUID-D703DF4D-57D1-4990-8F53-CAAA9C8FCB2F 

        // CAST returns user-specified value if conversion error https://docs.oracle.com/en/database/oracle/oracle-database/12.2/newft/new-features.html#GUID-03517A06-2AA8-4EE5-9A20-B76E519EB69C
        // VALIDATE_CONVERSION function 
        // LISTAGG overflow facility
        // Identifiers (table/field names) increase from 30 bytes to 128 bytes

        // IDENTITY cols in table definition (ints only)
        // CREATE TABLE SEQ_TABLE2 (NO NUMBER GENERATED AS IDENTITY (START WITH 10 INCREMENT BY 5), NAME VARCHAR2(100));
        // 

        // Change to paging
        // SELECT ENAME, SAL
        // FROM EMP
        // ORDER BY SAL DESC
        // OFFSET 2 ROWS
        // FETCH FIRST 5 ROWS ONLY;
    }
}