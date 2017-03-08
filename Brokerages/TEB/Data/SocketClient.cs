using System;
using System.Net;
using System.Collections;
using System.Net.Sockets;
using System.Threading; 

namespace QuantConnect.Brokerages.TEB.Data
{
    /// <summary> 
    /// This class abstracts a socket 
    /// </summary>
    public class SocketClient : SocketBase
    {
        private Queue messageQueue = new Queue();
        private byte[] theRest;
        private bool m_Connected = false;

        #region Private Properties
        /// <summary>
        /// A network stream object 
        /// </summary>
        private NetworkStream networkStream;
        /// <summary>
        /// A TcpClient object for socket connection 
        /// </summary>
        private TcpClient tcpClient;
        /// <summary>
        /// A callback object for processing recieved socket data 
        /// </summary>
        private AsyncCallback callbackReadMethod;
        /// <summary>
        /// A callback object for processing send socket data
        /// </summary>
        private AsyncCallback callbackWriteMethod;
        /// <summary>
        /// Size of the receive buffer. Defaults to 1048576
        /// </summary>
        private int receiveBufferSize = 1048576;
        /// <summary>
        /// Size of the send buffer. Defaults to 1048576
        /// </summary>
        private int sendBufferSize = 1048576;
        /// <summary>
        /// The SocketServer for this socket object
        /// </summary>
        private SocketServer socketServer;

        public SocketServer SocketServer
        {
            get { return socketServer; }
        }

        /// <summary>
        /// The socket for the client connection 
        /// </summary>
        private Socket clientSocket;

        #endregion Private Properties

        #region Constructor & Destructor

        /// <summary> 
        /// Constructor for client support
        /// </summary>
        /// <param name="sizeOfRawBuffer"> The size of the raw buffer </param>
        /// <param name="userArg"> A Reference to the Users arguments </param>
        /// <param name="messageHandler">  Reference to the user defined message handler method </param>
        /// <param name="closeHandler">  Reference to the user defined close handler method </param>
        /// <param name="errorHandler">  Reference to the user defined error handler method </param>
        public SocketClient(int sizeOfRawBuffer, object userArg,
            MessageHandler messageHandler, CloseHandler closeHandler,
            ErrorHandler errorHandler)
        {
            // Create the raw buffer
            this.SizeOfRawBuffer = sizeOfRawBuffer;
            this.RawBuffer = new Byte[this.SizeOfRawBuffer];

            // Save the user argument
            this.userArg = userArg;

            // Set the handler methods
            this.messageHandler = messageHandler;
            this.closeHandler = closeHandler;
            this.errorHandler = errorHandler;

            // Set the async socket method handlers
            this.callbackReadMethod = new AsyncCallback(ReceiveComplete);
            this.callbackWriteMethod = new AsyncCallback(SendComplete);

            this.m_Connected = true;
            // Init the dispose flag
            this.disposed = false;
        }

        /// <summary> Constructor for SocketServer Suppport </summary>
        /// <param name="socketServer"> A Reference to the parent SocketServer </param>
        /// <param name="clientSocket"> The Socket object we are encapsulating </param>
        /// <param name="socketListArray"> The index of the SocketServer Socket List Array </param>
        /// <param name="ipAddress"> The IpAddress of the remote server </param>
        /// <param name="port"> The Port of the remote server </param>
        /// <param name="messageHandler"> Reference to the user defined message handler function </param>
        /// <param name="closeHandler"> Reference to the user defined close handler function </param>
        /// <param name="errorHandler"> Reference to the user defined error handler function </param>
        /// <param name="sizeOfRawBuffer"> The size of the raw buffer </param>
        /// <param name="userArg"> A Reference to the Users arguments </param>
        public SocketClient(SocketServer socketServer, Socket clientSocket,
            string ipAddress, int port, int sizeOfRawBuffer,
            object userArg, MessageHandler messageHandler, CloseHandler closeHandler,
            ErrorHandler errorHandler)
            : this(sizeOfRawBuffer, userArg, messageHandler, closeHandler, errorHandler)
        {

            // Set reference to SocketServer
            this.socketServer = socketServer;

            // Init the socket references
            this.clientSocket = clientSocket;

            // Set the Ipaddress and Port
            this.ipAddress = ipAddress;
            this.port = port;

            // Init the NetworkStream reference
            this.networkStream = new NetworkStream(this.clientSocket);

            // Set these socket options
            this.clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
                System.Net.Sockets.SocketOptionName.ReceiveBuffer, this.receiveBufferSize);
            this.clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
                System.Net.Sockets.SocketOptionName.SendBuffer, this.sendBufferSize);
            this.clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
                System.Net.Sockets.SocketOptionName.DontLinger, 1);
            this.clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Tcp,
                System.Net.Sockets.SocketOptionName.NoDelay, 1);

            // Wait for a message
            Receive();
        }

        /// <summary> 
        /// Overloaded constructor for client support
        /// </summary>
        /// <param name="sendBufferSize"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="sizeOfRawBuffer"> The size of the raw buffer </param>
        /// <param name="userArg"> A Reference to the Users arguments </param>
        /// <param name="messageHandler">  Reference to the user defined message handler method </param>
        /// <param name="closeHandler">  Reference to the user defined close handler method </param>
        /// <param name="errorHandler">  Reference to the user defined error handler method </param>
        public SocketClient(int sendBufferSize, int receiveBufferSize,
            int sizeOfRawBuffer,
            object userArg,
            MessageHandler messageHandler,
            CloseHandler closeHandler,
            ErrorHandler errorHandler
            )
            : this(sizeOfRawBuffer, userArg, messageHandler, closeHandler, errorHandler)
        {
            //Set the size of the send/receive buffers
            this.sendBufferSize = sendBufferSize;
            this.receiveBufferSize = receiveBufferSize;
        }

        ///// <summary> 
        ///// Finialize 
        ///// </summary>

        //~SocketClient()
        //{
        //    if (!this.disposed)
        //        Dispose();
        //}

        /// <summary>
        /// Disposes of internal objects
        /// </summary>
        public override void Dispose()
        {
            try
            {
                // Flag that dispose has been called
                this.disposed = true;
                // Disconnect the client from the server
                Disconnect();
            }
            catch
            {
            }
            // Remove the socket from the list
            if (this.socketServer != null)
                this.socketServer.RemoveSocket(this);

            base.Dispose();
        }


        #endregion Constructor & Destructor

        private void SaveTheRestofTheStream(byte[] src, int ptr, int TotalLen)
        {
            theRest = new byte[TotalLen - ptr];
            for (int i = 0; ptr < TotalLen; i++)
                theRest[i] = RawBuffer[ptr++];

            return;
        }
        private void ParseMessage(int TotalLength)
        {
            int ptr = 0;

            try
            {
                while (ptr < TotalLength)
                {
                    if ((TotalLength - ptr) <= 6)
                    {
                        SaveTheRestofTheStream(RawBuffer, ptr, TotalLength);
                        return;
                    }

                    int iLen = TotalLength;
                    byte[] msg;

                    if (iLen > (TotalLength - ptr))
                    {
                        SaveTheRestofTheStream(RawBuffer, ptr, TotalLength);
                        return;
                    }

                    msg = new byte[iLen];
                    for (int i = 0; i < iLen; i++)
                        msg[i] = RawBuffer[ptr++];

                    messageQueue.Enqueue(msg);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:SocketClient: Got Exception while ParseMessage. Message: " + e.Message);
            }
        }

        /// <summary> 
        /// Called when a message arrives
        ///  </summary>
        /// <param name="ar"> An async result interface </param>
        private void ReceiveComplete(IAsyncResult ar)
        {
            try
            {
                // Is the Network Stream object valid
                if (this.networkStream.CanRead)
                {
                    // Read the current bytes from the stream buffer
                    int bytesRecieved = this.networkStream.EndRead(ar);
                    // If there are bytes to process else the connection is lost
                    if (bytesRecieved > 0)
                    {
                        try
                        {
                            if (theRest != null)
                            {
                                int i;
                                byte[] tmp = new byte[bytesRecieved + theRest.Length];

                                for (i = 0; i < theRest.Length; i++)
                                    tmp[i] = theRest[i];

                                for (int j = 0; j < bytesRecieved; j++)
                                    tmp[i++] = RawBuffer[j];

                                RawBuffer = tmp;

                                bytesRecieved = bytesRecieved + theRest.Length;

                                theRest = null;
                            }

                            ParseMessage(bytesRecieved);

                            if (messageQueue.Count > 0)
                            {
                                RawBuffer = (byte[])messageQueue.Dequeue();
                                // A message came in send it to the MessageHandler
                                this.messageHandler(this, RawBuffer.Length);
                            }
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                        // Wait for a new message

                        if (this.m_Connected == true)
                            Receive();
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:SocketClient: Got Exception while ReceiveComplite. Message: " + e.Message);

                try
                {
                    //System.Diagnostics.Debugger.Break();

                    // The connection must have dropped call the CloseHandler
                    this.closeHandler(this);
                }
                catch
                {
                }
            }
        }

        /// <summary> 
        /// Called when a message is sent
        ///  </summary>
        /// <param name="ar"> An async result interface </param>
        private void SendComplete(IAsyncResult ar)
        {
            try
            {
                // Is the Network Stream object valid
                if (this.networkStream.CanWrite)
                    this.networkStream.EndWrite(ar);
            }
            catch (Exception)
            {
                Console.WriteLine("Error:SocketClient: Got Exception while SendComplete");
            }
        }

        /// <summary> 
        /// The socket for the client connection 
        /// </summary>
        public Socket ClientSocket
        {
            get
            {
                return this.clientSocket;
            }
            set
            {
                this.clientSocket = value;
            }
        }

        /// <summary> 
        /// Function used to connect to a server 
        /// </summary>
        /// <param name="strIpAddress"> The address to connect to </param>
        /// <param name="iPort"> The Port to connect to </param>
        public void Connect(String ipAddress, int port)
        {
            try
            {
                if (this.networkStream == null)
                {
                    // Set the Ipaddress and Port
                    this.IpAddress = ipAddress;
                    this.Port = port;

                    // Attempt to establish a connection
                    this.tcpClient = new TcpClient(this.IpAddress, this.Port);
                    this.networkStream = this.tcpClient.GetStream();

                    // Set these socket options
                    this.tcpClient.ReceiveBufferSize = this.receiveBufferSize;
                    this.tcpClient.SendBufferSize = this.sendBufferSize;
                    this.tcpClient.NoDelay = true;
                    this.tcpClient.LingerState = new System.Net.Sockets.LingerOption(false, 0);

                    this.m_Connected = true;
                    // Start to receive messages
                    Receive();
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine("Error:SocketClient: Got Exception while Connect:" + e.Message);
                throw new Exception(e.Message, e.InnerException);
            }
        }

        /// <summary> 
        /// Function used to disconnect from the server 
        /// </summary>
        public virtual void Disconnect()
        {
            if (m_Connected == true)
            {
                // Close down the connection
                if (this.networkStream != null)
                    this.networkStream.Close();
                if (this.tcpClient != null)
                    this.tcpClient.Close();
                if (this.clientSocket != null)
                    this.clientSocket.Close();

                // Clean up the connection state
                this.networkStream = null;
                this.tcpClient = null;
                this.clientSocket = null;

                this.m_Connected = false;
            }
        }

        /// <summary>
        ///  Function to send a string to the server 
        ///  </summary>
        /// <param name="message"> A string to send </param>
        public bool Send(string message)
        {
            Byte[] rawBuffer = System.Text.Encoding.GetEncoding(1254).GetBytes(message);
            return this.Send(rawBuffer);
        }

        /// <summary>
        ///  Function to send a string to the server 
        ///  </summary>
        /// <param name="message"> A string to send </param>
        virtual public bool SendNotification(string message)
        {
            return this.Send(message);
        }

        /// <summary> 
        /// Function to send a raw buffer to the server 
        /// </summary>
        /// <param name="rawBuffer"> A Raw buffer of bytes to send </param>
        public bool Send(Byte[] rawBuffer)
        {
            if ((this.networkStream != null) && (this.networkStream.CanWrite))
            //&& 
            //(clientSocket != null) && (this.clientSocket.Connected == true))
            {
                // Issue an asynchronus write
                this.networkStream.BeginWrite(rawBuffer, 0, rawBuffer.GetLength(0), this.callbackWriteMethod, null);
                return true;
            }
            else
                return false;
        }

        /// <summary> 
        /// Wait for a message to arrive
        /// </summary>
        public void Receive()
        {
            while (messageQueue.Count != 0)
            {
                RawBuffer = (byte[])messageQueue.Dequeue();

                this.messageHandler(this, RawBuffer.Length);
            }

            if ((this.networkStream != null) && (this.networkStream.CanRead))
            {
                this.RawBuffer = new Byte[this.SizeOfRawBuffer];
                // Issue an asynchronous read
                this.networkStream.BeginRead(this.RawBuffer, 0, this.SizeOfRawBuffer, this.callbackReadMethod, null);
            }
            else
                throw new Exception("Socket Closed");
        }

    }
}
