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

namespace MQTT.Client
{
    class Program
    {
        private static int MessageCounter = 0;
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var serverHost = Environment.GetEnvironmentVariable("MQTT_SERVER_HOST") ?? "mqtt-server";
            
            MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                        .WithClientId("client")
                                        .WithCredentials("user1123", "12345")
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
                string payload = Encoding.UTF8.GetString(a.ApplicationMessage.Payload);
                Log.Logger.Information("Message received: {payload}", payload);
            });

            mqttClientFactory.StartAsync(options).GetAwaiter().GetResult();
            mqttClientFactory.SubscribeAsync("info");
            Console.WriteLine();
            while (true);
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
