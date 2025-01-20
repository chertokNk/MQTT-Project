using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using Serilog;
using System.Collections.Generic;

namespace MQTT.Client
{
    class Program
    {
        private static IManagedMqttClient mqttManagedClient;
        //used to pause messages
        private static bool messagePause = false;
        private static Queue<string> messageBuffer = new Queue<string>();
        static void Main(string[] args)
        {
            Console.WriteLine("Hint: docker attach mqtt-server");
            Console.WriteLine("Use 'connect' to start");
            //Commands
            Task.Run(() => ConsoleInput());
            //MQTT
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            mqttManagedClient = new MqttFactory().CreateManagedMqttClient();

            mqttManagedClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
            mqttManagedClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);
            mqttManagedClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(OnConnectingFailed);

            mqttManagedClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(a =>
            {
                if (messagePause == false)
                {
                    string payload = Encoding.UTF8.GetString(a.ApplicationMessage.Payload);
                    Log.Logger.Information($"Message received: {payload}");
                }
                else
                {
                    string payload = Encoding.UTF8.GetString(a.ApplicationMessage.Payload);
                    messageBuffer.Enqueue($"Buffered message: {payload}");
                }
            });
            //Run, client, run
            while (true);
        }
        private static void Connect()
        {
            Console.WriteLine("Enter credentials");
            Console.WriteLine("Username: ");
            string username = Console.ReadLine();
            Console.WriteLine("Password: ");
            string password = Console.ReadLine();
            MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                        .WithClientId("client")
                                        .WithCredentials(username, password)
                                        .WithTcpServer("mqtt-server", 707);

            ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
                                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
                                    .WithClientOptions(builder.Build())
                                    .Build();

            mqttManagedClient.StartAsync(options).GetAwaiter().GetResult();
            mqttManagedClient.SubscribeAsync("info");


        }
        private static void ConsoleInput()
        {
            while(true)
            {
                string input = Console.ReadLine();
                switch (input.ToLower())
                {
                    case "connect":
                        mqttManagedClient.StopAsync();
                        Connect();
                        break;
                    case "r":
                        Console.WriteLine("Message Display Resumed");
                        while (messageBuffer.Count > 0)
                        {
                            Console.WriteLine(messageBuffer.Dequeue());
                        }
                        messagePause = false;
                        break;
                    case "hello":
                        Console.WriteLine("Hello World");
                        break;
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "p":
                        Console.WriteLine("Message Display Paused");
                        messagePause = true;
                        break;
                }
            }
        }
        public static void OnConnected(MqttClientConnectedEventArgs obj)
        {
            Log.Logger.Information("Successfully connected.");
        }

        public static void OnConnectingFailed(ManagedProcessFailedEventArgs obj)
        {
            Log.Logger.Warning("Couldn't connect to broker.");
        }

        public static void OnDisconnected(MqttClientDisconnectedEventArgs obj)
        {
            Log.Logger.Information("Successfully disconnected.");
        }
    }
}
