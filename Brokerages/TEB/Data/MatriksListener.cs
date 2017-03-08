using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class MatriksListener
    {
        private const string InitialMatriksMsg = "Tip:3;";
        private SocketClient matriksPriceSocket;
        private string ip;
        private int port;
        private int readSizeOfRawBuffer = 512;
        private MessageHandler messageHandler;

        public MatriksListener(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public int ReadSizeOfRawBuffer
        {
            get { return readSizeOfRawBuffer; }
            set { readSizeOfRawBuffer = value; }
        }

        public SocketClient MatriksPriceSocket
        {
            get { return matriksPriceSocket; }
            set { matriksPriceSocket = value; }
        }

        public MessageHandler MessageHandler
        {
            get { return messageHandler; }
            set { messageHandler = value; }
        }

        public void StartMatriksPriceSocket(MessageHandler messageHandler)
        {
            MessageHandler = messageHandler;

            MatriksPriceSocket = new SocketClient(ReadSizeOfRawBuffer, null, new  MessageHandler(MessageHandler), null, null);

            MatriksPriceSocket.Connect(ip, port);

            MatriksPriceSocket.Send(InitialMatriksMsg + Environment.NewLine);

        }

        public void StopMatriksPriceSocket()
        {
            if (MatriksPriceSocket != null)
            {
                MatriksPriceSocket.Disconnect();
                MatriksPriceSocket.Dispose();
                MatriksPriceSocket = null;
            }
        }
    }
}
