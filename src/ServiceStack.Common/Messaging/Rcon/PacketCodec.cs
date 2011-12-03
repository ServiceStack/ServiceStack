using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Messaging.Rcon
{
    /// <summary>
    /// Contains methods required for encoding and decoding rcon packets.
    /// </summary>
    internal class PacketCodec
    {
        /// <summary>
        /// Decodes a packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns>A packet object.</returns>
        internal static Packet DecodePacket(byte[] packet)
        {
            var header = DecodeHeader(packet);
            var words = DecodeWords(packet);

            bool fromServer = false;
            if (header[0] > 0)
                fromServer = true;
            bool isResponse = false;
            if (header[1] > 0)
                isResponse = true;
            uint idNumber = 0;
            if (header[2] > 0)
                idNumber = header[2];

            return new Packet()
            {
                FromServer = fromServer,
                IsResponse = isResponse,
                Sequence = idNumber,
                Words = words
            };
        }

        /// <summary>
        /// Decodes the packet header.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private static uint[] DecodeHeader(byte[] packet)
        {
            var x = BitConverter.ToUInt32(packet, 0);
            return new uint[] { x & 0x80000000, x & 0x40000000, x & 0x3FFFFFFF };
        }

        /// <summary>
        /// Decodes words in a packet.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private static byte[][] DecodeWords(byte[] packet)
        {
            var wordCount = BitConverter.ToUInt32(packet, 8);
            var words = new byte[wordCount][];
            var wordIndex = 0;
            int offset = 12;
            for (int i = 0; i < wordCount; i++)
            {
                var wordLen = BitConverter.ToInt32(packet, offset);
                var word = new byte[wordLen];
                for (int j = 0; j < wordLen; j++)
                {
                    word[j] = packet[offset + 4 + j];
                }
                words[wordIndex++] = word;
                offset += 5 + wordLen;
            }
            return words;
        }

        /// <summary>
        /// Encodes a packet for transmission to the server.
        /// </summary>
        /// <param name="fromServer"></param>
        /// <param name="isResponse"></param>
        /// <param name="id"></param>
        /// <param name="words"></param>
        /// <returns></returns>
        internal static byte[] EncodePacket(bool fromServer, bool isResponse, uint id, byte[][] words)
        {
            /*
             * Packet format:
             * 0 - 3 = header
             * 4 - 7 = size of packet
             * 8 -11 = number of words
             * 12+   = words
             * 
             * Word format:
             * 0 - 3 = word length
             * 4 - n = word
             * n+1   = null (0x0)
             */
            var encodedHeader = EncodeHeader(fromServer, isResponse, id);
            var encodedWordCount = BitConverter.GetBytes((uint)words.Length);
            var encodedWords = EncodeWords(words);
            var encodedPacketSize = BitConverter.GetBytes((uint)(encodedHeader.Length + encodedWordCount.Length + encodedWords.Length + 4));  //  +4 for the packet size indicator

            var packet = new List<byte>();
            packet.AddRange(encodedHeader);
            packet.AddRange(encodedPacketSize);
            packet.AddRange(encodedWordCount);
            packet.AddRange(encodedWords);
            return packet.ToArray();
        }

        /// <summary>
        /// Encodes a packet header.
        /// </summary>
        /// <param name="fromServer"></param>
        /// <param name="isResponse"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static byte[] EncodeHeader(bool fromServer, bool isResponse, uint id)
        {
            uint header = id & 0x3FFFFFFF;
            if (fromServer)
                header += 0x80000000;
            if (isResponse)
                header += 0x40000000;
            return BitConverter.GetBytes(header);
        }

        /// <summary>
        /// Encodes words.
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        private static byte[] EncodeWords(byte[][] words)
        {
            var wordPacket = new List<byte>();
            foreach (var word in words)
            {
                var encodedWord = new List<byte>();
                encodedWord.AddRange(word);
                encodedWord.Add(0);

                var encodedLength = BitConverter.GetBytes((uint)word.Length);

                wordPacket.AddRange(encodedLength);
                wordPacket.AddRange(encodedWord);
            }
            return wordPacket.ToArray();
        }
    }
}
