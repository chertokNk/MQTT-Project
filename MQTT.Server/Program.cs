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
        private static IMqttServer mqttServer;
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
        public static string DumpInfo()
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
    }
}
