using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Internal;
//using MQTTnet.Client.Connecting;
//using MQTTnet.Client.Disconnecting;
//using MQTTnet.Client.Options;

namespace TestApp2
{
    public enum EntityType
    {
        Registry = 0,
        Device = 1
    }

    public enum TopicType
    {
        Events = 0,
        Commands = 1
    }

    /// <summary>
    /// Класс взаимодействия с устройствами и реестрами YaCloud
    /// </summary>
    class YaClient : IDisposable
    {
        public const string MqttServer = "mqtt.cloud.yandex.net";
        public const int MqttPort = 8883;

        private static X509Certificate2 rootCrt = new X509Certificate2("rootCA.crt");

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="entityId">Id сущности</param>
        /// <param name="entity">Тип сущности</param>
        /// <param name="topic">Тип топика</param>
        /// <returns></returns>
        public static string TopicName(string entityId, EntityType entity, TopicType topic)
        {
            string result = (entity == EntityType.Registry) ? "$registries/" : "$devices/";
            result += entityId;
            result += (topic == TopicType.Events) ? "/events" : "/commands";
            return result;
        }

        public delegate void OnSubscribedData(string topic, byte[] payload);
        public event OnSubscribedData SubscribedData;

        private IMqttClient mqttClient = null;
        private ManualResetEvent oCloseEvent = new ManualResetEvent(false);
        private ManualResetEvent oConnectedEvent = new ManualResetEvent(false);

        /// <summary>
        /// Метод подключения по Tls
        /// </summary>
        /// <param name="certPath">Путь к сертификату</param>
        public void Start(string certPath)
        {
            X509Certificate2 certificate = new X509Certificate2(certPath);
            List<X509Certificate2> certificates = new List<X509Certificate2>();
            certificates.Add(certificate);

            //certificates.Add(certificate.Export(X509ContentType.SerializedCert));

            //setup connection options
            MqttClientOptionsBuilderTlsParameters tlsOptions = new MqttClientOptionsBuilderTlsParameters
            {
                Certificates = certificates,
                SslProtocol = SslProtocols.Tls12,
                UseTls = true
            };

            tlsOptions.CertificateValidationHandler = CertificateValidationCallback;

            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId($"Test_C#_Client_{Guid.NewGuid()}")
                .WithTcpServer(MqttServer, MqttPort)
                .WithTls(tlsOptions)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(90))
                .Build();

            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            mqttClient.ApplicationMessageReceivedAsync += DataHandler;
            mqttClient.ConnectedAsync += ConnectedHandler;
            mqttClient.DisconnectedAsync += DisconnectedHandler;

            Console.WriteLine($"Connecting to mqtt.cloud.yandex.net...");
            mqttClient.ConnectAsync(options, CancellationToken.None);

        }


        /// <summary>
        /// Метод подключения к облаку
        /// </summary>
        /// <param name="id">Id ус-ва</param>
        /// <param name="password">Пароль</param>
        /// <returns></returns>
        public Task Start(string id, string password)
        {
            // Инициилизируем настройки подключения
            MqttClientOptionsBuilderTlsParameters tlsOptions = new MqttClientOptionsBuilderTlsParameters
            {
                SslProtocol = SslProtocols.Tls12,
                UseTls = true
            };

            tlsOptions.CertificateValidationHandler = CertificateValidationCallback;

            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId($"Test_C#_Client_{Guid.NewGuid()}")
                .WithTcpServer(MqttServer, MqttPort)
                .WithTls(tlsOptions)
                .WithCleanSession()
                .WithCredentials(id, password)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(90))
                .Build();

            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            mqttClient.ApplicationMessageReceivedAsync += DataHandler;
            mqttClient.ConnectedAsync += ConnectedHandler;
            mqttClient.DisconnectedAsync += DisconnectedHandler;

            Console.WriteLine($"Connecting to mqtt.cloud.yandex.net...");
            return mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        public void Stop()
        {
            oCloseEvent.Set();
            mqttClient.DisconnectAsync();
        }

        public void Dispose()
        {
            Stop();
        }

        public bool WaitConnected()
        {
            WaitHandle[] waites = { oCloseEvent, oConnectedEvent };
            return WaitHandle.WaitAny(waites) == 1;
        }

        public Task Subscribe(string topic, MQTTnet.Protocol.MqttQualityOfServiceLevel qos)
        {
            return mqttClient.SubscribeAsync(topic, qos);
        }

        public Task Publish(string topic, string payload, MQTTnet.Protocol.MqttQualityOfServiceLevel qos)
        {
            var mqqtAppMessage = new MqttApplicationMessage();
            mqqtAppMessage.Payload = Encoding.UTF8.GetBytes(payload);
            mqqtAppMessage.Topic = topic;

            return mqttClient.PublishAsync(mqqtAppMessage);
        }


        private Task ConnectedHandler(MqttClientConnectedEventArgs arg)
        {
            oConnectedEvent.Set();
            return Task.CompletedTask;
        }

        private static Task DisconnectedHandler(MqttClientDisconnectedEventArgs arg)
        {
            Console.WriteLine($"Disconnected mqtt.cloud.yandex.net.");
            return Task.CompletedTask;
        }

        private Task DataHandler(MqttApplicationMessageReceivedEventArgs arg)
        {
            SubscribedData(arg.ApplicationMessage.Topic, arg.ApplicationMessage.Payload);
            return Task.CompletedTask;
        }

        private static bool CertificateValidationCallback(MqttClientCertificateValidationEventArgs args)
        {
            //try
            //{
            //  if (errors == SslPolicyErrors.None)
            //  {
            //    return true;
            //  }

            //  if (errors == SslPolicyErrors.RemoteCertificateChainErrors)
            //  {
            //    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            //    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            //    chain.ChainPolicy.ExtraStore.Add(rootCrt);

            //    chain.Build((X509Certificate2)rootCrt);
            //    var res = chain.ChainElements.Cast<X509ChainElement>().Any(a => a.Certificate.Thumbprint == rootCrt.Thumbprint);
            //    return res;
            //  }
            //}
            //catch { }

            //return false;
            return true;
        }

    }
}