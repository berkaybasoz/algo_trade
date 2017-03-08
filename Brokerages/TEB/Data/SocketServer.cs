using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;


namespace QuantConnect.Brokerages.TEB.Data
{
    /// <summary> 
    /// This class abstracts a socket server 
    /// </summary>
    public class SocketServer : SocketBase
    {
        /// <summary> 
        /// Called when a socket connection is accepted 
        /// </summary>
        public delegate void AcceptHandler(SocketClient socket);
        /// <summary>
        /// A reference to a user supplied function to be called when a socket connection is accepted
        /// </summary>
        private AcceptHandler acceptHandler;
        /// <summary> 
        /// A TcpListener object to accept socket connections 
        /// </summary>
        private TcpListener tcpListener;
        /// <summary>
        /// A thread to process accepting socket connections
        /// </summary>
        private Thread acceptThread;
        /// <summary>
        /// An Array of SocketClient objects 
        /// </summary>
        private ArrayList socketClientList = new ArrayList();
        public ArrayList SocketClientList
        {
            get { return socketClientList; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SocketServer()
        {
            // Init the dispose flag
            this.disposed = false;
        }

        ///// <summary>
        ///// Finalizer
        ///// </summary>
        //~SocketServer()
        //{
        //    // If this object has not been disposed yet
        //    if (!this.disposed)
        //        Stop();
        //}

        /// <summary>
        /// Release objects
        /// </summary>
        public override void Dispose()
        {
            try
            {
                // Mark the object as disposed
                this.disposed = true;

                // Stop the server if the thread is running
                if (this.acceptThread != null)
                    Stop();
            }
            catch
            {
            }
            base.Dispose();
        }


        protected virtual SocketClient AcceptedSocketClient(SocketServer socketServer,
            Socket clientSocket, string ipAddress, int port,
            int sizeOfRawBuffer, object userArg, MessageHandler messageHandler,
            CloseHandler closeHandler, ErrorHandler errorHandler)
        {
            return new SocketClient(socketServer, clientSocket,
                ipAddress, port, sizeOfRawBuffer, userArg, messageHandler,
                closeHandler, errorHandler);
        }
        /// <summary>
        /// Function to process and accept socket connection requests
        /// </summary>
        private void AcceptThread()
        {
            Socket clientSocket = null;
            try
            {
                // Create a new TCPListner and start it up
                this.tcpListener = new TcpListener(Dns.GetHostEntry(this.IpAddress).AddressList[0], this.Port);
                //this.tcpListener = new TcpListener(Dns.Resolve(this.IpAddress).AddressList[0],this.Port);
                this.tcpListener.Start();
                for (; ; )
                {
                    // If a client connects, accept the connection
                    clientSocket = this.tcpListener.AcceptSocket();
                    if (clientSocket.Connected)
                    {
                        string Addr = clientSocket.RemoteEndPoint.ToString();
                        int index = Addr.IndexOf(':');
                        Addr = Addr.Substring(0, index);

                        // Create a SocketClient object
                        SocketClient socket = AcceptedSocketClient(this,
                            clientSocket,                                           // The socket object for the connection
                            Addr,                                                   // The IpAddress of the client
                            this.Port,                                                 // The port the client connected to
                            this.SizeOfRawBuffer,                                      // The size of the byte array for storing messages
                            this.UserArg,                                              // Application developer state
                            new MessageHandler(this.messageHandler),    // Application developer Message Handler
                            new CloseHandler(this.closeHandler),        // Application developer Close Handler
                            new ErrorHandler(this.errorHandler));       // Application developer Error Handler

                        socketClientList.Add(socket);
                        // Call the Accept Handler
                        this.acceptHandler(socket);
                    }
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                // Did we stop the TCPListener
                if (e.ErrorCode != 10004)
                {
                    // Call the error handler
                    this.errorHandler(null, e);
                    // Close the socket down if it exists
                    if (clientSocket != null)
                        if (clientSocket.Connected)
                            clientSocket.Close();
                }
            }
            catch (Exception e)
            {
                // Call the error handler
                this.errorHandler(null, e);
                // Close the socket down if it exists
                if (clientSocket != null)
                    if (clientSocket.Connected)
                        clientSocket.Close();
            }
        }

        public void RemoveSocket(SocketClient socketClient)
        {
            Monitor.Enter(socketClientList);
            try
            {
                foreach (SocketClient socket in socketClientList)
                {
                    if (socket == socketClient)
                    {
                        socketClientList.Remove(socketClient);
                        break;
                    }
                }
            }
            catch
            {
            }
            Monitor.Exit(socketClientList);
        }

        /// <summary> 
        /// Function to start the SocketServer 
        /// </summary>
        /// <param name="ipAddress"> The IpAddress to listening on </param>
        /// <param name="port"> The Port to listen on </param>
        /// <param name="sizeOfRawBuffer"> Size of the Raw Buffer </param>
        /// <param name="userArg"> User supplied arguments </param>
        /// <param name="messageHandler"> Function pointer to the user MessageHandler function </param>
        /// <param name="acceptHandler"> Function pointer to the user AcceptHandler function </param>
        /// <param name="closeHandler"> Function pointer to the user CloseHandler function </param>
        /// <param name="errorHandler"> Function pointer to the user ErrorHandler function </param>
        public void Start(string ipAddress, int port, int sizeOfRawBuffer, object userArg,
            MessageHandler messageHandler, AcceptHandler acceptHandler, CloseHandler closeHandler,
            ErrorHandler errorHandler)
        {
            // Is an AcceptThread currently running
            if (this.acceptThread == null)
            {
                // Set connection values
                this.IpAddress = ipAddress;
                this.Port = port;

                // Save the Handler Functions
                this.messageHandler = messageHandler;
                this.acceptHandler = acceptHandler;
                this.closeHandler = closeHandler;
                this.errorHandler = errorHandler;

                // Save the buffer size and user arguments
                this.SizeOfRawBuffer = sizeOfRawBuffer;
                this.UserArg = userArg;

                // Start the listening thread if one is currently not running
                ThreadStart tsThread = new ThreadStart(AcceptThread);
                this.acceptThread = new Thread(tsThread);
                this.acceptThread.Name = "Notification.Accept";
                this.acceptThread.Start();
            }
        }

        /// <summary> 
        /// Function to stop the SocketServer.  It can be restarted with Start 
        /// </summary>
        public void Stop()
        {
            // Abort the accept thread
            if (this.acceptThread != null)
            {
                this.tcpListener.Stop();
                this.acceptThread.Join();
                this.acceptThread = null;
            }

            // Dispose of all of the socket connections
            for (int iSocket = 0; iSocket < this.socketClientList.Count; ++iSocket)
            {
                SocketClient socket = (SocketClient)socketClientList[iSocket];
                socketClientList.Remove(socket);
                socket.Dispose();
            }

            // Wait for all of the socket client objects to be destroyed
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Clear the Handler Functions
            this.messageHandler = null;
            this.acceptHandler = null;
            this.closeHandler = null;
            this.errorHandler = null;

            // Clear the buffer size and user arguments
            this.sizeOfRawBuffer = 0;
            this.userArg = null;
        }


        /// <summary>
        /// The number of clients connected
        /// </summary>
        public int ConnectedClientCount
        {
            get
            {
                if (this.socketClientList == null)
                    return 0;

                return this.socketClientList.Count;
            }
        }

        /// <summary>
        /// Notifies connected clients of new alert from system
        /// </summary>
        /// <param name="data"></param>
        public int NotifyConnectedClients(string data)
        {
            int count = 0;
            ArrayList ObjectsToRemove = null;

            for (int x = 0; x < this.socketClientList.Count; x++)
            {
                try
                {
                    SocketClient socket = (SocketClient)this.socketClientList[x];

                    if (socket.ClientSocket.Connected == true &&
                        socket.SendNotification(data) == true)
                        count++;
                    else
                    {
                        if (ObjectsToRemove == null)
                            ObjectsToRemove = new ArrayList();

                        ObjectsToRemove.Add(socket);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:SocketServer: While in NotifyConnectedClients" +
                        e.Message);
                    //System.Diagnostics.Debugger.Break();
                }
            }

            if (ObjectsToRemove != null)
            {
                foreach (SocketClient socket in ObjectsToRemove)
                {
                    socket.Disconnect();
                    socketClientList.Remove(socket);
                    socket.Dispose();
                }
            }


            return count;

        }
    }
}
