using System;
using System.IO;
using ServiceStack.DataAnnotations;
using ServiceStack.Razor2.Compilation;

namespace ServiceStack.Razor2.Templating
{
	/// <summary>
    /// Defines a template writer used for helper templates.
    /// </summary>
    [References(typeof(CompilerServiceBase))] //referenced in CompilerServiceBase
    public class TemplateWriter
    {
        private readonly Action<TextWriter> writerDelegate;

        /// <summary>
        /// Initializes a new instance of <see cref="TemplateWriter"/>.
        /// </summary>
        /// <param name="writer">The writer delegate used to write using the specified <see cref="TextWriter"/>.</param>
        public TemplateWriter(Action<TextWriter> writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            writerDelegate = writer;
        }

        /// <summary>
        /// Executes the write delegate and returns the result of this <see cref="TemplateWriter"/>.
        /// </summary>
        /// <returns>The string result of the helper template.</returns>
        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                writerDelegate(writer);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Writes the helper result of the specified text writer.
        /// </summary>
        /// <param name="writer">The text writer to write the helper result to.</param>
        public void WriteTo(TextWriter writer)
        {
            writerDelegate(writer);
        }
    }
}