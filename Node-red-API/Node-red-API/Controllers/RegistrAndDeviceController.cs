using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace Node_red_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrAndDeviceController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _iotUrl = "https://iot-devices.api.cloud.yandex.net/iot-devices/v1/";
        private readonly string _folderId = "b1g913lffej8kphm0lm7";
        private readonly string _iamToken = "t1.9euelZqdnc2JjYmLm8nMx52Zk4vPlO3rnpWakJWKjpOejsycx82Jj8-Tz8nl9PcHc0o9-e9ZbCLl3fT3RyFIPfnvWWwi5c3n9euelZqPmJuMnZfIkZjOlpaVzs2bk-_8xeuelZqPmJuMnZfIkZjOlpaVzs2bkw.9Tf6BnrVp75L2pI8PhamFSPZY6sZYmKq47gnBhaHPzm7RWspdsKmARRckg3GXXVUYVb2SKCacClFohaukTUwCA";
        public RegistrAndDeviceController()
        {
            _httpClient = new HttpClient();
        }

        [HttpGet]
        public async Task<IActionResult> GetRegistriesAndDevices()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _iamToken);

            try
            {
                // Запрашиваем все регистры
                var registriesResponse = await _httpClient.GetAsync($"{_iotUrl}registries?folderId={_folderId}");
                registriesResponse.EnsureSuccessStatusCode();
                var registriesJson = await registriesResponse.Content.ReadAsStringAsync();
                dynamic registries = JsonConvert.DeserializeObject(registriesJson);

                var result = new List<object>();

                if (registries?.registries != null)
                {
                    foreach (var registry in registries.registries)
                    {
                        if (registry.id != null && registry.name != null)
                        {
                            var registryId = registry.id.ToString();
                            var registryName = registry.name.ToString();
                            var registryState = registry.status.ToString();

                            // Запрашиваем все устройства для текущего регистра
                            var devicesResponse = await _httpClient.GetAsync($"{_iotUrl}devices?registryId={registryId}");
                            devicesResponse.EnsureSuccessStatusCode();
                            var devicesJson = await devicesResponse.Content.ReadAsStringAsync();
                            dynamic devices = JsonConvert.DeserializeObject(devicesJson);

                            var deviceList = new List<object>();

                            if (devices?.devices != null)
                            {
                                foreach (var device in devices.devices)
                                {
                                    if (device.id != null && device.name != null)
                                    {
                                        // Создаем список меток с их значениями
                                        var labels = new Dictionary<string, string>();

                                        if (device.labels != null)
                                        {
                                            foreach (JProperty label in device.labels)
                                            {
                                                string key = label.Name;
                                                string value = label.Value.ToString();
                                                labels.Add(key, value);
                                            }
                                        }

                                        deviceList.Add(new
                                        {
                                            DeviceId = device.id.ToString(),
                                            DeviceName = device.name.ToString(),
                                            Labels = labels
                                        });
                                    }
                                }
                            }

                            result.Add(new
                            {
                                RegistryId = registryId,
                                RegistryName = registryName,
                                RegistryState = registryState,
                                Devices = deviceList
                            });
                        }
                    }
                }

                return Ok(result);
            }
            catch (HttpRequestException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Произошла ошибка при выполнении запроса: {e.Message}");
            }
        }
    }
}
