using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace EasyWeb
{
    public class EasySocket
    {
        public delegate void OnConnectDelegate(EasySocket socket);
        public delegate void OnReadDelegate(EasySocket socket, byte[] bytes, int length);
        public delegate void OnWriteDelegate(EasySocket socket);
        public delegate void OnCloseDelegate(EasySocket socket);
        public delegate void OnAcceptDelegate(EasySocket socket, EasySocket client);
        public delegate void OnTimeoutDelegate(EasySocket socket);

        public event OnConnectDelegate OnConnect;
        public event OnReadDelegate OnRead;
        public event OnWriteDelegate OnWrite;
        public event OnCloseDelegate OnClose;
        public event OnAcceptDelegate OnAccept;
        public event OnTimeoutDelegate OnTimeout;

        private Socket m_Socket;
        private byte[] m_ReadBuffer = new byte[4096];

        public EasySocket(Socket socket)
        {
            m_Socket = socket;
        }

        public void Write(byte[] bytes)
        {
            Write(bytes, 0, bytes.Length);
        }

        public void Write(byte[] bytes, int length)
        {
            Write(bytes, 0, length);
        }

        public void Write(byte[] bytes, int offset, int length)
        {
            m_Socket.BeginSend(bytes, offset, length, SocketFlags.None, SendEvent, this);
        }

        public void Write(string text)
        {
            Write(text, Encoding.UTF8);
        }

        public void Write(string text, Encoding encoding)
        {
            Write(encoding.GetBytes(text));
        }

        public void Close()
        {
            m_Socket.Shutdown(SocketShutdown.Both);
        }

        private void Connect(IPEndPoint local, IPEndPoint remote)
        {
            m_Socket.Bind(local);
            m_Socket.BeginConnect(remote, ConnectEvent, this);
        }

        public static EasySocket Listen(IPEndPoint local)
        {
            EasySocket socket = new EasySocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            socket.m_Socket.Bind(local);
            socket.m_Socket.Listen(5);
            socket.m_Socket.BeginAccept(AcceptEvent, socket);
            return socket;
        }

        private static void ConnectEvent(IAsyncResult rs)
        {
            EasySocket es = rs.AsyncState as EasySocket;

            // end the connection request
            es.m_Socket.EndConnect(rs);

            // check if the connection failed due to timeout or successfully connected
            if (!es.m_Socket.Connected)
            {
                IOLoop.Instance.PushEvent(() => { if (es.OnTimeout != null) es.OnTimeout(es); });
                return;
            }

            IOLoop.Instance.PushEvent(() => { if (es.OnConnect != null) es.OnConnect(es); });

            // set up event handlers
            es.m_Socket.BeginReceive(es.m_ReadBuffer, 0, es.m_ReadBuffer.Length, SocketFlags.None, ReceiveEvent, es);
        }

        private static void AcceptEvent(IAsyncResult rs)
        {
            EasySocket es = rs.AsyncState as EasySocket;

            // read in the new connection
            EasySocket client = new EasySocket(es.m_Socket.EndAccept(rs));

            // send the event
            IOLoop.Instance.PushEvent(() => { if (es.OnAccept != null) es.OnAccept(client); });

            // configure client for events
            client.m_Socket.BeginReceive(client.m_ReadBuffer, 0, client.m_ReadBuffer.Length, SocketFlags.None, ReceiveEvent, client);

            // continue listening for events
            es.m_Socket.BeginAccept(AcceptEvent, es);
        }

        private static void ReceiveEvent(IAsyncResult rs)
        {
            EasySocket es = rs.AsyncState as EasySocket;

            // read data from socket, and invoke either the OnClose or OnReceive callback as appropriate
            int read = es.m_Socket.EndReceive(rs);
            if (read <= 0)
            {
                IOLoop.Instance.PushEvent(() => { if (es.OnClose != null) es.OnClose(); });
            }
            else
            {
                IOLoop.Instance.PushEvent(() => { if (es.OnRead != null) es.OnRead(es.m_ReadBuffer, read); });

                // create a new buffer for reading, since the old one may not be processed for some time
                es.m_ReadBuffer = new byte[4096];

                // read another set of data, since the socket is not closed (yet)
                es.m_Socket.BeginReceive(es.m_ReadBuffer, 0, es.m_ReadBuffer.Length, SocketFlags.None, ReceiveEvent, es);
            }
        }

        private static void SendEvent(IAsyncResult rs)
        {
            EasySocket es = rs.AsyncState as EasySocket;

            // complete the operation; we don't do anything with it
            int bytes = es.m_Socket.EndSend(rs);

            // signal callback
            IOLoop.Instance.PushEvent(() => { if (es.OnWrite != null) es.OnWrite(); });
        }

        private void InvokeOnRead(byte[] bytes, int len)
        {
            if (OnRead != null)
                OnRead(this, bytes, len);
        }
    }
}
