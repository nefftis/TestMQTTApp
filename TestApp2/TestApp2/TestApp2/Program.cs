using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Confluent.Kafka;
using System.Configuration;
using System.Collections.Specialized;

namespace TestApp2
{
    class DeviceInfo
    {
        public string DeviceId { get; set; }
        public string Bio { get; set; }
        public DateTime JoinDate { get; set; }
        public bool Author { get; set; }
    }

    class Device
    {
        string topic_;
        string id_;
        YaClient device_;

        Thread workingThread;

        internal ManualResetEvent onStop = new ManualResetEvent(false);

        internal Device(string topic, YaClient device, string id)
        {
            topic_ = topic;
            id_ = id;
            device_ = device;

            //workingThread = new Thread(DeviceWork);
            //workingThread.Start();
        }

        internal void DeviceWork()
        {
            var topic = topic_;
            var device = device_;
            var deviceId = id_;

            while (true)
            {
                var result = onStop.WaitOne(10000);

                if (result)
                {
                    return;
                }

                device.Publish(topic, "Test data", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();
                Console.WriteLine($"{deviceId} published data to: {topic}");
            }
        }
    }

    class Program
    {
        private static List<string> devicesIds_ = new List<string>();
        private static List<string> topics_ = new List<string>();
        private static List<YaClient> devices_ = new List<YaClient>();
        private static List<ManualResetEvent> onSubscibedDataEvents = new List<ManualResetEvent>();

        private static List<Device> deviceObjects = new List<Device>();

        private const bool useCerts = false; // change it if login-password authentication is used

        // used for certificate authentication
        private const string RegistryCertFileName = @"R:\IoT\registry-cert.pem";
        private const string DeviceCertFileName = @"R:\IoT\device-cert.pem";

        KafkaProducerService kafka_ = null;

        private static ManualResetEvent onStop = new ManualResetEvent(false);

        private static YaClient regClient = new YaClient();
        private static YaClient devClient = new YaClient();

        static void Main(string[] args)
        {
           KafkaProducerService.Initialize(ConfigurationManager.AppSettings["KafkaBootstrapServers"],
                ConfigurationManager.AppSettings["SecurityProtocol"],
                ConfigurationManager.AppSettings["SaslMechanism"],
                ConfigurationManager.AppSettings["SaslUsername"],
                ConfigurationManager.AppSettings["SaslPassword"]);

            // Простые ключи из <appSettings>
            string mqttServer = ConfigurationManager.AppSettings["MqttServer"];
            int mqttPort = int.Parse(ConfigurationManager.AppSettings["MqttPort"]);
            var registryId = ConfigurationManager.AppSettings["RegistryId"];
            var registryPassword = ConfigurationManager.AppSettings["RegistryPassword"];
            var deviceId = ConfigurationManager.AppSettings["DeviceId"];
            var devicePassword = ConfigurationManager.AppSettings["DevicePassword"];

            devicesIds_.Add(deviceId);

            //if (useCerts)
            //{
            //    regClient.Start(RegistryCertFileName);
            //}
            //else
            //{
            //    // throw new NotImplementedException();
            //    regClient.Start(registryId, registryPassword);
            //}

            regClient.Start(registryId, registryPassword);

            if (!regClient.WaitConnected())
            {
                return;
            }

            foreach (var id in devicesIds_)
            {
                var dev = new YaClient();

                topics_.Add(YaClient.TopicName(id, EntityType.Device, TopicType.Events) + "/emulator");

                dev.Start(deviceId, devicePassword);
                devices_.Add(dev);
                if (!dev.WaitConnected())
                {
                    return;
                }
            }

            regClient.SubscribedData += DataHandler;

            for (int i = 0; i < devicesIds_.Count; i++)
            {
                var topic = topics_[i];
                var device = devices_[i];

                regClient.Subscribe(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

                device.Publish(topic, "Test data 1", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

                Console.WriteLine($"Published data to: {topic}");

                onSubscibedDataEvents.Add(new ManualResetEvent(false));
            }

            WaitHandle.WaitAll(onSubscibedDataEvents.ToArray());

            //var regTopic = YaClient.TopicName(registryId, EntityType.Registry, TopicType.Events) + "/emulator";

            //for (int i = 0; i < devicesIds_.Count; i++)
            //{
            //    devices_[i].Subscribe(regTopic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

            //    var dev = new Device(regTopic, devices_[i], devicesIds_[i]);

            //    deviceObjects.Add(dev);
            //}

            //regClient.Publish(regTopic, "Test data 2", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

            Console.ReadLine();

            foreach (var devObj in deviceObjects)
            {
                devObj.onStop.Set();
            }


            regClient.Dispose();
            devClient.Dispose();
        }


        private static void DataHandler(string topic, byte[] payload)
        {
            if (payload != null)
            {
                var Payload = System.Text.Encoding.UTF8.GetString(payload);

                //Utf8JsonReader reader = new Utf8JsonReader();

                KafkaProducerService.ProduceAsync(topic, Payload);

                try
                {
                    JObject jsonObject = JObject.Parse(Payload);

                    // Accessing values from the JObject
                    string deviceId = (string)jsonObject["DeviceId"];
                    DateTime date = (DateTime)jsonObject["TimeStamp"];

                    var parameters = jsonObject["Values"];

                    StringBuilder builder = new StringBuilder();
                    builder.Append($"DeviceId: {deviceId}, timestamp {date}.\n");

                    foreach (var parameter in parameters)
                    {
                        var type = (string)parameter["Type"];

                        if (type == "Float")
                        {
                            builder.Append($"{parameter["Name"]}: {parameter["Value"]}.\n");
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }

                    Console.WriteLine(builder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Received data: {topic}:\t{Payload}");
                }

                // Console.WriteLine($"Received data: {topic}:\t{Payload}");
            }

            for (int i = 0; i < topics_.Count; i++)
            {
                if (topics_[i] == topic)
                {
                    onSubscibedDataEvents[i].Set();
                    break;
                }
            }
        }
    }
}
