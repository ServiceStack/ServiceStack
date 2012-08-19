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
using System.Web.Razor;
using System.Diagnostics;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using ServiceStack.MiniProfiler;

namespace ServiceStack.Razor.ServiceStack
{
	public class MvcWebPageRazorHost : RazorEngineHost
	{
		protected MvcWebPageRazorHost()
		{
			Init();
		}

		public MvcWebPageRazorHost(RazorCodeLanguage codeLanguage) 
			: base(codeLanguage)
		{
			Init();			
		}

		public MvcWebPageRazorHost(RazorCodeLanguage codeLanguage, Func<MarkupParser> markupParserFactory) 
			: base(codeLanguage, markupParserFactory)
		{
			Init();			
		}

		private void Init()
		{
			GetRidOfNamespace("System.Web.WebPages.Html");

			this.DefaultBaseClass = typeof(ViewPageRef).FullName;
			this.DefaultNamespace = "RazorOutput";
			this.DefaultClassName = "RazorView";

			this.GeneratedClassContext = new GeneratedClassContext(
				"Execute", "Write", "WriteLiteral", "WriteTo", "WriteLiteralTo", 
				typeof(HelperResult).FullName, "DefineSection");
		}

		public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
		{
			if (incomingCodeGenerator is Compilation.CSharp.CSharpRazorCodeGenerator)
			{
				return new Compilation.CSharp.CSharpRazorCodeGenerator(incomingCodeGenerator.ClassName,
					incomingCodeGenerator.RootNamespaceName,
					incomingCodeGenerator.SourceFileName,
					incomingCodeGenerator.Host);
			}
			return base.DecorateCodeGenerator(incomingCodeGenerator);
		}
		 
		public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
		{
		    if (incomingCodeParser is CSharpCodeParser)
			{
				return new MvcCSharpRazorCodeParser();
			}
		    return base.DecorateCodeParser(incomingCodeParser);
		}

	    private void GetRidOfNamespace(string ns)
		{
			if (NamespaceImports.Contains(ns))
			{
				NamespaceImports.Remove(ns);
			}
		}
	}

}
