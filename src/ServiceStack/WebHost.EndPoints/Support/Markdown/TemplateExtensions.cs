using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Common;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public static class TemplateExtensions
	{
		private const string UnwantedPrefix = "<p>";
		private const string UnwantedSuffix = "</p>";
		private const char EscapeChar = '\\';
		private const char QuoteChar = '"';
		public const char StatementPlaceholderChar = '^';
		private const char BeginStatementChar = '{';
		private const char EndStatementChar = '}';
		static readonly char[] WhiteSpaceChars = new[] { ' ', '\t', '\r', '\n' };
		static readonly char[] WhiteSpaceAndSymbolChars = new[] {
			' ', '\t', '\r', '\n', '(', ')', '!', '+', '-'
		};

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

		public static string[] SplitOnWhiteSpace(this string text)
		{
			return text.SplitAndTrimOn(WhiteSpaceChars);
		}

		public static string[] SplitOnWhiteSpaceAndSymbols(this string text)
		{
			return text.SplitAndTrimOn(WhiteSpaceAndSymbolChars);
		}

		public static string[] SplitAndTrimOn(this string text, char[] chars)
		{
			if (text == null) return new string[0];
			var parts = text.Split(chars);
			var results = new List<string>();

			foreach (var part in parts)
			{
				var val = part.Trim();
				if (string.IsNullOrEmpty(val)) continue;

				results.Add(part);
			}

			return results.ToArray();
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

		public static string CaptureOutput(this List<TemplateBlock> childBlocks, 
			TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			var sb = new StringBuilder();
			using (var sw = new StringWriter(sb))
			{
				foreach (var templateBlock in childBlocks)
				{
					templateBlock.Write(sw, scopeArgs);
				}
			}

			var output = sb.ToString();
			return output;
		}

		public static void RemoveIfEndingWith(this TextBlock textBlock, string text)
		{
			if (textBlock == null) return;
			if (textBlock.Content.EndsWith(text))
			{
				textBlock.Content = textBlock.Content.Substring(
					0, textBlock.Content.Length - text.Length);
			}
		}

		public static string TrimIfEndingWith(this string content, string text)
		{
			if (content == null || !content.EndsWith(text)) return content;
			return content.Substring(0, content.Length - text.Length);
		}

		public static void SkipIfNextIs(this string content, ref int pos, string text)
		{
			if (content == null || text == null) return;

			if (content.Length < pos + text.Length) return;

			var test = content.Substring(pos, text.Length);
			if (test != text) return;

			pos += text.Length;
		}

		public static string TrimLineIfOnlyHasWhitespace(this string text)
		{
			var pos = text.LastIndexOf('\n') + 1; //after \n or at start if not found
			if (pos == text.Length) return text;
			var startPos = pos;
			text.EatWhitespace(ref pos);
			if (pos == text.Length) return text.Substring(0, startPos);
			return text;
		}

		public static List<TemplateBlock> CreateTemplateBlocks(this string content, List<StatementExprBlock> statementBlocks)
		{
			var blocks = new List<TemplateBlock>();
			if (content.IsNullOrEmpty()) return blocks;

			int pos;
			var lastPos = 0;
			while ((pos = content.IndexOf('@', lastPos)) != -1)
			{
				var contentBlock = content.Substring(lastPos, pos - lastPos);
				var prevTextBlock = new TextBlock(contentBlock);
				blocks.Add(prevTextBlock);

				pos++; //@

				if (content[pos] == StatementPlaceholderChar)
				{
					pos++; //^
					var index = content.GetNextAlphaNumericExpr(ref pos);
					int statementNo;
					if (int.TryParse(index, out statementNo))
					{
						var statementIndex = statementNo - 1;
						if (statementIndex >= statementBlocks.Count)
							throw new ArgumentOutOfRangeException(
								"Expected < " + statementBlocks.Count + " but was " + statementIndex);
						
						var statement = statementBlocks[statementIndex];
						blocks.Add(statement);
					}

					//Strip everything but @^1 in <p>@^1</p>\n
					prevTextBlock.RemoveIfEndingWith(UnwantedPrefix);
					content.SkipIfNextIs(ref pos, UnwantedSuffix);
					content.SkipIfNextIs(ref pos, "\n");
				}
				else
				{
					var memberExpr = content.GetNextMemberExpr(ref pos);
					if (memberExpr != null)
					{
						blocks.Add(new MemberExprBlock(memberExpr));
					}
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

		public static StatementExprBlock GetNextStatementExpr(this string content, ref int fromPos)
		{
			var varExpr = content.GetNextAlphaNumericExpr(ref fromPos);
			if (varExpr != null)
			{
				if (varExpr == "foreach" || varExpr == "if")
				{
					var conditionEndPos = content.IndexOf(BeginStatementChar, fromPos);
					if (conditionEndPos == -1)
						throw new InvalidDataException(varExpr + " at index: " + fromPos);

					var condition = content.Substring(fromPos, conditionEndPos - fromPos)
						.Trim('(', ')', ' ');

					fromPos = conditionEndPos;
					var statement = content.EatStatementExpr(ref fromPos);
					statement = statement.TrimLineIfOnlyHasWhitespace();

					if (varExpr == "foreach")
						return new ForEachStatementExprBlock(condition, statement);

					if (varExpr == "if")
						return new IfStatementExprBlock(condition, statement);
				}
			}
			return null;
		}

		public static void EatWhitespace(this string content, ref int index)
		{
			int c;
			for (; index < content.Length; index++)
			{
				c = content[index];
				if (c >= WhiteSpaceFlags.Length || !WhiteSpaceFlags[c])
				{
					break;
				}
			}
		}

		public static void EatRestOfLine(this string content, ref int index)
		{
			int c;
			for (; index < content.Length; index++)
			{
				c = content[index];
				if (c >= WhiteSpaceFlags.Length || !WhiteSpaceFlags[c])
				{
					return;
				}
				if (c != '\n') continue;

				index++;
				return;
			}
		}

		private static string EatStatementExpr(this string content, ref int fromPos)
		{
			content.EatWhitespace(ref fromPos);
			if (content[fromPos++] != BeginStatementChar)
				throw new InvalidDataException("Expected { at: " + fromPos);
			content.EatRestOfLine(ref fromPos);

			var startPos = fromPos;
			
			var withinQuotes = false;
			var endsToEat = 1;
			while (++fromPos < content.Length && endsToEat > 0)
			{
				var c = content[fromPos];

				if (c == QuoteChar
					&& content[fromPos - 1] != EscapeChar)
					withinQuotes = !withinQuotes;

				if (withinQuotes)
					continue;

				if (c == BeginStatementChar)
					endsToEat++;

				if (c == EndStatementChar)
					endsToEat--;
			}

			//content.EatRestOfLine(ref fromPos);

			return content.Substring(startPos, fromPos - startPos - 1);
		}

		public static string GetNextAlphaNumericExpr(this string content, ref int fromPos)
		{
			var startPos = fromPos;
			for (; fromPos < content.Length; fromPos++)
			{
				var exprChar = content[fromPos];
				if (exprChar >= AlphaNumericFlags.Length) return null;
				if (!MemberExprFlags[exprChar]) break;
			}
			return content.Substring(startPos, fromPos - startPos);
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
			
			if (fromPos == startPos) return null;

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