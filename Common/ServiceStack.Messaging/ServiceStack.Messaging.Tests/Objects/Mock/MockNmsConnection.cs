using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsConnection : NMS.IConnection
    {
        private ILog log;
        private readonly List<MockNmsSession> sessions;
        private readonly MockMessagingFactory factory;
        private string brokerUri;

        private int startedNo;
        private int stopNo;
        private int closedNo;
        private int disposedNo;
        private int createSessionNo;

        private NMS.AcknowledgementMode acknowledgementMode;
        private string clientId;

        public event EventHandler SessionCreated;

        public MockNmsConnection(MockMessagingFactory connectionFactory)
        {
            log = LogManager.GetLogger(GetType());
            this.factory = connectionFactory;
            sessions = new List<MockNmsSession>();
            this.startedNo = 0;
            this.stopNo = 0;
            this.closedNo = 0;
            this.disposedNo = 0;
            this.createSessionNo = 0;
        }

        public MockMessagingFactory Factory
        {
            get { return factory; }
        }

        public List<MockNmsSession> Sessions
        {
            get { return sessions; }
        }

        public int StartedNo
        {
            get { return startedNo; }
        }

        public int StopNo
        {
            get { return stopNo; }
        }

        public int ClosedNo
        {
            get { return closedNo; }
        }

        public int DisposedNo
        {
            get { return disposedNo; }
        }

        public int CreateSessionNo
        {
            get { return createSessionNo; }
        }

        public void TriggerConnectionException()
        {
            if (ExceptionListener != null)
            {
                ExceptionListener(new MockNmsConnectionException("Connection Exception"));
            }
        }

        protected void OnSessionCreated(EventArgs e)
        {
            if (SessionCreated != null)
            {
                SessionCreated(this, e);
            }
        }

        public string BrokerUri
        {
            get { return brokerUri; }
            set { brokerUri = value; }
        }

        #region IConnection Members

        public NMS.AcknowledgementMode AcknowledgementMode
        {
            get { return acknowledgementMode; }
            set { acknowledgementMode = value; }
        }

        public string ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }

        public void Close()
        {
            closedNo++;
            Dispose();
        }

        public virtual NMS.ISession CreateSession(NMS.AcknowledgementMode mode)
        {
            createSessionNo++;
            MockNmsSession mock = new MockNmsSession(this);
            mock.AcknowledgementMode = mode;
            sessions.Add(mock);
            return mock;
        }

        public NMS.ISession CreateSession()
        {
            return CreateSession(NMS.AcknowledgementMode.Transactional);
        }

        public event NMS.ExceptionListener ExceptionListener;

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            disposedNo++;
        }

        #endregion

        #region IStartable Members

        public void Start()
        {
            foreach (MockNmsSession session in sessions)
            {
                session.SendUnsentMessages();
            }
            startedNo++;
        }

        #endregion

        #region IStoppable Members

        public void Stop()
        {
            stopNo++;
        }

        #endregion
    }
}
