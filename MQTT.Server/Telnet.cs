using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MQTT.Server
{
    internal class TelnetServer
    {
        private readonly int _port;

        internal TelnetServer(int port)
        {
            _port = port;
        }

        internal async Task StartAsync()
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

                await ReturnMessage(stream, "Username: ");
                string username = await TelnetInput(stream);
                await ReturnMessage(stream, "Password: ");
                string password = await TelnetInput(stream);

                if (!Program.UserAuth(username, password, true))
                {
                    await ReturnMessage(stream, "Authentication failed!", true);
                    return;
                }
                await ReturnMessage(stream, "Authentication successful!", true);
                await ReturnMessage(stream, "Use 'help' to get a list of all commands", true);
                while (true)
                {
                    string command = await TelnetInput(stream);
                    Console.WriteLine($"Received command: {command}");

                    switch (command.ToLower())
                    {
                        case "p":
                            Program.publishPause = true;
                            await ReturnMessage(stream, "Publishing paused.",true);
                            break;
                        case "r":
                            Program.publishPause = false;
                            await ReturnMessage(stream, "Publishing resumed.",true);
                            break;
                        case "getall":
                            var telnetClients = await Program.mqttServer.GetClientStatusAsync();

                            foreach (var c in telnetClients)
                            {
                                await ReturnMessage(stream, $"Client ID: {c.ClientId}",true);
                                await ReturnMessage(stream, $"Endpoint: {c.Endpoint}",true);
                            }
                            break;
                        case "kickall":
                            Program.KickAllClients().GetAwaiter().GetResult();
                            break;
                        case "kick":
                            await ReturnMessage(stream, "Enter client ID:");
                            Program.KickClient(await TelnetInput(stream)).GetAwaiter().GetResult();
                            break;
                        case "timeout":
                            await ReturnMessage(stream, "Enter timeout duration(seconds)",true);
                            await ReturnMessage(stream, $"Current: {Program.timeoutDuration}",true);
                            try
                            {
                                Program.timeoutDuration = TimeSpan.FromSeconds(double.Parse(await TelnetInput(stream)));
                                break;
                            }
                            catch (Exception ex)
                            {
                                await ReturnMessage(stream, $"Error: {ex.Message}", true);
                            }
                            break;
                        case "help":
                            await ReturnMessage(stream, $"Stop publishing/resume publishing: p/r", true);
                            await ReturnMessage(stream, $"Get all clients: getall", true);
                            await ReturnMessage(stream, $"Kick client: kick", true);
                            await ReturnMessage(stream, $"Kick all clients: kickall", true);
                            await ReturnMessage(stream, $"Change timeout duration: timeout", true);
                            await ReturnMessage(stream, $"Close telnet connection: exit", true);
                            await ReturnMessage(stream, $"Shutdown server: shutdown", true);
                            break;
                        case "shutdown":
                            Environment.Exit(0);
                            break;
                        case "exit":
                            return;
                        default:
                            await ReturnMessage(stream, "Unknown command", true);
                            break;
                    }
                }
            }
        }
        private static async Task ReturnMessage(NetworkStream stream,string message,bool newline = false)
        {
            if (newline)
            {
                //using \r\n instead of just \n to support both CRLF and LF
                message += "\r\n";
            }
            await stream.WriteAsync(Encoding.UTF8.GetBytes(message, 0, message.Length));
        }
        private async Task<string> TelnetInput(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            //for powershell compatability using SB instead of writing directly
            StringBuilder input = new StringBuilder();
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string key = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (key.Contains("\r") || key.Contains("\n"))
                {
                    input.Append(key.TrimEnd('\r', '\n'));
                    break;
                }
                input.Append(key);
            }
            
            return input.ToString().Trim();
        }
    }
}
