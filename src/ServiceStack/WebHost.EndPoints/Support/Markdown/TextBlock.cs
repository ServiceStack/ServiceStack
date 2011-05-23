using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.WebHost.EndPoints.Formats;

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
				valueFn = DataBinder.CompileToString(type, modelMemberExpr);
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

		public List<TemplateBlock> ChildBlocks { get; set; }

		protected void Prepare(List<StatementExprBlock> allStatements)
		{
			if (this.Statement.IsNullOrEmpty()) return;

			var parsedStatement = Extract(this.Statement, allStatements);

			this.ChildBlocks = parsedStatement.CreateTemplateBlocks(allStatements);
			this.ChildBlocks.ForEach(x => x.IsNested = true);
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			WriteImpl(textWriter, scopeArgs);
		}

		private void WriteImpl(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			foreach (var templateBlock in ChildBlocks)
			{
				templateBlock.Write(textWriter, scopeArgs);
			}
		}

		public static string Extract(string content, List<StatementExprBlock> allStatements)
		{
			var sb = new StringBuilder();

			var initialCount = allStatements.Count;
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
					statementExpr.Prepare(allStatements);
					allStatements.Add(statementExpr);
					var placeholder = "@" + TemplateExtensions.StatementPlaceholderChar + allStatements.Count;
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

			return allStatements.Count > initialCount ? sb.ToString() : content;
		}

		protected void WriteStatement(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			if (IsNested)
			{
				//Write Markdown
				WriteImpl(textWriter, scopeArgs);
			}
			else
			{
				//Buffer Markdown output before converting and writing HTML
				var sb = new StringBuilder();
				using (var sw = new StringWriter(sb))
				{
					WriteImpl(sw, scopeArgs);
				}

				var markdown = sb.ToString();
				var html = MarkdownFormat.Instance.Transform(markdown);
				textWriter.Write(html);
			}
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
		public string MemberVarName { get; set; }

		private Func<object, object> memberExprFn;
		private Func<object, object> GetMemberExprFn(Type type)
		{
			if (memberExprFn == null)
			{
				memberExprFn = DataBinder.Compile(type, MemberExpr);
			}
			return memberExprFn;
		}

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
			this.MemberVarName = this.MemberExpr.GetVarName();
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			object model;
			if (!scopeArgs.TryGetValue(this.MemberVarName, out model))
				throw new ArgumentException(this.MemberVarName + " does not exist");

			var getMemberFn = GetMemberExprFn(model.GetType());
			var memberExprEnumerator = getMemberFn(model) as IEnumerable;

			if (memberExprEnumerator == null)
				throw new ArgumentException(this.MemberExpr + " is not an IEnumerable");

			if (IsNested)
			{
				//Write Markdown
				foreach (var item in memberExprEnumerator)
				{
					scopeArgs[this.EnumeratorName] = item;
					base.Write(textWriter, scopeArgs);
				}
			}
			else
			{
				//Buffer Markdown output before converting and writing HTML
				var sb = new StringBuilder();
				using (var sw = new StringWriter(sb))
				{
					foreach (var item in memberExprEnumerator)
					{
						scopeArgs[this.EnumeratorName] = item;
						base.Write(sw, scopeArgs);
					}
				}

				var markdown = sb.ToString();
				var html = MarkdownFormat.Instance.Transform(markdown);
				textWriter.Write(html);
			}
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
		private Evaluator evaluator;

		private void Prepare()
		{
			var parts = Condition.SplitOnWhiteSpaceAndSymbols();
			foreach (var part in parts)
			{
				var isLowerCase = part[0] >= 'a' && part[0] <= 'z';
				if (isLowerCase)
				{
					var varName = part.GetVarName();
					this.ParamNames.Add(varName);
				}
			}
		}

		public Evaluator GetEvaluator(List<object> paramValues)
		{
			if (this.evaluator == null)
			{
				var exprParams = new SortedDictionary<string, Type>();

				for (var i = 0; i < this.ParamNames.Count; i++)
				{
					var paramName = this.ParamNames[i];
					var paramValue = paramValues[i];

					exprParams[paramName] = paramValue.GetType();
				}

				this.evaluator = new Evaluator(typeof(bool), Condition, "IfCondition", exprParams);
			}
			return this.evaluator;
		}

		private List<object> GetParamValues(IDictionary<string, object> scopeArgs)
		{
			var results = new List<object>();
			foreach (var paramName in this.ParamNames)
			{
				object paramValue;
				if (!scopeArgs.TryGetValue(paramName, out paramValue))
					throw new ArgumentException("Unresolved param " + paramName + " in " + Condition);

				results.Add(paramValue);
			}
			return results;
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			var paramValues = GetParamValues(scopeArgs);
			var eval = GetEvaluator(paramValues);
			var resultCondition = eval.Eval<bool>("IfCondition", paramValues.ToArray());
			if (!resultCondition) return;

			WriteStatement(textWriter, scopeArgs);
		}
	}

	public abstract class TemplateBlock : ITemplateWriter
	{
		public bool IsNested { get; set; }

		public const string ModelVarName = "model";

		public abstract void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs);
	}
}