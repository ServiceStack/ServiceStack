using System;
using System.IO;
using System.IO.Compression;
using ServiceStack.Logging;

namespace ServiceStack.Messaging.Tests.UnitTests.Utils
{
    public class GZipUtils
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (GZipUtils));

        public static byte[] Compress(byte[] uncompressedBuffer)
        {
            try
            {
                if (uncompressedBuffer == null)
                {
                    throw new ArgumentNullException("uncompressedBuffer");
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true))
                    {
                        gzip.Write(uncompressedBuffer, 0, uncompressedBuffer.Length);
                    }
                    byte[] compressedBuffer = ms.ToArray();
                    log.DebugFormat("Compressed bytes size from {0} to {1} bytes", uncompressedBuffer.Length, compressedBuffer.Length);
                    return compressedBuffer;
                }
            }
            catch(Exception ex)
            {
                log.Error("Error during compression", ex);
                throw;
            }
        }

        public static byte[] Decompress(byte[] compressedBuffer)
        {
            try
            {
                if (compressedBuffer == null)
                {
                    throw new ArgumentNullException("compressedBuffer");
                }

                using (GZipStream gzip = new GZipStream(new MemoryStream(compressedBuffer), CompressionMode.Decompress))
                {
                    byte[] uncompressedBuffer = ReadAllBytes(gzip);
                    log.DebugFormat("Uncompressed bytes from {0} to {1} bytes", compressedBuffer.Length, uncompressedBuffer.Length);
                    return uncompressedBuffer;
                }
            }
            catch(Exception ex)
            {
                log.Error("Error during uncompression", ex);
                throw;
            }
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            byte[] buffer = new byte[4096];
            using (MemoryStream ms = new MemoryStream())
            {
                int bytesRead = 0;
                do
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        ms.Write(buffer, 0, bytesRead);
                    }
                } while (bytesRead > 0);

                return ms.ToArray();
            }
        }
    }
}