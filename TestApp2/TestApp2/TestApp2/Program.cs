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
    /// <summary>
    /// Информация об устройстве
    /// </summary>
    class DeviceInfo
    {
        public string DeviceId { get; set; }
        public string Bio { get; set; }
        public DateTime JoinDate { get; set; }
        public bool Author { get; set; }
    }

    /// <summary>
    /// Класс, используемый для постоянной отправки устройством данных в топик (сейчас не используется)
    /// </summary>
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

            workingThread = new Thread(DeviceWork);
            workingThread.Start();
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
        /// <summary>
        /// Идентификаторы устройств
        /// </summary>
        private static List<string> devicesIds_ = new List<string>();

        /// <summary>
        /// Топики устройств
        /// </summary>
        private static List<string> topics_ = new List<string>();

        /// <summary>
        /// Устройства
        /// </summary>
        private static List<YaClient> devices_ = new List<YaClient>();

        /// <summary>
        /// Список событий подписки
        /// </summary>
        private static List<ManualResetEvent> onSubscibedDataEvents = new List<ManualResetEvent>();

        /// <summary>
        /// Список устройств для эмуляции отправки (пок не используется)
        /// </summary>
        private static List<Device> deviceObjects = new List<Device>();

        /// <summary>
        /// Флаг использования сертификатов (для Tls)
        /// </summary>
        private const bool useCerts = false; // change it if login-password authentication is used

        /// <summary>
        /// Пути к сертификатам
        /// </summary>
        private const string RegistryCertFileName = @"R:\IoT\registry-cert.pem";
        private const string DeviceCertFileName = @"R:\IoT\device-cert.pem";

        /// <summary>
        /// Задел под кафку
        /// </summary>
        KafkaProducerService kafka_ = null;

        private static ManualResetEvent onStop = new ManualResetEvent(false);

        private static YaClient regClient = new YaClient();

        static void Main(string[] args)
        {
            //KafkaProducerService.Initialize(ConfigurationManager.AppSettings["KafkaBootstrapServers"],
            //     ConfigurationManager.AppSettings["SecurityProtocol"],
            //     ConfigurationManager.AppSettings["SaslMechanism"],
            //     ConfigurationManager.AppSettings["SaslUsername"],
            //     ConfigurationManager.AppSettings["SaslPassword"]);

            // Получаем ключи из <appSettings>
            string mqttServer = ConfigurationManager.AppSettings["MqttServer"];
            int mqttPort = int.Parse(ConfigurationManager.AppSettings["MqttPort"]);
            var registryId = ConfigurationManager.AppSettings["RegistryId"];
            var registryPassword = ConfigurationManager.AppSettings["RegistryPassword"];
            var deviceId = ConfigurationManager.AppSettings["DeviceId"];
            var devicePassword = ConfigurationManager.AppSettings["DevicePassword"];

            devicesIds_.Add(deviceId);

            // Подключаемся к реестру
            regClient.Start(registryId, registryPassword);
            if (!regClient.WaitConnected())
                return;

            // Для всех устройств
            foreach (var id in devicesIds_)
            {
                //  Создаем объект устройства
                var device = new YaClient();
                devices_.Add(device);

                // Добавляем название топика устройства в список
                topics_.Add(YaClient.TopicName(id, EntityType.Device, TopicType.Events) + "/emulator");

                // Подключаемся к стройству
                device.Start(deviceId, devicePassword);
                if (!device.WaitConnected())
                {
                    return;
                }
            }

            // Подписываемся на событие получения данных 
            regClient.SubscribedData += DataHandler;

            // Для всех устройств (сейчас одно)
            for (int i = 0; i < devicesIds_.Count; i++)
            {
                var topic = topics_[i];
                var device = devices_[i];

                // Реестри подписывается на топик устройства
                regClient.Subscribe(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

                // Для примера устройство публикует в топик данные, должен сработать DataHandler
                device.Publish(topic, "Test data 1", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();
                Console.WriteLine($"Published data to: {topic}");

                onSubscibedDataEvents.Add(new ManualResetEvent(false));
            }

            // Ждем окончание подписки
            WaitHandle.WaitAll(onSubscibedDataEvents.ToArray());

            Console.ReadLine();

            // Освобождаем объекты
            foreach (var dev in devices_)
            {
                dev.Dispose();
            }

            regClient.Dispose();
        }

        /// <summary>
        /// Обработчик получения реестром данным после подписки на топики устройств
        /// Варианты получения данных:
        /// 1. При старте приложения (стр. 166 кода) "Test Data 1"
        /// 2. При использованиие класса Device (сейчас не используется)
        /// 3. В облаке необходимо запустить скрипты, чтобы сюда сыпались данные
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        private static void DataHandler(string topic, byte[] payload)
        {
            if (payload != null)
            {
                var Payload = System.Text.Encoding.UTF8.GetString(payload);

                // Utf8JsonReader reader = new Utf8JsonReader();

                // KafkaProducerService.ProduceAsync(topic, Payload);

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
