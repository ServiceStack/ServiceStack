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
using ServiceStack.Html;
using System.Globalization;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Text;

namespace ServiceStack.Razor.ServiceStack
{
	public class MvcCSharpRazorCodeParser : CSharpCodeParser
	{
		private const string ModelKeyword = "model";
		private SourceLocation? _endInheritsLocation;
		private bool _modelStatementFound;

		public MvcCSharpRazorCodeParser()
		{
			RazorKeywords.Add(ModelKeyword, WrapSimpleBlockParser(BlockType.Directive, ParseModelStatement));
		}

		protected override bool ParseInheritsStatement(CodeBlockInfo block)
		{
			_endInheritsLocation = CurrentLocation;
			bool result = base.ParseInheritsStatement(block);
			CheckForInheritsAndModelStatements();
			return result;
		}

		private void CheckForInheritsAndModelStatements()
		{
			if (_modelStatementFound && _endInheritsLocation.HasValue)
			{
				OnError(_endInheritsLocation.Value, String.Format(CultureInfo.CurrentCulture, MvcResources.MvcRazorCodeParser_CannotHaveModelAndInheritsKeyword, ModelKeyword));
			}
		}

		private bool ParseModelStatement(CodeBlockInfo block)
		{
			SourceLocation endModelLocation = CurrentLocation;

			bool readWhitespace = RequireSingleWhiteSpace();
			End(MetaCodeSpan.Create(Context, hidden: false, acceptedCharacters: readWhitespace ? AcceptedCharacters.None : AcceptedCharacters.Any));

			if (_modelStatementFound)
			{
				OnError(endModelLocation, String.Format(CultureInfo.CurrentCulture, MvcResources.MvcRazorCodeParser_OnlyOneModelStatementIsAllowed, ModelKeyword));
			}

			_modelStatementFound = true;

			// Accept Whitespace up to the new line or non-whitespace character
			Context.AcceptWhiteSpace(includeNewLines: false);

			string typeName = null;
			if (ParserHelpers.IsIdentifierStart(CurrentCharacter))
			{
				using (Context.StartTemporaryBuffer())
				{
					Context.AcceptUntil(ParserHelpers.IsNewLine);
					typeName = Context.ContentBuffer.ToString();
					Context.AcceptTemporaryBuffer();
				}
				Context.AcceptNewLine();
			}
			else
			{
				OnError(endModelLocation, String.Format(CultureInfo.CurrentCulture, MvcResources.MvcRazorCodeParser_ModelKeywordMustBeFollowedByTypeName, ModelKeyword));
			}
			CheckForInheritsAndModelStatements();
			End(new ModelSpan(Context, typeName));
			return false;
		}
	}
}
