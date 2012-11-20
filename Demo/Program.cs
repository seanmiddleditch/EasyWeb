using EasyWeb;
using System.Net;
using System.Text;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            EasySocket listener = EasySocket.Listen(new IPEndPoint(IPAddress.Any, 8080));
            listener.OnAccept += OnAccept;

            IOLoop.Instance.Run();
        }

        static void OnAccept(EasySocket client)
        {
            System.Console.Out.WriteLine("CONNECTION!");

            client.OnRead += OnRead;
            client.OnClose += OnClose;

            client.Write(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\n\rContent-Length: 3\n\r\n\rHi!"));
            client.Close();
        }

        static void OnRead(byte[] bytes, int len)
        {
            if (len != 0)
                System.Console.Out.WriteLine(Encoding.UTF8.GetString(bytes, 0, len));
        }

        static void OnClose()
        {

        }
    }
}
