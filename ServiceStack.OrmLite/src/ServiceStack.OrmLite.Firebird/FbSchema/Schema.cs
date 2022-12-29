using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Legacy;
using ServiceStack.OrmLite.Firebird.DbSchema;

namespace ServiceStack.OrmLite.Firebird
{
	public class Schema : ISchema<Table, Column, Procedure, Parameter>
	{
		private string sqlTables;

		private StringBuilder sqlColumns = new StringBuilder();
		private StringBuilder sqlFieldGenerator = new StringBuilder();
		private StringBuilder sqlGenerator = new StringBuilder();
		private StringBuilder sqlProcedures = new StringBuilder();

		private StringBuilder  sqlParameters = new StringBuilder();

		public IDbConnection Connection { private get; set; }

		public Schema()
		{
			Init();
		}

		public List<Table> Tables => Connection.Select<Table>(sqlTables);

		public Table GetTable(string name)
		{

			string sql = sqlTables +
			             $"    AND a.rdb$relation_name ='{name}' ";

            var query = Connection.Select<Table>(sql);
            return query.FirstOrDefault();
        }
		
		public List<Column> GetColumns(string tableName)
		{

			string sql = string.Format(sqlColumns.ToString(),
									   string.IsNullOrEmpty(tableName) ? "idx.rdb$relation_name" : $"'{tableName}'",
									   string.IsNullOrEmpty(tableName) ? "r.rdb$relation_name" : $"'{tableName}'");

            List<Column> columns =Connection.Select<Column>(sql);

            List<Generador> gens = Connection.Select<Generador>(sqlGenerator.ToString());

            sql = string.Format(sqlFieldGenerator.ToString(),
                                string.IsNullOrEmpty(tableName) ? "TRIGGERS.RDB$RELATION_NAME" : $"'{tableName}'");

            List<FieldGenerator> fg = Connection.Select<FieldGenerator>(sql);

            foreach (var record in columns)
            {
                IEnumerable<string> query=  from q in fg
                                            where q.TableName == record.TableName
                                            && q.FieldName == record.Name
                                            select q.SequenceName;
                if (query.Count() == 1)
                    record.Sequence = query.First();
                else
                {
                    string g = (from gen in gens
                                where gen.Name == $"{tableName}_{record.Name}_GEN"
                                select gen.Name).FirstOrDefault();

                    if (!string.IsNullOrEmpty(g)) record.Sequence = g.Trim();
                }
            }

            return columns;
        }

		public List<Column> GetColumns(Table table)
		{
			return GetColumns(table.Name);
		}


		public Procedure GetProcedure(string name)
		{
			string sql= sqlProcedures.ToString() +
			            $"WHERE  b.rdb$procedure_name ='{name}'";

            var query = Connection.Select<Procedure>(sql);
            return query.FirstOrDefault();
        }

		public List<Procedure> Procedures => Connection.Select<Procedure>(sqlProcedures.ToString());

		public List<Parameter> GetParameters(Procedure procedure)
		{
			return GetParameters(procedure.Name);
		}

		public List<Parameter> GetParameters(string procedureName)
		{

			string sql = string.Format(sqlParameters.ToString(),
									   string.IsNullOrEmpty(procedureName) ? "a.rdb$procedure_name" : $"'{procedureName}'");

            return Connection.Select<Parameter>(sql);
        }
		
		private void Init()
		{

			sqlTables =
			"SELECT \n" +
			"    trim(a.rdb$relation_name) AS name, \n" +
			"    trim(a.rdb$owner_name)    AS owner \n" +
			"FROM \n" +
			"    rdb$relations a \n" +
			"WHERE\n" +
			"    rdb$system_flag = 0 \n" +
			"    AND rdb$view_blr IS NULL \n";


			sqlColumns.Append("SELECT TRIM(r.rdb$field_name)                          AS field_name, \n");
			sqlColumns.Append("       r.rdb$field_position                            AS field_position, \n");
			sqlColumns.Append("       CASE f.rdb$field_type \n");
			sqlColumns.Append("         WHEN 261 THEN " +
							  "         trim(iif(f.rdb$field_sub_type = 0,'BLOB', 'TEXT')) \n");
			sqlColumns.Append("         WHEN 14 THEN trim(iif( cset.rdb$character_set_name='OCTETS'and f.rdb$field_length=16,'GUID', 'CHAR' )) \n"); //CHAR
			sqlColumns.Append("         WHEN 40 THEN trim('VARCHAR') \n"); //CSTRING
			sqlColumns.Append("         WHEN 11 THEN trim('FLOAT') \n"); //D_FLOAT
			sqlColumns.Append("         WHEN 27 THEN trim('DOUBLE') \n");
			sqlColumns.Append("         WHEN 10 THEN trim('FLOAT') \n");
			sqlColumns.Append("         WHEN 16 THEN trim(Iif(f.rdb$field_sub_type = 0, 'BIGINT', \n");
			sqlColumns.Append("          Iif(f.rdb$field_sub_type = 1, 'NUMERIC', 'DECIMAL'))) \n");
			sqlColumns.Append("         WHEN 8 THEN trim(Iif(f.rdb$field_sub_type = 0, 'INTEGER', \n");
			sqlColumns.Append("          Iif(f.rdb$field_sub_type = 1, 'NUMERIC', 'DECIMAL'))) \n");
			sqlColumns.Append("         WHEN 9 THEN trim('BIGINT') \n");       //QUAD
			sqlColumns.Append("         WHEN 7 THEN trim('SMALLINT') \n");
			sqlColumns.Append("         WHEN 12 THEN trim('DATE') \n");
			sqlColumns.Append("         WHEN 13 THEN trim('TIME') \n");
			sqlColumns.Append("         WHEN 35 THEN trim('TIMESTAMP') \n");
			sqlColumns.Append("         WHEN 37 THEN trim('VARCHAR') \n");
			sqlColumns.Append("         ELSE trim('UNKNOWN') \n");
			sqlColumns.Append("        END                                             AS field_type, \n");
			//sqlColumns.Append("        f.rdb$field_sub_type  as field_subtype, \n");
			sqlColumns.Append("        cast(f.rdb$field_length as smallint)            AS field_length, \n");
			sqlColumns.Append("        cast(Coalesce(f.rdb$field_precision, -1) as smallint)          AS field_precision, \n");
			sqlColumns.Append("        cast( Abs(f.rdb$field_scale) as smallint )                     AS field_scale, \n");
			//sqlColumns.Append("        r.rdb$default_value                             AS default_value, \n");
			sqlColumns.Append("        Iif(Coalesce(r.rdb$null_flag, 0) = 1, 0, 1)     AS nullable, \n");
			sqlColumns.Append("        Coalesce(r.rdb$description, '')                 AS DESCRIPTION, \n");
			sqlColumns.Append("        TRIM(r.rdb$relation_name)                       AS tablename, \n");
			sqlColumns.Append("        Iif(idxs.constraint_type = 'PRIMARY KEY', 1, 0) AS is_primary_key, \n");
			sqlColumns.Append("        Iif(idxs.constraint_type = 'UNIQUE', 1, 0)      AS is_unique, \n");
			sqlColumns.Append("        cast ('' as varchar(31) ) as SEQUENCE_NAME, \n");
			sqlColumns.Append("        iif(R.RDB$UPDATE_FLAG=1,0,1) as IS_COMPUTED \n");
			sqlColumns.Append("FROM   rdb$relation_fields r \n");
			sqlColumns.Append("       LEFT JOIN rdb$fields f \n");
			sqlColumns.Append("         ON r.rdb$field_source = f.rdb$field_name \n");
			sqlColumns.Append("       LEFT JOIN rdb$character_sets cset \n");
			sqlColumns.Append("         ON f.rdb$character_set_id = cset.rdb$character_set_id \n");
			sqlColumns.Append("       LEFT JOIN (SELECT DISTINCT rc.rdb$constraint_type AS constraint_type, \n");
			sqlColumns.Append("                                  idxflds.rdb$field_name AS field_name \n");
			sqlColumns.Append("                  FROM   rdb$indices idx \n");
			sqlColumns.Append("                         LEFT JOIN rdb$relation_constraints rc \n");
			sqlColumns.Append("                           ON ( idx.rdb$index_name = rc.rdb$index_name ) \n");
			sqlColumns.Append("                         LEFT JOIN rdb$index_segments idxflds \n");
			sqlColumns.Append("                           ON ( idx.rdb$index_name = idxflds.rdb$index_name ) \n");
			sqlColumns.Append("                  WHERE  idx.rdb$relation_name = {0} \n");
			sqlColumns.Append("                         AND rc.rdb$constraint_type IN ( 'PRIMARY KEY', 'UNIQUE' \n");
			sqlColumns.Append("                                                       )) \n");
			sqlColumns.Append("                                                                         idxs \n");
			sqlColumns.Append("         ON idxs.field_name = r.rdb$field_name \n");
			sqlColumns.Append("WHERE  r.rdb$system_flag = '0' \n");
			sqlColumns.Append("       AND r.rdb$relation_name = {1} \n");
			sqlColumns.Append(" ORDER  BY r.rdb$relation_name,r.rdb$field_position \n");


			sqlFieldGenerator.Append("SELECT  trim(TRIGGERS.RDB$RELATION_NAME) as TableName,");
			sqlFieldGenerator.Append("trim(deps.rdb$field_name)        AS field_name, \n");
			sqlFieldGenerator.Append("       trim(deps2.rdb$depended_on_name) AS sequence_name \n");
			sqlFieldGenerator.Append("FROM   rdb$triggers triggers \n");
			sqlFieldGenerator.Append("       JOIN rdb$dependencies deps \n");
			sqlFieldGenerator.Append("         ON deps.rdb$dependent_name = triggers.rdb$trigger_name \n");
			sqlFieldGenerator.Append("            AND deps.rdb$depended_on_name = triggers.rdb$relation_name \n");
			sqlFieldGenerator.Append("            AND deps.rdb$dependent_type = 2 \n");
			sqlFieldGenerator.Append("            AND deps.rdb$depended_on_type = 0 \n");
			sqlFieldGenerator.Append("       JOIN rdb$dependencies deps2 \n");
			sqlFieldGenerator.Append("         ON deps2.rdb$dependent_name = triggers.rdb$trigger_name \n");
			sqlFieldGenerator.Append("            AND deps2.rdb$field_name IS NULL \n");
			sqlFieldGenerator.Append("            AND deps2.rdb$dependent_type = 2 \n");
			sqlFieldGenerator.Append("            AND deps2.rdb$depended_on_type = 14 \n");
			sqlFieldGenerator.Append("WHERE  triggers.rdb$system_flag = 0 \n");
			sqlFieldGenerator.Append("       AND triggers.rdb$trigger_type = 1 \n");
			sqlFieldGenerator.Append("       AND triggers.rdb$trigger_inactive = 0 \n");
			sqlFieldGenerator.Append("       AND triggers.rdb$relation_name = {0} ");


			sqlProcedures.Append("SELECT TRIM(b.rdb$procedure_name)           AS name, \n");
			sqlProcedures.Append("       TRIM(b.rdb$owner_name)               AS owner, \n");
			sqlProcedures.Append("       cast(Coalesce(b.rdb$procedure_inputs, 0) as smallint) AS inputs, \n");
			sqlProcedures.Append("       cast(Coalesce(b.rdb$procedure_outputs, 0) as smallint) AS outputs \n");
			sqlProcedures.Append("FROM   rdb$procedures b \n");


			sqlParameters.Append("SELECT TRIM(a.rdb$procedure_name)                            AS procedure_name, \n");
			sqlParameters.Append("       TRIM(a.rdb$parameter_name)                            AS parameter_name, \n");
			sqlParameters.Append("       CAST(a.rdb$parameter_number AS SMALLINT)              AS parameter_number \n");
			sqlParameters.Append("       , \n");
			sqlParameters.Append("       CAST(a.rdb$parameter_type AS SMALLINT)                AS \n");
			sqlParameters.Append("       parameter_type, \n");
			sqlParameters.Append("       TRIM(t.rdb$type_name)                                 AS field_type, \n");
			sqlParameters.Append("       CAST(b.rdb$field_length AS SMALLINT)                  AS field_length, \n");
			sqlParameters.Append("       CAST(Coalesce(b.rdb$field_precision, -1) AS SMALLINT) AS field_precision, \n");
			sqlParameters.Append("       CAST(b.rdb$field_scale AS SMALLINT)                   AS field_scale \n");
			//sqlParameters.Append("       --b.rdb$field_type       AS field_type,  \n");
			//sqlParameters.Append("       --b.rdb$field_sub_type   AS field_sub_type,  \n");

			sqlParameters.Append("FROM   rdb$procedure_parameters a \n");
			sqlParameters.Append("       JOIN rdb$fields b \n");
			sqlParameters.Append("         ON b.rdb$field_name = a.rdb$field_source \n");
			sqlParameters.Append("       JOIN rdb$types t \n");
			sqlParameters.Append("         ON t.rdb$type = b.rdb$field_type \n");
			sqlParameters.Append("WHERE  t.rdb$field_name = 'RDB$FIELD_TYPE' \n");
			sqlParameters.Append("       AND a.rdb$procedure_name = {0} \n");
			sqlParameters.Append("ORDER  BY a.rdb$procedure_name, \n");
			sqlParameters.Append("          a.rdb$parameter_type, \n");
			sqlParameters.Append("          a.rdb$parameter_number ");
			
			sqlGenerator.Append("SELECT trim(RDB$GENERATOR_NAME) AS \"Name\"	FROM RDB$GENERATORS");
		}
		
		private class FieldGenerator
		{

			[Alias("TABLENAME")]
			public string TableName
			{
				get;
				set;
			}

			[Alias("FIELD_NAME")]
			public string FieldName
			{
				get;
				set;
			}

			[Alias("SEQUENCE_NAME")]
			public string SequenceName
			{
				get;
				set;
			}

		}
		
		private class Generador{
			public string Name { get; set;}
		}
		
	}
}

/*ID--0--False--2 -- SMALLINT-- System.Int16-- 
NAME--1--False--60 -- VARCHAR-- System.String-- 
PASSWORD--2--False--30 -- VARCHAR-- System.String-- 
FULL_NAME--3--True--60 -- VARCHAR-- System.String-- 
COL1--4--False--2 -- VARCHAR-- System.String-- 
COL2--5--False--2 -- VARCHAR-- System.String-- 
COL3--6--False--2 -- VARCHAR-- System.String-- 
COL4--7--True--4 -- NUMERIC-- System.Nullable`1[System.Decimal]-- 
COL5--8--True--4 -- FLOAT-- System.Nullable`1[System.Single]-- 
COL6--9--True--4 -- INTEGER-- System.Nullable`1[System.Int32]-- 
COL7--10--True--8 -- DOUBLE-- System.Nullable`1[System.Double]-- 
COL8--11--True--8 -- BIGINT-- System.Nullable`1[System.Int64]-- 
COL9--12--True--4 -- DATE-- System.Nullable`1[System.DateTime]-- 
COL10--13--True--8 -- TIMESTAMP-- System.Nullable`1[System.DateTime]-- 
COL11--14--True--8 -- BLOB-- System.Nullable`1[System.Byte][]-- 
COLNUM--15--True--8 -- NUMERIC-- System.Nullable`1[System.Decimal]-- 
COLDECIMAL--16--True--8 -- DECIMAL-- System.Nullable`1[System.Decimal]-- 
--------------------------------------------
Columns for EMPLOYEE:
EMP_NO--0--False--2 -- SMALLINT-- System.Int16--EMP_NO_GEN 
FIRST_NAME--1--False--15 -- VARCHAR-- System.String-- 
LAST_NAME--2--False--20 -- VARCHAR-- System.String-- 
PHONE_EXT--3--True--4 -- VARCHAR-- System.String-- 
HIRE_DATE--4--False--8 -- TIMESTAMP-- System.DateTime-- 
DEPT_NO--5--False--3 -- CHAR-- System.String-- 
JOB_CODE--6--False--5 -- VARCHAR-- System.String-- 
JOB_GRADE--7--False--2 -- SMALLINT-- System.Int16-- 
JOB_COUNTRY--8--False--15 -- VARCHAR-- System.String-- 
SALARY--9--False--8 -- NUMERIC-- System.Decimal-- 
FULL_NAME--10--True--37 -- VARCHAR-- System.String--    Caculado !!!!! Computed True 

ID--0--False--4 -- INTEGER-- System.Int32--COMPANY_ID_GEN 
NAME--1--True--100 -- VARCHAR-- System.String-- 
TURNOVER--2--True--4 -- FLOAT-- System.Nullable`1[System.Single]-- 
STARTED--3--True--4 -- DATE-- System.Nullable`1[System.DateTime]-- 
EMPLOYEES--4--True--4 -- INTEGER-- System.Nullable`1[System.Int32]-- 
CREATED_DATE--5--True--8 -- TIMESTAMP-- System.Nullable`1[System.DateTime]-- 
GUID--6--True--16 -- GUID-- System.Nullable`1[System.Guid]-- 

 * 
 * 
 * 
 */