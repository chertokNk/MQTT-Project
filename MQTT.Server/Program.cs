using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;
using Serilog;
using System.DirectoryServices.Protocols;
using System.IO;

namespace MQTT.Server
{
    class Program
    {
        internal static IMqttServer mqttServer;
        internal static bool publishPause = false;
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
                        .WithPayload(DumpInfo())
                        .WithExactlyOnceQoS()
                        .WithRetainFlag()
                        .Build();
                    mqttServer.PublishAsync(testmsg);

                    Task.Delay(2500).GetAwaiter().GetResult();
                }
            }
        }
        private static string DumpInfo()
        {
            var process = Process.GetCurrentProcess();
            TimeSpan cpuTime = process.TotalProcessorTime;
            long memoryUsage = process.WorkingSet64;
            string dump = 
                $"CPU Time: {cpuTime.TotalMilliseconds} ms\n" +
                $"Memory Usage: {memoryUsage / 1024 / 1024} MB\n" +
                $"{DateTimeOffset.UtcNow}\n";
            return dump;
        }
        internal static bool UserAuth(string username, string password, bool if_telnet = false)
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
                    if(if_telnet == true)
                    {
                        var request = new SearchRequest(userDn, "(objectClass=*)", SearchScope.Base, new string[] { "employeeType" });
                        var response = (SearchResponse)ldapConnection.SendRequest(request);
                        try
                        {
                            var employeeType = response.Entries[0].Attributes["employeeType"];
                            if (employeeType?.Count > 0 && employeeType[0].ToString().Equals("telnet", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                            else
                            {
                                Log.Logger.Warning($"Telnet access denied to {username}");
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error($"Error during telnet authentication: {ex.Message}");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Logger.Information($"User {username} authenticated successfully");
                        return true;
                    }
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Log.Logger.Error($"LDAP server not found: {ex.Message}");
                return false;
            }
            catch (LdapException ex)
            {
                Log.Logger.Error($"LDAP authentication failed for user {username}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Error during LDAP authentication: {ex.Message}");
                return false;
            }
        }
        private static void OnNewConnection(MqttConnectionValidatorContext context)
        {
            var username = context.Username;
            var password = context.Password;

            if (!UserAuth(username, password))
            {
                context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.BadUserNameOrPassword;
                Log.Logger.Warning($"Authentication failed for user: {username},{password}");
                return;
            }

            Log.Logger.Information($"New connection perhaps: ClientId = {context.ClientId}, Endpoint = {context.Endpoint}, CleanSession = {context.CleanSession},usName = {context.Username}, pass = {context.Password}");
        }
        internal static async Task GetAllClients()
        {
            var clients = await mqttServer.GetClientStatusAsync();

            foreach (var client in clients)
            {
                Console.WriteLine($"Client ID: {client.ClientId}");
                Console.WriteLine($"Endpoint: {client.Endpoint}");
            }

        }
        internal static async Task KickAllClients()
        {
            var clients = await mqttServer.GetClientStatusAsync();

            foreach (var client in clients)
            {
                await client.DisconnectAsync();
            }

        }
        internal static async Task KickClient(string kickId)
        {
            var clients = await mqttServer.GetClientStatusAsync();
            try
            {
                foreach (var client in clients)
                {
                    if (client.ClientId == kickId)
                    {
                        await client.DisconnectAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
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
                    case "getall":
                        GetAllClients().GetAwaiter().GetResult();
                        break;
                    case "kickall":
                        KickAllClients().GetAwaiter().GetResult();
                        break;
                    case "kick":
                        Console.WriteLine("Enter client ID:");
                        KickClient(Console.ReadLine()).GetAwaiter().GetResult();
                        break;
                    case "shutdown":
                        Environment.Exit(0);
                        break;
                    case "p":
                        Console.WriteLine("Publishing Paused");
                        publishPause = true;
                        break;
                }
            }
        }
    }
}
