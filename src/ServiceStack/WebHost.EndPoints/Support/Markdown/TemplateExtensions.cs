using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Common;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public static class TemplateExtensions
	{
		static readonly char[] WhiteSpaceChars = new[] { ' ', '\t', '\r', '\n' };
		static readonly bool[] AlphaNumericFlags = new bool[128];
		static readonly bool[] MemberExprFlags = new bool[128];
		static readonly bool[] StatementFlags = new bool[128];
		static readonly bool[] WhiteSpaceFlags = new bool[128];

		static TemplateExtensions()
		{
			for (int i = 'A'; i < 'Z'; i++)
			{
				AlphaNumericFlags[i] = true;
				MemberExprFlags[i] = true;
				StatementFlags[i] = true;
			}
			for (int i = 'a'; i < 'z'; i++)
			{
				AlphaNumericFlags[i] = true;
				MemberExprFlags[i] = true;
				StatementFlags[i] = true;
			}
			for (int i = '0'; i < '9'; i++)
			{
				AlphaNumericFlags[i] = true;
				MemberExprFlags[i] = true;
				StatementFlags[i] = true;
			}

			var exprChars = new[] { '.', '[', ']' };
			var tokenChars = new[] { '(', ')', '{', '}' };

			foreach (var exprChar in exprChars)
			{
				MemberExprFlags[exprChar] = true;
				StatementFlags[exprChar] = true;
			}
			foreach (var whitespaceChar in WhiteSpaceChars)
			{
				StatementFlags[whitespaceChar] = true;
				WhiteSpaceFlags[whitespaceChar] = true;
			}
			foreach (var tokenChar in tokenChars)
			{
				StatementFlags[tokenChar] = true;
			}
		}

		public static string RenderToString(this ITemplateWriter templateWriter, Dictionary<string, object> scopeArgs)
		{
			var sb = new StringBuilder();
			using (var writer = new StringWriter(sb))
			{
				templateWriter.Write(writer, scopeArgs);
			}
			return sb.ToString();
		}

		public static string RenderToString(this IEnumerable<ITemplateWriter> templateWriters, Dictionary<string, object> scopeArgs)
		{
			var sb = new StringBuilder();
			using (var writer = new StringWriter(sb))
			{
				foreach (var templateWriter in templateWriters)
				{
					templateWriter.Write(writer, scopeArgs);
				}
			}
			return sb.ToString();
		}

		public static List<TemplateBlock> SplitIntoBlocks(this string content, string onPlaceHolder)
		{
			var blocks = new List<TemplateBlock>();
			if (content.IsNullOrEmpty()) return blocks;

			var pos = 0;
			var lastPos = 0;
			while ((pos = content.IndexOf(onPlaceHolder, lastPos)) != -1)
			{
				var contentBlock = content.Substring(lastPos, pos - lastPos);

				blocks.Add(new TextBlock(contentBlock));

				lastPos = pos + onPlaceHolder.Length;
			}

			if (lastPos != content.Length - 1)
			{
				var lastBlock = content.Substring(lastPos);
				blocks.Add(new TextBlock(lastBlock));
			}

			return blocks;
		}

		public static List<TemplateBlock> CreateTemplateBlocks(this string content)
		{
			var blocks = new List<TemplateBlock>();
			if (content.IsNullOrEmpty()) return blocks;

			var pos = 0;
			var lastPos = 0;
			while ((pos = content.IndexOf('@', lastPos)) != -1)
			{
				var contentBlock = content.Substring(lastPos, pos - lastPos);
				blocks.Add(new TextBlock(contentBlock));

				pos++; //@
				var memberExpr = GetNextMemberExpr(content, ref pos);
				if (memberExpr != null)
				{
					blocks.Add(new MemberExprBlock(memberExpr));
				}

				lastPos = pos;
			}

			if (lastPos != content.Length - 1)
			{
				var lastBlock = lastPos == 0 ? content : content.Substring(lastPos);
				blocks.Add(new TextBlock(lastBlock));
			}

			return blocks;
		}

		public static string GetNextMemberExpr(this string content, ref int fromPos)
		{
			var startPos = fromPos;
			for (; fromPos < content.Length; fromPos++)
			{
				var exprChar = content[fromPos];
				if (exprChar >= MemberExprFlags.Length) return null;
				if (!MemberExprFlags[exprChar]) break;
			}
			return content.Substring(startPos, fromPos - startPos);
		}

		public static string RemoveAllWhiteSpace(this string content)
		{
			return content.RemoveCharFlags(WhiteSpaceFlags);
		}

		public static string GetVarName(this string memberExpr)
		{
			if (memberExpr == null) return null;

			for (var i = 0; i < memberExpr.Length; i++)
			{
				var exprChar = memberExpr[i];
				if (exprChar >= AlphaNumericFlags.Length) return null;
				if (!AlphaNumericFlags[exprChar]) return memberExpr.Substring(0, i);
			}

			return memberExpr;
		}

	}
}