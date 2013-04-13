using System;
using System.Web.Razor.Parser.SyntaxTree;

namespace ServiceStack.Razor2.Templating
{
	/// <summary>
    /// Defines an exception that occurs during parsing of a template.
    /// </summary>
    public class TemplateParsingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TemplateParsingException"/>
        /// </summary>
        internal TemplateParsingException(RazorError error)
            : base(error.Message)
        {
            Column = error.Location.CharacterIndex;
            Line = error.Location.LineIndex;
        }

        /// <summary>
        /// Gets the column the parsing error occurred.
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// Gets the line the parsing error occurred.
        /// </summary>
        public int Line { get; private set; }
    }
}