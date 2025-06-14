using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Confluent.Kafka;
using System.Configuration;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;

namespace TestApp2
{
    /// <summary>
    /// Информация об устройстве
    /// </summary>
    /// 

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

        private static ManualResetEvent onStop = new ManualResetEvent(false);

        private static YaClient regClient = new YaClient();
        static KafkaProducer kafka = null;



        static void Main(string[] args)
        {
            //var kafkaBootstrap = ConfigurationManager.AppSettings["KafkaBootstrapServers"];
            //var kafkaTopic = ConfigurationManager.AppSettings["KafkaTopic"];
            //var kafka = new KafkaProducer(kafkaBootstrap, kafkaTopic);

            string mqttServer = ConfigurationManager.AppSettings["MqttServer"];
            int mqttPort = int.Parse(ConfigurationManager.AppSettings["MqttPort"]);
            string yandexIamToken = "t1.9euelZqOyMuUnJONj5CQj5GXl5bHku3rnpWakJWKjpOejsycx82Jj8-Tz8nl8_dhRlQ9-e9sC0UP_d3z9yF1UT3572wLRQ_9zef1656Vmo7Pi5WLnpfPlZaMnp2dlpCY7_zF656Vmo7Pi5WLnpfPlZaMnp2dlpCY.ctLZh7DUGwqwshO3LWGxgUxF0M8U_0b7XXnuzpGgPV_FcGa0DWRFtImhEH9qf7IHJAZMSCwzDouGVxsjFLiwBQ";
            string onePasswordForAll = "tESTbROKER1234";
            string folderId = ConfigurationManager.AppSettings["FolderId"];

            // Получаем список всех реестров в папке
            List<RegistryInfo> registries = GetRegistries(folderId, yandexIamToken);

            foreach (var registryInfo in registries)
            {
                Console.WriteLine($"Обработка реестра: {registryInfo.Id}");

                var registryId = registryInfo.Id;
                var registryPassword = onePasswordForAll;

                // Получаем устройства для текущего реестра
                List<string> deviceIdsList = GetDevicesForRegistry(registryId, yandexIamToken);
                var deviceIds = deviceIdsList.ToArray();
                var devicePasswords = Enumerable.Repeat(onePasswordForAll, deviceIds.Length).ToArray();

                if (deviceIds.Length != devicePasswords.Length)
                {
                    Console.WriteLine($"Количество deviceId и devicePassword не совпадает для реестра {registryId}!");
                    continue;
                }

                // Создаем и подключаем клиент для реестра
                var regClient = new YaClient();
                regClient.Start(registryId, registryPassword);
                if (!regClient.WaitConnected())
                {
                    Console.WriteLine($"Не удалось подключиться к реестру {registryId}");
                    continue;
                }

                List<YaClient> devices = new List<YaClient>();
                List<string> topics = new List<string>();
                List<ManualResetEvent> events = new List<ManualResetEvent>();

                // Для всех устройств в реестре
                for (int i = 0; i < deviceIds.Length; i++)
                {
                    var deviceId = deviceIds[i];
                    var devicePassword = devicePasswords[i];

                    // Создаем объект устройства
                    var device = new YaClient();
                    devices.Add(device);

                    // Добавляем название топика устройства в список
                    topics.Add(YaClient.TopicName(registryId, EntityType.Registry, TopicType.Events));

                    // Подключаемся к устройству
                    device.Start(deviceId, devicePassword);
                    if (!device.WaitConnected())
                    {
                        Console.WriteLine($"Не удалось подключиться к устройству {deviceId} в реестре {registryId}");
                        continue;
                    }
                }

                // Подписываемся на событие получения данных 
                regClient.SubscribedData += (topic, payload) => DataHandler(topic, payload, registryId, devices);

                // Для всех устройств
                for (int i = 0; i < deviceIds.Length; i++)
                {
                    var topic = topics[i];
                    var device = devices[i];

                    // Реестр подписывается на топик устройства
                    regClient.Subscribe(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

                    // Устройство публикует в топик данные
                    device.Publish(topic, $"Test data from {deviceIds[i]}", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();
                    Console.WriteLine($"Published data to: {topic}");

                    events.Add(new ManualResetEvent(false));
                }

                //// Ждем окончание подписки для этого реестра
                //if (events.Count > 0)
                //{
                //    WaitHandle.WaitAll(events.ToArray());
                //}

                //// Освобождаем объекты
                //foreach (var dev in devices)
                //{
                //    dev.Dispose();
                //}
                //regClient.Dispose();
            }

            //kafka.Dispose();
            Console.ReadLine();
        }

        private static List<RegistryInfo> GetRegistries(string folderId, string iamToken)
        {
            var registries = new List<RegistryInfo>();
            string registriesUrl = $"https://iot-devices.api.cloud.yandex.net/iot-devices/v1/registries?folderId={folderId}";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", iamToken);

                var response = httpClient.GetAsync(registriesUrl).Result;
                response.EnsureSuccessStatusCode();

                var content = response.Content.ReadAsStringAsync().Result;
                var json = JObject.Parse(content);

                foreach (var registry in json["registries"])
                {
                    registries.Add(new RegistryInfo
                    {
                        Id = registry["id"].ToString(),
                        Name = registry["name"].ToString()
                    });
                }
            }

            return registries;
        }

        private static List<string> GetDevicesForRegistry(string registryId, string iamToken)
        {
            var deviceIds = new List<string>();
            string devicesUrl = $"https://iot-devices.api.cloud.yandex.net/iot-devices/v1/devices?registryId={registryId}";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", iamToken);

                    var response = httpClient.GetAsync(devicesUrl).Result;
                    response.EnsureSuccessStatusCode();

                    var content = response.Content.ReadAsStringAsync().Result;
                    var json = JObject.Parse(content);

                    // Проверяем наличие поля devices и что это массив
                    if (json["devices"] is JArray devicesArray)
                    {
                        foreach (var device in devicesArray)
                        {
                            // Проверяем наличие поля id
                            if (device["id"] != null)
                            {
                                deviceIds.Add(device["id"].ToString());
                            }
                        }
                    }
                    else
                    {
                        // Логируем или обрабатываем отсутствие устройств
                        Console.WriteLine("No 'devices' array found in response or it's empty");
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем полную ошибку
                Console.WriteLine($"Error getting devices: {ex}");
                throw; // Перебрасываем исключение дальше или обрабатываем
            }

            return deviceIds;
        }

        private static void DataHandler(string topic, byte[] payload, string registryId, List<YaClient> devices)
        {
            if (payload == null) return;

            var payloadString = System.Text.Encoding.UTF8.GetString(payload);

            try
            {
                JObject jsonObject = JObject.Parse(payloadString);

                string deviceId = jsonObject["DeviceId"]?.ToString() ?? "Неизвестно";
                DateTime date = jsonObject["TimeStamp"]?.Value<DateTime>() ?? DateTime.MinValue;

                var parametersToken = jsonObject["Values"];
                StringBuilder builder = new StringBuilder();
                builder.Append($"DeviceId: {deviceId}, timestamp {date}.\n");

                if (parametersToken is JArray parametersArray)
                {
                    foreach (var parameter in parametersArray)
                    {
                        var name = parameter["Name"]?.ToString() ?? "Неизвестно";
                        var value = parameter["Value"]?.ToString() ?? "Неизвестно";
                        var type = parameter["Type"]?.ToString() ?? "Неизвестно";
                        builder.Append($"{name}: {value} (тип: {type}).\n");
                    }
                }
                else if (parametersToken is JObject parameterObj)
                {
                    var name = parameterObj["Name"]?.ToString() ?? "Неизвестно";
                    var value = parameterObj["Value"]?.ToString() ?? "Неизвестно";
                    var type = parameterObj["Type"]?.ToString() ?? "Неизвестно";
                    builder.Append($"{name}: {value} (тип: {type}).\n");
                }
                else
                {
                    builder.Append("Нет параметров Values или неизвестный формат.\n");
                }

                Console.WriteLine(builder);

                // Публикация в Яндекс-топик 'state'
                var stateTopic = YaClient.TopicName(registryId, EntityType.Registry, TopicType.state);
                foreach (var device in devices)
                {
                    device.Publish(stateTopic, payloadString, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();
                    Console.WriteLine($"Published data to state topic: {stateTopic}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке данных: {ex.Message}");
                Console.WriteLine($"Полученные данные: {topic}:\t{payloadString}");
            }
        }

        public class RegistryInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

    }
}
