using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.Messaging.ActiveMq.Support.Wrappers;

namespace ServiceStack.Messaging.ActiveMq.Support.Utils
{
    public class MessageUtils
    {
        private const int MAX_TEXT_LENGTH = 1024*1024*10; //10mb

        /// <summary>
        /// Creates the NMS message.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static NMS.IMessage CreateNmsMessage(NMS.ISession session, string text)
        {
            NMS.IMessage nmsMessage = null;

            //If the text is too large a compressed binary (byte) message is sent to the broker instead.
            if (text.Length > MAX_TEXT_LENGTH)
            {
                byte[] uncompressedBuffer = new UTF8Encoding().GetBytes(text);
                byte[] compressedBuffer = SharpZipUtils.Compress(uncompressedBuffer);
                nmsMessage = session.CreateBytesMessage(compressedBuffer);
                nmsMessage.NMSType = ActiveMqMessageType.Bytes.ToString();
            }
            else
            {
                nmsMessage = session.CreateTextMessage(text);
                nmsMessage.NMSType = ActiveMqMessageType.Text.ToString();
            }

            return nmsMessage;
        }

        public static string GetText(NMS.IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            NMS.ITextMessage textMessage = message as NMS.ITextMessage;
            if (textMessage != null)
            {
                return textMessage.Text;
            }
            
            NMS.IBytesMessage bytesMessage = message as NMS.IBytesMessage;
            if (bytesMessage != null)
            {
                byte[] uncompressedBuffer = SharpZipUtils.Decompress(bytesMessage.Content);
                string text = new UTF8Encoding().GetString(uncompressedBuffer);
                return text;
            }

            throw new NotSupportedException(string.Format("Cannot GetText from unsupported type: {0}", message.GetType().Name));
        }

    }
}
