using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Server
{
    // phan luong, mo cong
    public class TcpServer
    {
        private TcpListener _listener;
        private int _port;

        public TcpServer (string ipAddress, int port)
        {
            _port = port;
            IPAddress ip = IPAddress.Parse(ipAddress);
            _listener = new TcpListener(ip, port);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine($"🚀 TCP Server dang lang nghe tai Port: {_port}...");
            while (true)
            {
                // cho client ket noi den 
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"\n>>> Co ket noi moi tu: {client.Client.RemoteEndPoint}");

                ClientHandler clientHandler = new ClientHandler(client);

                _ = Task.Run(() =>
                clientHandler.RunAsync() );
            }
        }
    }
}
