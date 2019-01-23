using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
namespace Server
{
    class Listener
    {
        Socket sck;
        public delegate void RunTimeErrorHandler(Exception ex);
        public event RunTimeErrorHandler RunTimeErrorEvent;
        public delegate void AcceptedHandler(Socket s);
        public event AcceptedHandler  AcceptedEvent;
        public bool Listening { get; private set; }
        public int Port { get; private set; }
        public Listener(int port)
        {
            Port = port;
            Listening = false;
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        public void Start()
        {
            if (Listening) return;
            sck.Bind(new IPEndPoint(0,Port));
            sck.Listen(0);
            sck.BeginAccept(AcceptCallBack, 0);

            Listening = true;
        }
        public void Stop()
        {
            if (!Listening) return;
            sck.Close();
            sck.Dispose();
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listening = false; 

        }
        void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                Socket Accepted = sck.EndAccept(ar);
                if (AcceptedEvent != null) AcceptedEvent(Accepted);


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (RunTimeErrorEvent != null) RunTimeErrorEvent(ex);
            }
            sck.BeginAccept(AcceptCallBack, 0);

        }
    }
}
