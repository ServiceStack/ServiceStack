using System;
using System.Text;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Method signature for the MessageReceived event
    /// </summary>
    public delegate void MessageReceivedHandler(object source, MessageEventArgs e);
}
