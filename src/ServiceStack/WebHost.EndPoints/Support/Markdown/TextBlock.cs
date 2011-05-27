using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Markdown;
using ServiceStack.Text;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public abstract class TemplateBlock : ITemplateWriter
	{
		public MarkdownPage Page { get; set; }

		protected Evaluator Evaluator { get; set; }

		public bool IsNested { get; set; }

		public bool WriteRawHtml { get; set; }

		public const string ModelVarName = "model";

		public virtual void BeginFirstRun(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs)
		{
			this.Page = markdownPage;
		}

		public virtual void EndFirstRun(Evaluator evaluator)
		{
			this.Evaluator = evaluator;
		}

		public void AddEvalItem(EvaluatorItem evalItem)
		{
			this.Page.ExecutionContext.Items.Add(evalItem);
		}

		public string Transform(string markdownText)
		{
			//TODO: call instance var of Page
			return MarkdownFormat.Instance.Transform(markdownText);
		}

		public abstract void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs);
	}

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

		private bool ReferencesSelf
		{
			get { return this.modelMemberExpr == null; }
		}

		public MemberExprBlock(string memberExpr)
		{
			try
			{
				this.varName = memberExpr.GetVarName();
				this.modelMemberExpr = varName != memberExpr
					? memberExpr.Substring(this.varName.Length + 1)
					: null;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public Func<object, string> valueFn;

		public override void BeginFirstRun(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs)
		{
			base.BeginFirstRun(markdownPage, scopeArgs);

			object memberExprValue;
			if (scopeArgs.TryGetValue(this.varName, out memberExprValue))
			{
				valueFn = this.ReferencesSelf
					? Convert.ToString
					: DataBinder.CompileToString(memberExprValue.GetType(), modelMemberExpr);
			}
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
				var strValue = this.ReferencesSelf
					? Convert.ToString(memberExprValue)
					: valueFn(memberExprValue);

				textWriter.Write(HttpUtility.HtmlEncode(strValue));
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
			this.ChildBlocks = new List<TemplateBlock>();
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

			RemoveTrailingNewLineIfProceedsStatement();
		}

		private void RemoveTrailingNewLineIfProceedsStatement()
		{
			if (this.ChildBlocks.Count < 2) return;

			var lastIndex = this.ChildBlocks.Count - 1;
			if (!(this.ChildBlocks[lastIndex - 1] is StatementExprBlock)) return;

			var textBlock = this.ChildBlocks[lastIndex] as TextBlock;
			if (textBlock == null) return;

			if (textBlock.Content == "\r\n")
			{
				this.ChildBlocks.RemoveAt(lastIndex);
			}
		}

		public int Id { get; set; }

		public void BeginFirstRun(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs, bool applyToChildren)
		{
			if (applyToChildren)
				this.BeginFirstRun(markdownPage, scopeArgs);
			else
				base.BeginFirstRun(markdownPage, scopeArgs);
		}
		
		public override void BeginFirstRun(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs)
		{
			base.BeginFirstRun(markdownPage, scopeArgs);

			this.Id = Page.GetNextId();

			foreach (var templateBlock in ChildBlocks)
			{
				templateBlock.BeginFirstRun(markdownPage, scopeArgs);
			}
		}

		public override void EndFirstRun(Evaluator evaluator)
		{
			base.EndFirstRun(evaluator);

			foreach (var templateBlock in ChildBlocks)
			{
				templateBlock.EndFirstRun(evaluator);
			}
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			WriteInternal(textWriter, scopeArgs);
		}

		private void WriteInternal(TextWriter textWriter, Dictionary<string, object> scopeArgs)
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

				var startPos = pos;
				pos++; //@

				var statementExpr = content.GetNextStatementExpr(ref pos);
				if (statementExpr != null)
				{
					contentBlock = contentBlock.TrimLineIfOnlyHasWhitespace();
					sb.Append(contentBlock);

					if (statementExpr is MethodStatementExprBlock)
						sb.Append(' '); //ensure a spacer between method blocks

					statementExpr.Prepare(allStatements);
					allStatements.Add(statementExpr);
					var placeholder = "@" + TemplateExtensions.StatementPlaceholderChar + allStatements.Count;
					sb.Append(placeholder);
					lastPos = pos;
				}
				else
				{
					sb.Append(contentBlock);

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
				WriteInternal(textWriter, scopeArgs);
			}
			else
			{
				//Buffer Markdown output before converting and writing HTML
				var sb = new StringBuilder();
				using (var sw = new StringWriter(sb))
				{
					WriteInternal(sw, scopeArgs);
				}

				var markdown = sb.ToString();
				var html = Transform(markdown);
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

		private object GetModel(Dictionary<string, object> scopeArgs)
		{
			object model;
			if (!scopeArgs.TryGetValue(this.MemberVarName, out model))
				throw new ArgumentException(this.MemberVarName + " does not exist");
			return model;
		}

		private IEnumerable GetMemberExprEnumerator(object model)
		{
			var memberExprEnumerator = getMemberFn(model) as IEnumerable;
			if (memberExprEnumerator == null)
				throw new ArgumentException(this.MemberExpr + " is not an IEnumerable");
			return memberExprEnumerator;
		}

		private Func<object, object> getMemberFn;
		public override void BeginFirstRun(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs)
		{
			base.BeginFirstRun(markdownPage, scopeArgs, false);
			var model = GetModel(scopeArgs);

			getMemberFn = DataBinder.Compile(model.GetType(), MemberExpr);
			var memberExprEnumerator = GetMemberExprEnumerator(model);

			foreach (var item in memberExprEnumerator)
			{
				scopeArgs[this.EnumeratorName] = item;
				foreach (var templateBlock in ChildBlocks)
				{
					templateBlock.BeginFirstRun(markdownPage, scopeArgs);
				}
			}
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			var model = GetModel(scopeArgs);
			var memberExprEnumerator = GetMemberExprEnumerator(model);

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
				var html = Transform(markdown);
				textWriter.Write(html);
			}
		}
	}

	public abstract class EvalExprStatementBase : StatementExprBlock
	{
		protected EvalExprStatementBase(string condition, string statement)
			: base(condition, statement)
		{
		}

		protected Type ReturnType = typeof(string);
		private string[] paramNames;
		protected string codeGenMethodName;

		public string[] GetParamNames(Dictionary<string, object> scopeArgs)
		{
			return this.paramNames ?? (this.paramNames = scopeArgs.Keys.ToArray());
		}

		public override void BeginFirstRun(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs)
		{
			base.BeginFirstRun(markdownPage, scopeArgs);

			codeGenMethodName = "EvalExpr_" + this.Id;

			var exprParams = new Dictionary<string, Type>();

			paramNames = GetParamNames(scopeArgs);
			var paramValues = GetParamValues(scopeArgs);
			for (var i = 0; i < paramNames.Length; i++)
			{
				var paramName = paramNames[i];
				var paramValue = paramValues[i];

				exprParams[paramName] = paramValue.GetType();
			}

			AddEvalItem(new EvaluatorItem(ReturnType, codeGenMethodName, Condition, exprParams));
		}

		protected List<object> GetParamValues(IDictionary<string, object> scopeArgs)
		{
			var results = new List<object>();
			foreach (var paramName in paramNames)
			{
				object paramValue;
				if (!scopeArgs.TryGetValue(paramName, out paramValue))
					throw new ArgumentException("Unresolved param " + paramName + " in " + Condition);

				results.Add(paramValue);
			}
			return results;
		}

		public T Evaluate<T>(Dictionary<string, object> scopeArgs)
		{
			var paramValues = GetParamValues(scopeArgs);
			return (T)Evaluator.Evaluate(codeGenMethodName, paramValues.ToArray());
		}
	}

	public class IfStatementExprBlock : EvalExprStatementBase
	{
		public IfStatementExprBlock(string condition, string statement)
			: base(condition, statement)
		{
			this.ReturnType = typeof(bool);
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			var resultCondition = Evaluate<bool>(scopeArgs);
			if (!resultCondition) return;

			WriteStatement(textWriter, scopeArgs);
		}
	}

	public class MethodStatementExprBlock : EvalExprStatementBase
	{
		public MethodStatementExprBlock(string methodExpr, string condition, string statement)
			: base(condition, statement)
		{
			this.methodExpr = methodExpr;
		}

		private readonly string methodExpr;
		public override void BeginFirstRun(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs)
		{
			Prepare(markdownPage);
			base.BeginFirstRun(markdownPage, scopeArgs);
		}

		private void Prepare(MarkdownPage markdownPage)
		{
			var rawMethodExpr = methodExpr.Replace("Html.", "");
			this.WriteRawHtml = rawMethodExpr == "Raw" || rawMethodExpr == "Partial";

			var argEx = new ArgumentException("Unable to resolve method: " + methodExpr);

			var parts = methodExpr.Split('.');
			if (parts.Length > 2)
				throw argEx;
			 
			var usesBaseType = parts.Length == 1;
			var typePropertyName = parts[0];
			var methodName = usesBaseType ? parts[0] : parts[1];

			var type = typePropertyName == "Html" ? typeof(HtmlHelper) : null;
			if (type == null)
			{
				type = usesBaseType
					? markdownPage.Markdown.MarkdownBaseType
					: markdownPage.Markdown.MarkdownGlobalHelpers.TryGetValue(typePropertyName, out type) ? type : null;
			}

			if (type == null)
				throw new InvalidDataException(string.Format(
					"Unable to resolve type '{0}'. Check type exists in Config.MarkdownBaseType or Page.Markdown.MarkdownGlobalHelpers",
					typePropertyName));

			var mi = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
			if (mi == null) throw argEx;

			base.ReturnType = mi.ReturnType;

			var isMemberExpr = Condition.IndexOf('(') != -1;
			if (!isMemberExpr)
			{
				base.Condition = methodName + "(" + Condition + ")";
			}
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			var paramValues = GetParamValues(scopeArgs);
			var result = Evaluator.Evaluate(codeGenMethodName, paramValues.ToArray());
			if (result == null) return;

			var strResult = result as string ?? Convert.ToString(result);

			if (!WriteRawHtml)
				strResult = HttpUtility.HtmlEncode(strResult);

			textWriter.Write(strResult);
		}
	}
}