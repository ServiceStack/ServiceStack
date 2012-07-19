/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * This software is subject to the Microsoft Public License (Ms-PL). 
 * A copy of the license can be found in the license.htm file included 
 * in this distribution.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Text;

namespace ServiceStack.Razor.ServiceStack
{
	public class ModelSpan : CodeSpan
	{
		public ModelSpan(SourceLocation start, string content, string modelTypeName)
			: base(start, content)
		{
			this.ModelTypeName = modelTypeName;
		}

		internal ModelSpan(ParserContext context, string modelTypeName)
			: this(context.CurrentSpanStart, context.ContentBuffer.ToString(), modelTypeName)
		{
		}

		public string ModelTypeName
		{
			get;
			private set;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() ^ (ModelTypeName ?? String.Empty).GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var span = obj as ModelSpan;
			return span != null && Equals(span);
		}

		private bool Equals(ModelSpan span)
		{
			return base.Equals(span) && String.Equals(ModelTypeName, span.ModelTypeName, StringComparison.Ordinal);
		}
	}
}
