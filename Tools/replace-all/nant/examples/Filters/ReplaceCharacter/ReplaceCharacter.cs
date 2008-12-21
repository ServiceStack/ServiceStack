using System;
using System.IO;
using System.Globalization;

using NAnt.Core.Attributes;
using NAnt.Core.Filters;

namespace NAnt.Examples.Filters {
    /// <summary>
    /// Replaces a specific character in a file.
    /// </summary>
    /// <remarks>
    /// Replaces the character specified by <see cref="From" /> with the
    /// character specified by <see cref="To" />.
    /// </remarks>
    /// <example>
    ///   <para>Replace all "@" characters with "~".</para>
    ///   <code>
    ///     <![CDATA[
    ///       <replacecharacter from="@" to="~" />
    ///     ]]>
    ///   </code>
    /// </example>
    [ElementName("replacecharacter")]
    public class ReplaceCharacter : Filter {
        /// <summary>
        /// Delegate for Read and Peek. Allows the same implementation
        /// to be used for both methods.
        /// </summary>
        delegate int AcquireCharDelegate();

        #region Private Instance Fields

        private char _from;
        private char _to;

        //Methods used for Read and Peek
        private AcquireCharDelegate ReadChar = null;
        private AcquireCharDelegate PeekChar = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The character to replace.
        /// </summary>
        [TaskAttribute("from", Required=true)]
        public char From {
            get { return _from; }
            set { _from = value; }
        }

        /// <summary>
        /// The character to replace <see cref="From" /> with.
        /// </summary>
        [TaskAttribute("to", Required=true)]
        public char To {
            get { return _to; }
            set { _to = value; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Construct that allows this filter to be chained to the one
        /// in the parameter chainedReader.
        /// </summary>
        /// <param name="chainedReader">Filter that the filter will be chained to</param>
        public override void Chain(ChainableReader chainedReader) {
            base.Chain(chainedReader);
            ReadChar = new AcquireCharDelegate(base.Read);
            PeekChar = new AcquireCharDelegate(base.Peek);
        }

        /// <summary>
        /// Reads the next character applying the filter logic.
        /// </summary>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        public override int Read() {
            return GetNextCharacter(ReadChar);
        }

        /// <summary>
        /// Reads the next character applying the filter logic without
        /// advancing the current position in the stream.
        /// </summary>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        public override int Peek() {
            return GetNextCharacter(PeekChar);
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Returns the next character in the stream replacing the specified character. Using the
        /// <see cref="AcquireCharDelegate"/> allows for the same implementation for Read and Peek
        /// </summary>
        /// <param name="AcquireChar">Delegate to acquire the next character. (Read/Peek)</param>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        private int GetNextCharacter(AcquireCharDelegate AcquireChar) {
            int nextChar = AcquireChar();
            if (nextChar == From) {
                return To;
            }
            return nextChar;
        }

        #endregion Private Instance Methods
    }
}
