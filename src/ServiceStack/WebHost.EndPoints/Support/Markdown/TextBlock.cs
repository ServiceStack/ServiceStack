using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public class TextBlock : TemplateBlock
	{
		public TextBlock(string content)
		{
			Content = content;
		}

		public string Content { get; set; }

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			textWriter.Write(Content);
		}
	}

	public class VariableBlock : TemplateBlock
	{
		private readonly string varName;

		public VariableBlock(string varName)
		{
			this.varName = varName;
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			object value = null;
			scopeArgs.TryGetValue(varName, out value);

			if (value == null)
				return;

			textWriter.Write(value);
		}
	}

	public class MemberExprBlock : TemplateBlock
	{
		private static ILog Log = LogManager.GetLogger(typeof(MemberExprBlock));

		private readonly string modelMemberExpr;
		private readonly string varName;

		public MemberExprBlock(string memberExpr)
		{
			this.varName = memberExpr.GetVarName();
			this.modelMemberExpr = memberExpr.Substring(this.varName.Length + 1);
		}

		public Func<object, string> valueFn;
		public Func<object, string> GetValueFn(Type type)
		{
			if (valueFn == null)
			{
				valueFn = DataBinder.CompileDataBinder(type, modelMemberExpr);
			}
			return valueFn;
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			object memberExprValue;
			if (!scopeArgs.TryGetValue(this.varName, out memberExprValue))
			{
				textWriter.Write(modelMemberExpr);
				return;
			}
			
			if (memberExprValue == null) return;

			try
			{
				var valueFn = GetValueFn(memberExprValue.GetType());
				var strValue = valueFn(memberExprValue);
				textWriter.Write(strValue);
			}
			catch (Exception ex)
			{
				Log.Error("MemberExprBlock: " + ex.Message, ex);
			}
		}
	}

	public class StatementExprBlock : TemplateBlock
	{
		public StatementExprBlock(string condition, string statement)
		{
			this.Condition = condition;
			this.Statement = statement;
		}

		public string Condition { get; set; }
		public string Statement { get; set; }

		public List<StatementExprBlock> AllExprBlocks { get; set; }

		public List<TemplateBlock> ChildBlocks { get; set; }

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			throw new NotImplementedException();
		}

		public static List<StatementExprBlock> Parse(ref string content)
		{
			var blocks = new List<StatementExprBlock>();

			var sb = new StringBuilder();

			int pos;
			var lastPos = 0;
			while ((pos = content.IndexOf('@', lastPos)) != -1)
			{
				var contentBlock = content.Substring(lastPos, pos - lastPos);
				sb.Append(contentBlock);

				var startPos = pos;
				pos++; //@

				var statementExpr = content.GetNextStatementExpr(ref pos);				
				if (statementExpr != null)
				{
					blocks.Add(statementExpr);
					var placeholder = "@`" + blocks.Count;
					sb.Append(placeholder);
					lastPos = pos;
				}
				else
				{
					sb.Append('@');
					lastPos = startPos + 1;
				}
			}

			if (lastPos != content.Length - 1)
			{
				var lastBlock = lastPos == 0 ? content : content.Substring(lastPos);
				sb.Append(lastBlock);
			}

			if (blocks.Count > 0)
				content = sb.ToString();

			return blocks;
		}
	}

	public class ForEachStatementExprBlock : StatementExprBlock
	{
		public ForEachStatementExprBlock(string condition, string statement)
			: base(condition, statement)
		{
			Prepare();
		}

		public string EnumeratorName { get; set; }
		public string MemberExpr { get; set; }

		private void Prepare()
		{
			var parts = Condition.SplitOnWhiteSpace();
			if (parts.Length < 3)
				throw new InvalidDataException("Invalid foreach condition: " + Condition);

			var i = parts[0] == "var" ? 1 : 0;
			this.EnumeratorName = parts[i++];
			if (parts[i++] != "in")
				throw new InvalidDataException("Invalid foreach 'in' condition: " + Condition);

			this.MemberExpr = parts[i++];
		}
	}

	public class IfStatementExprBlock : StatementExprBlock
	{
		public IfStatementExprBlock(string condition, string statement)
			: base(condition, statement)
		{
			this.ParamNames = new List<string>();
			Prepare();
		}

		public List<string> ParamNames { get; set; } 
		public MethodInfo CompiledCondition { get; set; }

		private void Prepare()
		{
			var parts = Condition.SplitOnWhiteSpaceAndSymbols();
			foreach (var part in parts)
			{
				var isLowerCase = part[0] >= 'a' && part[0] <= 'z';
				if (isLowerCase)
				{
					this.ParamNames.Add(part);
				}
			}
		}
	}

	public abstract class TemplateBlock : ITemplateWriter
	{
		public const string ModelVarName = "model";

		public abstract void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs);
	}
}