using System;
using System.IO;
using System.Text;

namespace ServiceStack.Text
{
    public class DirectStreamWriter : TextWriter
    {
        private const int optimizedBufferLength = 256;
        private const int maxBufferLength = 1024;
        
        private Stream stream;
        private StreamWriter writer = null;
        private byte[] curChar = new byte[1];
        private bool needFlush = false;

        private Encoding encoding;
        public override Encoding Encoding => encoding;

        public DirectStreamWriter(Stream stream, Encoding encoding)
        {
            this.stream = stream;
            this.encoding = encoding;
        }

        public override void Write(string s)
        {
            if (s.IsNullOrEmpty())
                return;

            if (s.Length <= optimizedBufferLength)
            {
                if (needFlush) 
                {
                    writer.Flush();
                    needFlush = false;
                }

                byte[] buffer = Encoding.GetBytes(s);
                stream.Write(buffer, 0, buffer.Length);
            } else 
            {
                if (writer == null)
                    writer = new StreamWriter(stream, Encoding, s.Length < maxBufferLength ? s.Length : maxBufferLength);
                
                writer.Write(s);
                needFlush = true;
            }
        }

        public override void Write(char c)
        {
            if ((int)c < 128)
            {
                if (needFlush)
                {
                    writer.Flush();
                    needFlush = false;
                }

                curChar[0] = (byte)c;
                stream.Write(curChar, 0, 1);
            } else
            {
                if (writer == null)
                    writer = new StreamWriter(stream, Encoding, optimizedBufferLength);
                
                writer.Write(c);
                needFlush = true;
            }
        }

        public override void Flush()
        {
            if (writer != null)
            {
                writer.Flush();
            }
            else
            {
                stream.Flush();
            }
        }
    }
}