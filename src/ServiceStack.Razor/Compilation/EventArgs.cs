using System;

namespace ServiceStack.Razor.Compilation
{
    public class GeneratorErrorEventArgs : EventArgs
    {
        public GeneratorErrorEventArgs(uint errorCode, string errorMessage, uint lineNumber, uint columnNumber)
        {
            ErorrCode = errorCode;
            ErrorMessage = errorMessage;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public uint ErorrCode { get; private set; }

        public string ErrorMessage { get; private set; }

        public uint LineNumber { get; private set; }

        public uint ColumnNumber { get; private set; }
    }

    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(uint completed, uint total)
        {
            Completed = completed;
            Total = total;
        }

        public uint Completed { get; private set; }

        public uint Total { get; private set; }
    }
}
