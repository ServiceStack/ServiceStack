using System;
using System.Collections.Generic;
using System.IO;
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

	public abstract class TemplateBlock : ITemplateWriter
	{
		public const string ModelVarName = "model";

		public abstract void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs);
	}
}