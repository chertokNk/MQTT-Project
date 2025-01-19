using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;
using Serilog;
using System.DirectoryServices.Protocols;
using System.Reflection.PortableExecutable;

namespace MQTT.Server
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

            MqttServerOptionsBuilder options = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(707)
                .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
                .WithConnectionValidator(OnNewConnection)
                .WithApplicationMessageInterceptor(OnNewMessage);


            IMqttServer mqttServerFactory = new MqttFactory().CreateMqttServer();

            mqttServerFactory.StartAsync(options.Build()).GetAwaiter().GetResult();
            while (true)
            {
                var testmsg = new MqttApplicationMessageBuilder()
                    .WithTopic("info")
                    .WithPayload($"Payload:{DateTimeOffset.UtcNow}")
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();
                mqttServerFactory.PublishAsync(testmsg);

                Task.Delay(1000).GetAwaiter().GetResult();
            }
        }
        public static bool UserAuth(string username, string password)
        {
            string ldapHost = "LDAP://localhost:8090";
            string dn = $"uid={username},ou=Users,dc=chnk,dc=com";
            try
            {
                using (var ldapConnection = new LdapConnection(new LdapDirectoryIdentifier(ldapHost)))
                {
                    ldapConnection.AuthType = AuthType.Basic;
                    ldapConnection.Bind(new NetworkCredential(dn, password));
                    return true;
                }
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
                Log.Logger.Warning("Authentication failed for user: {username}", username);
                return;
            }

            Log.Logger.Information(
                    "New connection: ClientId = {clientId}, Endpoint = {endpoint}, CleanSession = {cleanSession}",
                    context.ClientId,
                    context.Endpoint,
                    context.CleanSession);
        }

        public static void OnNewMessage(MqttApplicationMessageInterceptorContext context)
        {
            var payload = context.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(context.ApplicationMessage?.Payload);

            MessageCounter++;

            Log.Logger.Information(
                "MessageId: {MessageCounter} - TimeStamp: {TimeStamp} -- Message: ClientId = {clientId}, Topic = {topic}, Payload = {payload}, QoS = {qos}, Retain-Flag = {retainFlag}",
                MessageCounter,
                DateTime.Now,
                context.ApplicationMessage?.Topic,
                payload,
                context.ApplicationMessage?.QualityOfServiceLevel,
                context.ApplicationMessage?.Retain);
        }
    }
}
