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
using System.DirectoryServices;
using Serilog;
using System.Runtime.CompilerServices;

namespace MQTT.Client
{
    class Program
    {
        //used to pause messages
        private static bool messagePause = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Hint: docker attach mqtt-server");
            //Commands
            Task.Run(() => ConsoleInput());
            //MQTT
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            
            var serverHost = Environment.GetEnvironmentVariable("MQTT_SERVER_HOST") ?? "mqtt-server";
            
            MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                        .WithClientId("user1")
                                        .WithCredentials("user1","12345")
                                        .WithTcpServer(serverHost, 707);

            ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
                                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
                                    .WithClientOptions(builder.Build())
                                    .Build();

            IManagedMqttClient mqttClientFactory = new MqttFactory().CreateManagedMqttClient();

            mqttClientFactory.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
            mqttClientFactory.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);
            mqttClientFactory.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(OnConnectingFailed);

            mqttClientFactory.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(a => {
                if(messagePause == false)
                {
                    string payload = Encoding.UTF8.GetString(a.ApplicationMessage.Payload);
                    Log.Logger.Information("Message received: {payload}", payload);
                }
            });

            mqttClientFactory.StartAsync(options).GetAwaiter().GetResult();
            mqttClientFactory.SubscribeAsync("info");
            //Run, client, run
            while(true);
        }
        private static void ConsoleInput()
        {
            while(true)
            {
                
                string input = Console.ReadLine();
                switch (input.ToLower())
                {
                    case "r":
                        Console.WriteLine("Message Display Resumed");
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
