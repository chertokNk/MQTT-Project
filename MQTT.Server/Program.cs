using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using Serilog;
using System.DirectoryServices.Protocols;
using System.Reflection.PortableExecutable;
using System.IO;
using System.Security.AccessControl;
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
                        Console.WriteLine("Message Resumed");
                        publishPause = false;
                        break;
                    case "hello":
                        Console.WriteLine("Hello World");
                        break;
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "p":
                        Console.WriteLine("Messages Paused");
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
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    // Send welcome message
                    byte[] welcomeMessage = Encoding.UTF8.GetBytes("Welcome to the Telnet server!\n");
                    await stream.WriteAsync(welcomeMessage, 0, welcomeMessage.Length);

                    string username = await PromptForInput(stream, "Username: ");
                    string password = await PromptForInput(stream, "Password: ");

                    if (!UserAuth(username, password))
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Authentication failed. Goodbye!\n"), 0, 30);
                        return;
                    }
                    //something here broke
                    Console.WriteLine("Hello there debug");//got here
                    await stream.WriteAsync(Encoding.UTF8.GetBytes("Authentication successful!\n"), 0, 30);
                    Console.WriteLine("Hello there numba 2 debug");//but ot here
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        string command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        Console.WriteLine($"Received command: {command}");

                        // Process commands
                        switch (command.ToLower())
                        {
                            case "exit":
                                await stream.WriteAsync(Encoding.UTF8.GetBytes("Goodbye!\n"), 0, 8);
                                return;
                            case "hello":
                                await stream.WriteAsync(Encoding.UTF8.GetBytes("Server is running.\n"), 0, 22);
                                break;
                            case "p":
                                publishPause = true;
                                await stream.WriteAsync(Encoding.UTF8.GetBytes("Publishing paused.\n"), 0, 24);
                                break;
                            case "r":
                                publishPause = false; 
                                await stream.WriteAsync(Encoding.UTF8.GetBytes("Publishing resumed.\n"), 0, 25);
                                break;
                        }
                    }
                }
            }
            private async Task<string> PromptForInput(NetworkStream stream, string prompt)
            {
                byte[] promptBytes = Encoding.UTF8.GetBytes(prompt);
                await stream.WriteAsync(promptBytes, 0, promptBytes.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            }
        }
    }
}
