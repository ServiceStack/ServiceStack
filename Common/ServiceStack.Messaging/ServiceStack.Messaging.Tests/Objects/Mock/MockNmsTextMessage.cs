using System;
using System.Collections.Generic;
using System.Text;
using NMS;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsTextMessage : NMS.ITextMessage, NMS.IBytesMessage
    {
        private MockNmsPrimitiveMap properties;
        private string text;
        private byte[] content;
        private string nmsCorrelationID;
        private NMS.IDestination nmsDestination;
        private bool nmsPersistent;
        private TimeSpan nmsExpiration;
        private NMS.IDestination nmsReplyTo;
        private int nmsxDeliveryCount;
        private string nmsType;

        public MockNmsTextMessage()
        {
            properties = new MockNmsPrimitiveMap();
        }

        #region ITextMessage Members

        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
            }
        }

        #endregion

        #region IMessage Members

        public void Acknowledge()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string NMSCorrelationID
        {
            get
            {
                return nmsCorrelationID;
            }
            set
            {
                nmsCorrelationID = value;
            }
        }

        public NMS.IDestination NMSDestination
        {
            get { return nmsDestination; }
        }

        public TimeSpan NMSExpiration
        {
            get
            {
                return nmsExpiration;
            }
            set
            {
                nmsExpiration = value;
            }
        }

        public string NMSMessageId
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool NMSPersistent
        {
            get
            {
                return nmsPersistent;
            }
            set
            {
                nmsPersistent = value;
            }
        }

        public byte NMSPriority
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public bool NMSRedelivered
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public NMS.IDestination NMSReplyTo
        {
            get
            {
                return nmsReplyTo;
            }
            set
            {
                nmsReplyTo = value;
            }
        }

        public DateTime NMSTimestamp
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string NMSType
        {
            get { return nmsType; }
            set { nmsType = value; }
        }

        public IPrimitiveMap Properties
        {
            get { return properties; }
        }

        #endregion

        public int NMSXDeliveryCount
        {
            get
            {
                return nmsxDeliveryCount;
            }
            set
            {
                nmsxDeliveryCount = value;
            }
        }

        public byte[] Content
        {
            get { return content; }
            set { content = value; }
        }
    }
}
