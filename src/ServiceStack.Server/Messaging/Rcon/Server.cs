#if !SL5
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Text;

namespace ServiceStack.Messaging.Rcon
{
    /// <summary>
    /// Hosting services via a binary-safe TCP-based protocol.
    /// </summary>
    public class Server : IMessageService
    {
        private readonly Dictionary<Type, IMessageHandlerFactory> handlerMap
            = new Dictionary<Type, IMessageHandlerFactory>();
        private Dictionary<Type, IMessageHandler> messageHandlers = new Dictionary<Type,IMessageHandler>();

        Socket _listener = null;
        IPEndPoint _localEndpoint = null;

        public Server(IPEndPoint localEndpoint)
        {
            _localEndpoint = localEndpoint;
        }

        #region IMessageService Members

        /// <summary>
        /// Factory to create consumers and producers that work with this service
        /// </summary>
        public IMessageFactory MessageFactory { get; private set; }

        /// <summary>
        /// Register DTOs and hanlders the MQ Host will process
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="processMessageFn"></param>
        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn)
        {
            RegisterHandler(processMessageFn, null);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
        {
            if (handlerMap.ContainsKey(typeof(T)))
            {
                throw new ArgumentException("Message handler has already been registered for type: " + typeof(T).Name);
            }

            handlerMap[typeof(T)] = CreateMessageHandlerFactory(processMessageFn, processExceptionEx);
        }

        protected IMessageHandlerFactory CreateMessageHandlerFactory<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
        {
            return new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx);
        }

        public IMessageHandlerStats GetStats()
        {
            return null;
        }

        public List<Type> RegisteredTypes
        {
            get { return messageHandlers.Keys.ToList(); }
        }

        /// <summary>
        /// Get Total Current Stats for all Message Handlers
        /// </summary>
        /// <returns></returns>
        public string GetStatus()
        {
            return null;
        }

        /// <summary>
        /// Get a Stats dump
        /// </summary>
        /// <returns></returns>
        public string GetStatsDescription()
        {
            return null;
        }

        /// <summary>
        /// Start the MQ Host. Stops the server and restarts if already started.
        /// </summary>
        public void Start()
        {
            if (this.messageHandlers.Count == 0)
            {
                foreach (var kvp in this.handlerMap)
                {
                    this.messageHandlers[kvp.Key] = kvp.Value.CreateMessageHandler();
                }
            }

            Stop();

            _listener = new Socket(_localEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(_localEndpoint);
            _listener.Listen(60);
            var acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(acceptArgs_Completed);
            if (!_listener.AcceptAsync(acceptArgs))
            {
                ProcessAccept(_listener, acceptArgs);
            }
        }

        /// <summary>
        /// Stop the MQ Host if not already stopped. 
        /// </summary>
        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Close();
                _listener = null;
            }
        }

        #endregion

        public void Dispose()
        {
            if (_listener != null)
            {
                try
                {
                    _listener.Shutdown(SocketShutdown.Send);
                    _listener.Close();
                }
                catch (Exception) { }
            }
        }

        public void Notify(string queueName, byte[] message, Socket client, uint sequenceID)
        {
            var words = new byte[][] { Encoding.UTF8.GetBytes("notify"), Encoding.UTF8.GetBytes(queueName), message };
            var sendToClient = PacketCodec.EncodePacket(false, true, sequenceID, words);
            Send(sendToClient, client);
        }

        public void Publish(string queueName, byte[] message, Socket client, uint sequenceID)
        {
            var words = new byte[][] { Encoding.UTF8.GetBytes("publish"), Encoding.UTF8.GetBytes(queueName), message };
            var sendToClient = PacketCodec.EncodePacket(false, true, sequenceID, words);
            Send(sendToClient, client);
        }

        void acceptArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept((Socket)sender, e);
        }

        void ProcessAccept(Socket serverSock, SocketAsyncEventArgs e)
        {
            var newSocket = e.AcceptSocket;
            var readEventArgs = new SocketAsyncEventArgs();
            var state = new ClientSocketState();
            readEventArgs.UserToken = state;
            readEventArgs.SetBuffer(state.Header, 0, state.Header.Length);
            readEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(readEventArgs_Completed);

            if (!newSocket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(newSocket, readEventArgs);
            }

            e.AcceptSocket = null;
            serverSock.AcceptAsync(e);
        }

        void readEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive((Socket)sender, e);
        }

        void ProcessReceive(Socket readingSock, SocketAsyncEventArgs e)
        {
            var userToken = (ClientSocketState)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                if (!userToken.ReadHeader)
                {
                    //  if we've filled the buffer we can decode the header
                    if (e.Offset + e.BytesTransferred == userToken.Header.Length)
                    {
                        userToken.ReadHeader = true;
                        userToken.MessageLength = BitConverter.ToUInt32(userToken.Header, 4);
                        userToken.CompleteMessage = new byte[userToken.MessageLength];
                        for (int i = 0; i < userToken.Header.Length; i++)
                        {
                            userToken.CompleteMessage[i] = userToken.Header[i];
                        }
                        e.SetBuffer(userToken.CompleteMessage, userToken.Header.Length, userToken.CompleteMessage.Length - userToken.Header.Length);

                        if (!readingSock.ReceiveAsync(e))
                        {
                            ProcessReceive(readingSock, e);
                        }
                    }
                    else
                    {
                        if (!readingSock.ReceiveAsync(e))
                        {
                            ProcessReceive(readingSock, e);
                        }
                    }
                }
                else
                {
                    if (e.Offset + e.BytesTransferred == userToken.MessageLength)
                    {
                        //  copy buffer
                        var fullPacket = userToken.CompleteMessage;

                        //  reset state
                        userToken.ReadHeader = false;
                        userToken.MessageLength = 0;

                        //  process the message
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            ProcessPacket(fullPacket, readingSock, userToken);
                        });

                        //  start listening for more packets
                        e.SetBuffer(userToken.Header, 0, userToken.Header.Length);
                        if (!readingSock.ReceiveAsync(e))
                        {
                            ProcessReceive(readingSock, e);
                        }
                    }
                    else
                    {
                        if (!readingSock.ReceiveAsync(e))
                        {
                            ProcessReceive(readingSock, e);
                        }
                    }
                }
            }
            else
            {
                //  socket disconnected
                ClientDisconnected(readingSock);
            }
        }

        /// <summary>
        /// Processes a received packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        void ProcessPacket(byte[] packet, Socket client, ClientSocketState userToken)
        {
            var packetObj = PacketCodec.DecodePacket(packet);
#if !SL5 
            var type = Type.GetType(Encoding.UTF8.GetString(packetObj.Words[0]));
#else
            var bytes = packetObj.Words[0];
            var type = Type.GetType(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
#endif

            if (messageHandlers.ContainsKey(type))
            {
                messageHandlers[type].Process(new ProcessingClient(packetObj, client, this));
            }
        }

        void Send(byte[] data, Socket client)
        {
            var sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(sendEventArgs_Completed);
            sendEventArgs.SetBuffer(data, 0, data.Length);
            client.SendAsync(sendEventArgs);
        }

        void sendEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend((Socket)sender, e);
        }

        void ProcessSend(Socket sock, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                ClientDisconnected(sock);
            }
        }

        void ClientDisconnected(Socket sock)
        {
            if (sock != null)
            {
                try
                {
                    sock.Shutdown(SocketShutdown.Send);
                }
                catch (Exception) { }

                try
                {
                    sock.Close();
                }
                catch (Exception) { }
            }
        }
    }
}

#endif