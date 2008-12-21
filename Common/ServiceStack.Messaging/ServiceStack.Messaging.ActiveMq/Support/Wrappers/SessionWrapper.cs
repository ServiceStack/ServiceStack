using System;
using System.Collections.Generic;
using System.Text;

namespace Bbc.Ww.Services.Messaging.ActiveMq.Support.Wrappers
{
    public class SessionWrapper : ISession
    {
        private readonly ActiveMqConnection activeMqConnection;
        private readonly NMS.ISession session;

        public SessionWrapper(ActiveMqConnection activeMqConnection, NMS.ISession session)
        {
            this.activeMqConnection = activeMqConnection;
            this.session = session;
        }

        public void Close()
        {
            session.Close();
        }

        public void Commit()
        {
            session.Commit();
        }

        public void Rollback()
        {
            session.Rollback();
        }

        public ITextMessage CreateTextMessage(string text)
        {
            throw new NotImplementedException();
        }

        public IOneWayClient CreateClient(IDestination destination)
        {
            throw new NotImplementedException();
        }

        public IGatewayListener CreateListener(IDestination destination)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            session.Close();
        }

        public ActiveMqConnection ActiveMqConnection
        {
            get { return activeMqConnection; }
        }

        public NMS.ISession NmsSession
        {
            get { return session; }
        }
    }
}