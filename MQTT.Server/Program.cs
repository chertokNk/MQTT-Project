using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;
using Serilog;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Net.Sockets;

namespace MQTT.Server
{
    class Program
    {
        private static IMqttServer mqttServer;
        private static bool publishPause = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Hint: docker attach mqtt-server");
            Console.WriteLine("Use 'connect' to start");
            //Commands
            Task.Run(() => ConsoleInput());
            Task.Run(() => new TelnetServer(23).StartAsync());
            //MQTT
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            MqttServerOptionsBuilder options = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(707)
                .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
                .WithConnectionValidator(OnNewConnection);


            mqttServer = new MqttFactory().CreateMqttServer();

            mqttServer.StartAsync(options.Build()).GetAwaiter().GetResult();
            while (true)
            {
                while (!publishPause)
                {
                    var testmsg = new MqttApplicationMessageBuilder()
                        .WithTopic("info")
                        .WithPayload($"Payload:{DateTimeOffset.UtcNow}")
                        .WithExactlyOnceQoS()
                        .WithRetainFlag()
                        .Build();
                    mqttServer.PublishAsync(testmsg);

                    Task.Delay(2500).GetAwaiter().GetResult();
                }
            }
        }
        public static bool UserAuth(string username, string password)
        {
            string userDn = $"uid={username},ou=Users,dc=chnk,dc=org";
            if (username == "admin")
            {
                userDn = $"cn={username},dc=chnk,dc=org";
            }
            try
            {  
                using (var ldapConnection = new LdapConnection(new LdapDirectoryIdentifier("ldap_server", 389)))
                {
                    ldapConnection.AuthType = AuthType.Basic;
                    ldapConnection.SessionOptions.ProtocolVersion = 3;
                    ldapConnection.SessionOptions.SecureSocketLayer = false;
                    ldapConnection.Bind(new NetworkCredential(userDn, password));
                    Log.Logger.Information($"User {username} authenticated successfully");
                    return true;
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Log.Logger.Error("LDAP server not found: {message}", ex.Message);
                return false;
            }
            catch (LdapException ex)
            {
                Log.Logger.Error("LDAP authentication failed for user {username}: {message}", username, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error during LDAP authentication: {message}", ex.Message);
                return false;
            }
        }
        public static void OnNewConnection(MqttConnectionValidatorContext context)
        {
            var username = context.Username;
            var password = context.Password;

            if (!UserAuth(username, password))
            {
                context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.BadUserNameOrPassword;
                Log.Logger.Warning($"Authentication failed for user: {username},{password}");
                return;
            }

            Log.Logger.Information($"New connection pperhaps: ClientId = {context.ClientId}, Endpoint = {context.Endpoint}, CleanSession = {context.CleanSession},usName = {context.Username}, pass = {context.Password}");
        }
        private static void ConsoleInput()
        {
            while (true)
            {
                string input = Console.ReadLine();
                switch (input.ToLower())
                {
                    case "r":
                        Console.WriteLine("Publishing Resumed");
                        publishPause = false;
                        break;
                    case "hello":
                        Console.WriteLine("Hello World");
                        break;
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "p":
                        Console.WriteLine("Publishing Paused");
                        publishPause = true;
                        break;
                }
            }
        }
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

                    if (!UserAuth(username, password))
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
                                publishPause = true;
                                await stream.WriteAsync(Encoding.UTF8.GetBytes("Publishing paused.\n"), 0, "Publishing paused.\n".Length);
                                break;
                            case "r":
                                publishPause = false; 
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
}
