using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Legacy;
using ServiceStack.OrmLite.Oracle.DbSchema;

namespace ServiceStack.OrmLite.Oracle
{
	public class Schema : ISchema<Table, Column, Procedure, Parameter>
	{
		private string sqlTables;

		private StringBuilder sqlColumns = new StringBuilder();
		private StringBuilder sqlFieldGenerator = new StringBuilder();
		private StringBuilder sqlGenerator = new StringBuilder();
		private StringBuilder sqlProcedures = new StringBuilder();
        private StringBuilder sqlColConstrains = new StringBuilder();        

		private StringBuilder  sqlParameters = new StringBuilder();

		public IDbConnection Connection { private get; set; }

		public Schema()
		{
			Init();
		}

		public List<Table> Tables
		{
			get
			{
                return Connection.SelectFmt<Table>(sqlTables);
			}
		}

		public Table GetTable(string name)
		{
			string sql = sqlTables + string.Format("    WHERE TABLE_NAME ='{0}' ", name);

            var query = Connection.SelectFmt<Table>(sql);
            return query.FirstOrDefault();
		}
		
		public List<Column> GetColumns(string tableName)
		{

			string sql = string.Format(sqlColumns.ToString(),string.IsNullOrEmpty(tableName) ? "\'\'" : string.Format("\'{0}\'", tableName));

            List<Column> columns = Connection.SelectFmt<Column>(sql);

            List<Generador> gens = Connection.SelectFmt<Generador>(sqlGenerator.ToString());

            foreach (var record in columns)
            {
                record.IsPrimaryKey = (Connection.ScalarFmt<int>(string.Format(sqlColConstrains.ToString(), tableName, record.Name, "P")) > 0);
                record.IsUnique = (Connection.ScalarFmt<int>(string.Format(sqlColConstrains.ToString(), tableName, record.Name, "U")) > 0);
                string g = (from gen in gens
                            where gen.Name == tableName + "_" + record.Name + "_GEN"
                            select gen.Name).FirstOrDefault();

                if (!string.IsNullOrEmpty(g)) record.Sequence = g.Trim();
            }
            return columns;
        }

		public List<Column> GetColumns(Table table)
		{
			return GetColumns(table.Name);
		}


		public Procedure GetProcedure(string name)
		{
			string sql=  string.Format(" sqlProcedures.ToString() ", name);
            var query = Connection.SelectFmt<Procedure>(sql);
            return query.FirstOrDefault();
        }

		public List<Procedure> Procedures
		{
			get
			{
                return Connection.SelectFmt<Procedure>(sqlProcedures.ToString());
			}
		}

		public List<Parameter> GetParameters(Procedure procedure)
		{
			return GetParameters(procedure.ProcedureName);
		}

		public List<Parameter> GetParameters(string procedureName)
		{
			string sql = string.Format(sqlParameters.ToString(), string.IsNullOrEmpty(procedureName) ? "" :procedureName);
            return Connection.SelectFmt<Parameter>(sql);
		}
		
		private void Init()
		{
            sqlTables = "select TABLE_NAME, USER TABLE_SCHEMA  from USER_TABLES ";

			sqlColumns.Append(" select * \n");
			sqlColumns.Append(" from USER_TAB_COLS utc  \n");
			sqlColumns.Append(" where table_name = {0} \n");
            sqlColumns.Append(" AND hidden_column =  \'NO\' \n");
            
			sqlColumns.Append(" order by column_id \n");

            sqlProcedures.Append("SELECT * FROM ALL_PROCEDURES WHERE OBJECT_TYPE = \'PROCEDURE\'  OR  OBJECT_TYPE = \'FUNCTION\' \n");
			sqlParameters.Append("select * from user_arguments WHERE OBJECT_NAME = \'{0}\' ORDER BY Position asc\n");
            sqlGenerator.Append("SELECT TABLE_NAME AS \"Name\" FROM ALL_CATALOG WHERE Table_Type = \'SEQUENCE\' ");
            
            sqlColConstrains.Append(" SELECT Count(cols.position) \n");
            sqlColConstrains.Append(" FROM all_constraints cons, all_cons_columns cols \n");
            sqlColConstrains.Append(" WHERE cols.table_name = \'{0}\' \n");
            sqlColConstrains.Append(" AND cons.constraint_type = \'{2}\' \n");
            sqlColConstrains.Append(" AND cons.constraint_name = cols.constraint_name \n");
            sqlColConstrains.Append(" AND cons.owner = cols.owner \n");
            sqlColConstrains.Append(" AND cols.column_name = \'{1}\' \n");            
            sqlColConstrains.Append(" ORDER BY cols.table_name, cols.position \n");
		}		
		
		private class Generador
        {
			public string Name { get; set; }
		}
	}

}