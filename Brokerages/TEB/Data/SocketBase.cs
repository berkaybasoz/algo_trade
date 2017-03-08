using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.TEB.Data
{
    /// <summary> 
    /// Called when a message is received 
    /// </summary>
    public delegate void MessageHandler(SocketBase socket, int iNumberOfBytes);

    /// <summary> 
    /// Called when a connection is closed
    ///  </summary>
    public delegate void CloseHandler(SocketBase socket);

    /// <summary>
    ///  Called when a socket error occurs 
    ///  </summary>
    public delegate void ErrorHandler(SocketBase socket, Exception exception);

    /// <summary>
    /// Abstract class that defines base implementations
    /// </summary>
    public abstract class SocketBase : IDisposable
    {
        #region Variables
        /// <summary>
        /// A reference to a user defined object
        /// </summary>
        protected internal Object userArg;
        /// <summary>
        /// A reference to a user supplied function to be called when a socket message arrives 
        /// </summary>
        protected internal MessageHandler messageHandler;
        /// <summary>
        /// A reference to a user supplied function to be called when a socket connection is closed 
        /// </summary>
        protected internal CloseHandler closeHandler;
        /// <summary>
        /// A reference to a user supplied function to be called when a socket error occurs
        /// </summary>
        protected internal ErrorHandler errorHandler;
        /// <summary>
        /// Flag to indicate if the class has been disposed
        /// </summary>
        protected internal bool disposed;
        /// <summary>
        /// The IpAddress the client is connect to
        /// </summary>
        protected internal string ipAddress;
        /// <summary>
        /// The Port to either connect to or listen on
        /// </summary>
        protected internal int port;
        /// <summary>
        /// A raw buffer to capture data comming off the socket
        /// </summary>
        protected internal byte[] rawBuffer;
        /// <summary>
        /// Size of the raw buffer for received socket data
        /// </summary>
        protected internal int sizeOfRawBuffer;

        #endregion
        #region Public Properties

        // Public Properties
        /// <summary> 
        /// The IpAddress the client is connect to 
        /// </summary>
        public string IpAddress
        {
            get
            {
                return this.ipAddress;
            }
            set
            {
                this.ipAddress = value;
            }
        }

        /// <summary>
        /// The Port to either connect to or listen on
        /// </summary>
        public int Port
        {
            get
            {
                return this.port;
            }
            set
            {
                this.port = value;
            }
        }

        /// <summary>
        /// A reference to a user defined object
        /// </summary>
        public Object UserArg
        {
            get
            {
                return this.userArg;
            }
            set
            {
                this.userArg = value;
            }
        }

        /// <summary>
        /// A raw buffer to capture data comming off the socket
        /// </summary>
        public byte[] RawBuffer
        {
            get
            {
                return this.rawBuffer;
            }
            set
            {
                this.rawBuffer = value;
            }
        }

        /// <summary>
        /// Size of the raw buffer for received socket data
        /// </summary>
        public int SizeOfRawBuffer
        {
            get
            {
                return this.sizeOfRawBuffer;
            }
            set
            {
                this.sizeOfRawBuffer = value;
            }
        }


        #endregion Public Properties

        #region IDisposable Members

        public virtual void Dispose()
        {
        }

        #endregion
    }
}
