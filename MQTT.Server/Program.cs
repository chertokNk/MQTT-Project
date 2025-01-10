﻿using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;
using Serilog;
using Newtonsoft.Json;

namespace MQTTFirstLook.Broker
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

        public static void OnNewConnection(MqttConnectionValidatorContext context)
        {
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
