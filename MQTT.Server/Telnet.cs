using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MQTT.Server
{
    public class TelnetServer
    {
        private readonly int _port;

        public TelnetServer(int port)
        {
            _port = port;
        }

        public async Task StartAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            Console.WriteLine($"Telnet server started on port {_port}.");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();

                await stream.WriteAsync(Encoding.UTF8.GetBytes("Username: "), 0, "Username: ".Length);
                string username = await TelnetInput(stream);
                await stream.WriteAsync(Encoding.UTF8.GetBytes("Password: "), 0, "Password: ".Length);
                string password = await TelnetInput(stream);

                if (!Program.UserAuth(username, password))
                {
                    await stream.WriteAsync(Encoding.UTF8.GetBytes("Authentication failed!\n"), 0, "Authentication failed!\n".Length);
                    return;
                }
                await stream.WriteAsync(Encoding.UTF8.GetBytes("Authentication successful!\n"), 0, "Authentication successful!\n".Length);
                while (true)
                {
                    string command = await TelnetInput(stream);
                    Console.WriteLine($"Received command: {command}");

                    switch (command.ToLower())
                    {
                        case "exit":
                            return;
                        case "p":
                            Program.publishPause = true;
                            await stream.WriteAsync(Encoding.UTF8.GetBytes("Publishing paused.\n"), 0, "Publishing paused.\n".Length);
                            break;
                        case "r":
                            Program.publishPause = false;
                            await stream.WriteAsync(Encoding.UTF8.GetBytes("Publishing resumed.\n"), 0, "Publishing resumed.\n".Length);
                            break;
                    }
                }
            }
        }
        private async Task<string> TelnetInput(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        }
    }
}
