#if !SL5 
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using ServiceStack.Text;

namespace ServiceStack.Messaging.Rcon
{
    /// <summary>
    /// Base rcon class.
    /// </summary>
    public class Client
    {
        #region Delegates

        /// <summary>
        /// Event delegate when disconnected from the server.
        /// </summary>
        /// <param name="rcon"></param>
        public delegate void OnDisconnectedHandler(Client rcon);

        /// <summary>
        /// Delegate for async callbacks.
        /// </summary>
        /// <param name="rcon"></param>
        /// <param name="packet"></param>
        public delegate void AsyncCallback(Client rcon, byte[] response);

        #endregion

        #region Events

        /// <summary>
        /// Disconnected event.
        /// </summary>
        public event OnDisconnectedHandler OnDisconnected;

        #endregion

        #region Fields

        /// <summary>
        /// Rcon connection socket. Always set to null when not connected.
        /// </summary>
        Socket _sock = null;

        /// <summary>
        /// Unique ID for each message.
        /// </summary>
        uint _sequenceID = 1;

        /// <summary>
        /// Registered callbacks.
        /// </summary>
        Dictionary<uint, AsyncCallback> _registeredCallbacks = new Dictionary<uint, AsyncCallback>();

        #endregion

        #region Methods

        /// <summary>
        /// Create a new instance of rcon.
        /// </summary>
        /// <param name="rconEndpoint">Endpoint to connect to, usually the game server with query port.</param>
        public Client(IPEndPoint rconEndpoint)
        {
            Endpoint = rconEndpoint;
            Connected = false;
        }

        /// <summary>
        /// Attempts to connect to the game server for rcon operations.
        /// </summary>
        /// <returns>True if connection established, false otherwise.</returns>
        public virtual bool Connect()
        {
            if (Connected)
                Disconnect();

            Connected = false;
            _sequenceID = 1;

            try
            {
                _sock = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _sock.Connect(Endpoint);

                var readEventArgs = new SocketAsyncEventArgs();
                var state = new ClientSocketState();
                readEventArgs.UserToken = state;
                readEventArgs.SetBuffer(state.Header, 0, state.Header.Length);
                readEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(readEventArgs_Completed);

                if (!_sock.ReceiveAsync(readEventArgs))
                {
                    ProcessReceive(_sock, readEventArgs);
                }

                Connected = true;
                return true;
            }
            catch (Exception ex)
            {
                LastException = ex;
            }
            Disconnect();
            return false;
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
                        ProcessPacket(fullPacket, userToken);

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
                Disconnect();
            }
        }

        /// <summary>
        /// Processes a received packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        void ProcessPacket(byte[] packet, ClientSocketState userToken)
        {
            var packetObj = PacketCodec.DecodePacket(packet);
            if (_registeredCallbacks.ContainsKey(packetObj.Sequence))
            {
                var callback = _registeredCallbacks[packetObj.Sequence];
                _registeredCallbacks.Remove(packetObj.Sequence);
                if (packetObj.Words.Length < 3)
                {
                    callback(this, null);
                }
                else
                {
                    callback(this, packetObj.Words[2]);
                }
            }
        }

        /// <summary>
        /// Disconnects from rcon.
        /// </summary>
        public virtual void Disconnect()
        {
            Connected = false;
            _sequenceID = 1;

            if (_sock != null)
            {
                if (OnDisconnected != null)
                    OnDisconnected(this);

                //  these exceptions aren't really anything to worry about
                try
                {
                    _sock.Close();
                }
                catch (Exception) { }

                _sock = null;
            }
        }

        public void Call<T>(T request, AsyncCallback callback)
        {
            _registeredCallbacks[_sequenceID] = callback;
            IMessage<T> message = new Message<T>(request);
            InternalSend(new byte[][]{ Encoding.UTF8.GetBytes(request.GetType().AssemblyQualifiedName), message.ToBytes() });
        }

        /// <summary>
        /// Sends message to the server.
        /// </summary>
        /// <param name="words">Words to send.</param>
        protected virtual void InternalSend(byte[][] words)
        {
            if (!Connected)
            {
                LastException = new NotConnectedException();
                throw LastException;
            }

            var packet = PacketCodec.EncodePacket(false, false, _sequenceID++, words);
            try
            {
                var sendEventArgs = new SocketAsyncEventArgs();
                sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(sendEventArgs_Completed);
                sendEventArgs.SetBuffer(packet, 0, packet.Length);
                _sock.SendAsync(sendEventArgs);
            }
            catch (Exception ex)
            {
                Disconnect();
                LastException = ex;
                throw LastException;
            }
        }

        void sendEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend((Socket)sender, e);
        }

        void ProcessSend(Socket sock, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Disconnect();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Game server endpoint.
        /// </summary>
        public IPEndPoint Endpoint { get; protected set; }

        /// <summary>
        /// Last exception that occured during operation.
        /// </summary>
        public Exception LastException { get; protected set; }

        /// <summary>
        /// Connected?
        /// </summary>
        public bool Connected { get; protected set; }

        /// <summary>
        /// Gets the next unique ID to be used for transmisson. Read this before sending to pair responses to sent messages.
        /// </summary>
        public uint SequenceID { get { return _sequenceID; } }

        #endregion
    }

    /// <summary>
    /// Exception thrown when attempting to send on a non-connected service client.
    /// </summary>
    public class NotConnectedException : Exception
    {
    }

    public class ClientSocketState
    {
        public byte[] Header = new byte[8];
        public byte[] CompleteMessage = TypeConstants.EmptyByteArray;
        public bool ReadHeader = false;
        public uint MessageLength = 0;
    }
}
#endif