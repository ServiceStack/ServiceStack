// 
//   MarkdownDeep - http://www.toptensoftware.com/markdowndeep
//	 Copyright (C) 2010-2011 Topten Software
// 
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this product except in 
//   compliance with the License. You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software distributed under the License is 
//   distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//   See the License for the specific language governing permissions and limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkdownDeep
{
	public class LinkDefinition
	{
		public LinkDefinition(string id)
		{
			this.id= id;
		}

		public LinkDefinition(string id, string url)
		{
			this.id = id;
			this.url = url;
		}

		public LinkDefinition(string id, string url, string title)
		{
			this.id = id;
			this.url = url;
			this.title = title;
		}

		public string id
		{
			get;
			set;
		}

		public string url
		{
			get;
			set;
		}

		public string title
		{
			get;
			set;
		}


		internal void RenderLink(Markdown m, StringBuilder b, string link_text)
		{
			if (url.StartsWith("mailto:"))
			{
				b.Append("<a href=\"");
				Utils.HtmlRandomize(b, url);
				b.Append('\"');
				if (!String.IsNullOrEmpty(title))
				{
					b.Append(" title=\"");
					Utils.SmartHtmlEncodeAmpsAndAngles(b, title);
					b.Append('\"');
				}
				b.Append('>');
				Utils.HtmlRandomize(b, link_text);
				b.Append("</a>");
			}
			else
			{
				HtmlTag tag = new HtmlTag("a");

				// encode url
				StringBuilder sb = m.GetStringBuilder();
				Utils.SmartHtmlEncodeAmpsAndAngles(sb, url);
				tag.attributes["href"] = sb.ToString();

				// encode title
				if (!String.IsNullOrEmpty(title ))
				{
					sb.Length = 0;
					Utils.SmartHtmlEncodeAmpsAndAngles(sb, title);
					tag.attributes["title"] = sb.ToString();
				}

				// Do user processing
				m.OnPrepareLink(tag);

				// Render the opening tag
				tag.RenderOpening(b);

				b.Append(link_text);	  // Link text already escaped by SpanFormatter
				b.Append("</a>");
			}
		}

		internal void RenderImg(Markdown m, StringBuilder b, string alt_text)
		{
			HtmlTag tag = new HtmlTag("img");

			// encode url
			StringBuilder sb = m.GetStringBuilder();
			Utils.SmartHtmlEncodeAmpsAndAngles(sb, url);
			tag.attributes["src"] = sb.ToString();

			// encode alt text
			if (!String.IsNullOrEmpty(alt_text))
			{
				sb.Length = 0;
				Utils.SmartHtmlEncodeAmpsAndAngles(sb, alt_text);
				tag.attributes["alt"] = sb.ToString();
			}

			// encode title
			if (!String.IsNullOrEmpty(title))
			{
				sb.Length = 0;
				Utils.SmartHtmlEncodeAmpsAndAngles(sb, title);
				tag.attributes["title"] = sb.ToString();
			}

			tag.closed = true;

			m.OnPrepareImage(tag, m.RenderingTitledImage);

			tag.RenderOpening(b);
		}


		// Parse a link definition from a string (used by test cases)
		internal static LinkDefinition ParseLinkDefinition(string str, bool ExtraMode)
		{
			StringScanner p = new StringScanner(str);
			return ParseLinkDefinitionInternal(p, ExtraMode);
		}

		// Parse a link definition
		internal static LinkDefinition ParseLinkDefinition(StringScanner p, bool ExtraMode)
		{
			int savepos=p.position;
			var l = ParseLinkDefinitionInternal(p, ExtraMode);
			if (l==null)
				p.position = savepos;
			return l;

		}

		internal static LinkDefinition ParseLinkDefinitionInternal(StringScanner p, bool ExtraMode)
		{
			// Skip leading white space
			p.SkipWhitespace();

			// Must start with an opening square bracket
			if (!p.SkipChar('['))
				return null;

			// Extract the id
			p.Mark();
			if (!p.Find(']'))
				return null;
			string id = p.Extract();
			if (id.Length == 0)
				return null;
			if (!p.SkipString("]:"))
				return null;

			// Parse the url and title
			var link=ParseLinkTarget(p, id, ExtraMode);

			// and trailing whitespace
			p.SkipLinespace();

			// Trailing crap, not a valid link reference...
			if (!p.eol)
				return null;

			return link;
		}

		// Parse just the link target
		// For reference link definition, this is the bit after "[id]: thisbit"
		// For inline link, this is the bit in the parens: [link text](thisbit)
		internal static LinkDefinition ParseLinkTarget(StringScanner p, string id, bool ExtraMode)
		{
			// Skip whitespace
			p.SkipWhitespace();

			// End of string?
			if (p.eol)
				return null;

			// Create the link definition
			var r = new LinkDefinition(id);

			// Is the url enclosed in angle brackets
			if (p.SkipChar('<'))
			{
				// Extract the url
				p.Mark();

				// Find end of the url
				while (p.current != '>')
				{
					if (p.eof)
						return null;
					p.SkipEscapableChar(ExtraMode);
				}

				string url = p.Extract();
				if (!p.SkipChar('>'))
					return null;

				// Unescape it
				r.url = Utils.UnescapeString(url.Trim(), ExtraMode);

				// Skip whitespace
				p.SkipWhitespace();
			}
			else
			{
				// Find end of the url
				p.Mark();
				int paren_depth = 1;
				while (!p.eol)
				{
					char ch=p.current;
					if (char.IsWhiteSpace(ch))
						break;
					if (id == null)
					{
						if (ch == '(')
							paren_depth++;
						else if (ch == ')')
						{
							paren_depth--;
							if (paren_depth==0)
								break;
						}
					}

					p.SkipEscapableChar(ExtraMode);
				}

				r.url = Utils.UnescapeString(p.Extract().Trim(), ExtraMode);
			}

			p.SkipLinespace();

			// End of inline target
			if (p.DoesMatch(')'))
				return r;

			bool bOnNewLine = p.eol;
			int posLineEnd = p.position;
			if (p.eol)
			{
				p.SkipEol();
				p.SkipLinespace();
			}

			// Work out what the title is delimited with
			char delim;
			switch (p.current)
			{
				case '\'':  
				case '\"':
					delim = p.current;
					break;

				case '(':
					delim = ')';
					break;

				default:
					if (bOnNewLine)
					{
						p.position = posLineEnd;
						return r;
					}
					else
						return null;
			}

			// Skip the opening title delimiter
			p.SkipForward(1);

			// Find the end of the title
			p.Mark();
			while (true)
			{
				if (p.eol)
					return null;

				if (p.current == delim)
				{

					if (delim != ')')
					{
						int savepos = p.position;

						// Check for embedded quotes in title

						// Skip the quote and any trailing whitespace
						p.SkipForward(1);
						p.SkipLinespace();

						// Next we expect either the end of the line for a link definition
						// or the close bracket for an inline link
						if ((id == null && p.current != ')') ||
							(id != null && !p.eol))
						{
							continue;
						}

						p.position = savepos;
					}

					// End of title
					break;
				}

				p.SkipEscapableChar(ExtraMode);
			}

			// Store the title
			r.title = Utils.UnescapeString(p.Extract(), ExtraMode);

			// Skip closing quote
			p.SkipForward(1);

			// Done!
			return r;
		}
	}
}
