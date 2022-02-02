using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ServiceStack.OrmLite.Firebird.DbSchema
{
	public abstract class PocoCreator<TTable, TColumn, TProcedure, TParameter>
		where TTable : ITable, new()
		where TColumn : IColumn, new()
		where TProcedure : IProcedure, new()
		where TParameter : IParameter, new(){
		
		public PocoCreator()
		{

			OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "src");
			Usings ="using System;\n" +
					"\n" +
					"using ServiceStack.Common;\n" +
					"using ServiceStack.DataAnnotations;\n" +
					"using ServiceStack.Model;\n";

			SpaceName = "Database.Records";
			MetadataClassName="Me";
			IdField = OrmLiteConfig.IdField;
		}

		public bool GenerateMetadata{get;set;}
		
		public string MetadataClassName{get; set;}
		
		public string IdField{ get; set;}
		
		public string SpaceName
		{
			get;
			set;
		}
		
		public string ServiceNameSpace
		{
			get;
			set;
		}
		
		public string Usings
		{
			get;
			set;
		}

		public string OutputDirectory
		{
			get;
			set;
		}

		public ISchema<TTable, TColumn, TProcedure, TParameter> Schema
		{
			get;
			set;
		}

		public virtual void WriteClass(TTable table)
		{
			WriteClass(table, table.Name);
		}

		public virtual void WriteClass(TTable table, string className)
		{
			if(string.IsNullOrEmpty( ServiceNameSpace)) ServiceNameSpace="Interface";
			className = ToDotName(className);
			StringBuilder properties= new StringBuilder();
			StringBuilder meProperties= new StringBuilder();
			List<TColumn> columns = Schema.GetColumns(table.Name);

			bool hasIdField = columns.Count(r => ToDotName(r.Name) == IdField) == 1;
			string idType= string.Empty;

			foreach (var cl in columns)
			{
				properties.AppendFormat("\t\t[Alias(\"{0}\")]\n", cl.Name);
				if (!string.IsNullOrEmpty(cl.Sequence)) properties.AppendFormat("\t\t[Sequence(\"{0}\")]\n", cl.Sequence);
				if (cl.IsPrimaryKey) properties.Append("\t\t[PrimaryKey]\n");
				if (cl.AutoIncrement) properties.Append("\t\t[AutoIncrement]\n");
				if ( TypeToString(cl.NetType)=="System.String"){
					if (!cl.Nullable) properties.Append("\t\t[Required]\n");
					properties.AppendFormat("\t\t[StringLength({0})]\n",cl.Length);
				}
				if(cl.DbType.ToUpper()=="DECIMAL" || cl.DbType.ToUpper()=="NUMERIC")
					properties.AppendFormat("\t\t[DecimalLength({0},{1})]\n",cl.Presicion, cl.Scale);
				if (cl.IsComputed) properties.Append("\t\t[Compute]\n");
					
				string propertyName;
				if(cl.AutoIncrement && cl.IsPrimaryKey && !hasIdField){
					propertyName= IdField;
					idType = TypeToString(cl.NetType);
					hasIdField=true;
				}
				else{
					propertyName= ToDotName(cl.Name);
					if(propertyName==IdField) idType= TypeToString(cl.NetType);
					else if(propertyName==className) propertyName= propertyName+"Name";
				}
				
				properties.AppendFormat("\t\tpublic {0}{1} {2} {{ get; set;}} \n\n",
										TypeToString(cl.NetType),
										(cl.Nullable && cl.NetType != typeof(string)) ? "?" : "",
										 propertyName);
				
				if(GenerateMetadata){
					if(meProperties.Length==0)
						meProperties.AppendFormat("\n\t\t\tpublic static string ClassName {{ get {{ return \"{0}\"; }}}}",
						                          className);
					meProperties.AppendFormat("\n\t\t\tpublic static string {0} {{ get {{ return \"{0}\"; }}}}",
					                         propertyName);
				}
				
			}
				    
			if (!Directory.Exists(OutputDirectory))
				Directory.CreateDirectory(OutputDirectory);
			
			string typesDir=Path.Combine(OutputDirectory,"Types");
			
			if(!Directory.Exists(typesDir))		
				Directory.CreateDirectory(typesDir);
						
			string attrDir=Path.Combine(OutputDirectory,"Attributes");
			if(!Directory.Exists(attrDir))		
				Directory.CreateDirectory(attrDir);
			
			string servDir=Path.Combine(OutputDirectory,"Services");
			if(!Directory.Exists(servDir))		
				Directory.CreateDirectory(servDir);
			
			using (TextWriter tw = new StreamWriter(Path.Combine(typesDir, className + ".cs")))
			{
				StringBuilder ns = new StringBuilder();
				StringBuilder cl =  new StringBuilder();
				StringBuilder me = new StringBuilder();
				cl.AppendFormat("\t[Alias(\"{0}\")]\n", table.Name);
				if(GenerateMetadata){
					me.AppendFormat("\n\t\tpublic static class {0} {{\n\t\t\t{1}\n\n\t\t}}\n",
					                MetadataClassName, meProperties.ToString());
					
				}
				cl.AppendFormat("\tpublic partial class {0}{1}{{\n\n\t\tpublic {0}(){{}}\n\n{2}{3}\t}}",
								className, 
				                hasIdField?string.Format( ":IHasId<{0}>",idType):"", 
				                properties.ToString(),
				                me.ToString());
			
				ns.AppendFormat("namespace {0}\n{{\n{1}\n}}", SpaceName, cl.ToString());
				tw.WriteLine(Usings);
				tw.WriteLine(ns.ToString());	
				
				tw.Close();
			}
			
			using (TextWriter twp = new StreamWriter(Path.Combine(attrDir, className + ".cs")))
			{
				twp.Write(string.Format(partialTemplate,SpaceName,className,IdField));				
				twp.Close();
			}
			
			using (TextWriter twp = new StreamWriter(Path.Combine(servDir, className + "Service.cs")))
			{
				twp.Write(string.Format(serviceTemplate,SpaceName, ServiceNameSpace, className));				
				twp.Close();
			}
			
		}

		public virtual void WriteClass(TProcedure procedure)
		{
			WriteClass(procedure, procedure.Name);
		}

		public virtual  void WriteClass(TProcedure procedure, string className)
		{

		}

		protected string ToDotName(string name)
		{

			StringBuilder t = new StringBuilder();
			string [] parts = name.Split('_');
			foreach (var s in parts)
			{
				t.Append(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower()));
			}
			return t.ToString();
		}


		protected string TypeToString(Type type)
		{
			string st = type.ToString();
			return (!st.Contains("[")) ? st : st.Substring(st.IndexOf("[") + 1, st.IndexOf("]") - st.IndexOf("[") - 1);
		}
		
		private string partialTemplate=@"using System;
using ServiceStack.ServiceHost;

namespace {0}
{{
	[RestService(""/{1}/create"",""post"")]
	[RestService(""/{1}/read"",""get"")]
	[RestService(""/{1}/read/{{{2}}}"",""get"")]
	[RestService(""/{1}/update/{{{2}}}"",""put"")]
	[RestService(""/{1}/destroy/{{{2}}}"",""delete"")]
	public partial class {1}
	{{
	}}
}}";
		
		private string serviceTemplate =@"using System;
 using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

using {0};

namespace {1}
{{
	[Authenticate]
	[RequiredPermission(""{2}.read"")]
	[RequiredPermission(ApplyTo.Post, ""{2}.create"")]	
	[RequiredPermission(ApplyTo.Put , ""{2}.update"")]	
	[RequiredPermission(ApplyTo.Delete, ""{2}.destroy"")]
	public class {2}Service:AppRestService<{2}>
	{{
		
	}}
}}";
		

	}
}