using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Node_red_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(IHttpClientFactory httpClientFactory, ILogger<RegistrationController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpPost("AddRegistrANDDevice")]
        public async Task<IActionResult> AddRegistrANDDevice(string registryName, string registryDescription, string deviceName, string deviceDescription, string labelsRegistr, string labelsDevice,string minValue, string maxValue)
        {
            _logger.LogInformation("Starting AddRegistrANDDevice with parameters: " +
                                 $"RegistryName: {registryName}, " +
                                 $"RegistryDescription: {registryDescription}, " +
                                 $"DeviceName: {deviceName}, " +
                                 $"DeviceDescription: {deviceDescription}");

            var token = "t1.9euelZqdnc2JjYmLm8nMx52Zk4vPlO3rnpWakJWKjpOejsycx82Jj8-Tz8nl9PcHc0o9-e9ZbCLl3fT3RyFIPfnvWWwi5c3n9euelZqPmJuMnZfIkZjOlpaVzs2bk-_8xeuelZqPmJuMnZfIkZjOlpaVzs2bkw.9Tf6BnrVp75L2pI8PhamFSPZY6sZYmKq47gnBhaHPzm7RWspdsKmARRckg3GXXVUYVb2SKCacClFohaukTUwCA";

            try
            {
                // Создание реестра
                var registryData = new
                {
                    name = registryName,
                    description = registryDescription,
                    folderId = "b1g913lffej8kphm0lm7",
                    password = "tESTbROKER1234",
                    labels = ParseLabels(labelsRegistr)
                };

                _logger.LogInformation("Creating registry with data: {@RegistryData}", registryData);

                var registryResponse = await SendRequestAsync("https://iot-devices.api.cloud.yandex.net/iot-devices/v1/registries", registryData, token);
                if (!registryResponse.IsSuccessStatusCode)
                {
                    var errorContent = await registryResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create registry. StatusCode: {StatusCode}, Error: {Error}",
                                    registryResponse.StatusCode, errorContent);
                    return BadRequest($"Не удалось создать реестр: {errorContent}");
                }

                // Извлечение ID реестра из ответа
                var registryId = await ExtractRegistryIdAsync(registryResponse);
                if (string.IsNullOrEmpty(registryId))
                {
                    _logger.LogError("Failed to extract registry ID from response");
                    return BadRequest("Не удалось извлечь ID реестра");
                }

                _logger.LogInformation("Registry created successfully. RegistryId: {RegistryId}", registryId);

                // Создание устройства
                var deviceData = new
                {
                    registryId = registryId,
                    name = deviceName,
                    password = "tESTbROKER1234",
                    description = deviceDescription,
                    labels = ParseLabels(labelsDevice)
                };

                _logger.LogInformation("Creating device with data: {@DeviceData}", deviceData);
   
                var deviceResponse = await SendRequestAsync("https://iot-devices.api.cloud.yandex.net/iot-devices/v1/devices", deviceData, token);


                if (!deviceResponse.IsSuccessStatusCode)
                {
                    var errorContent = await deviceResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create device. StatusCode: {StatusCode}, Error: {Error}",
                                    deviceResponse.StatusCode, errorContent);
                    return BadRequest($"Не удалось создать устройство: {errorContent}");
                }

                _logger.LogInformation("Device created successfully for registry: {RegistryId}", registryId);

                // Извлечение ID реестра из ответа
                var deviceID = await ExtractDeviceIdAsync(deviceResponse);
                if (string.IsNullOrEmpty(deviceID))
                {
                    _logger.LogError("Failed to extract registry ID from response");
                    return BadRequest("Не удалось извлечь ID реестра");
                }

                return Ok(new
                {
                    Message = "Реестр и устройство успешно созданы",
                    RegistryName = registryName,
                    RegistryId = registryId,
                    DeviceName = deviceName,
                    DeviceID = deviceID,
                    LabelsReg = ParseLabels(labelsRegistr),
                    LabelsDev = ParseLabels(labelsDevice),
                    MinValue = minValue,
                    MaxValue = maxValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing AddRegistrANDDevice");
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла внутренняя ошибка сервера");
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string url, object data, string token)
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                _logger.LogDebug("Sending request to {Url} with data: {Data}", url, jsonData);

                var response = await _httpClient.PostAsync(url, content);

                _logger.LogDebug("Received response from {Url}. StatusCode: {StatusCode}", url, response.StatusCode);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to {Url}", url);
                throw;
            }
        }

        private async Task<string> ExtractRegistryIdAsync(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Extracting registry ID from response: {Content}", content);

                // Десериализация JSON-ответа
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);

                // Извлечение ID реестра из секции metadata
                var registryId = jsonResponse?.metadata?.registryId;

                return registryId?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while extracting registry ID");
                return null;
            }
        }

        private async Task<string> ExtractDeviceIdAsync(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Extracting device ID from response: {Content}", content);

                // Десериализация JSON-ответа
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);

                // Извлечение ID устройства из секции metadata
                var deviceId = jsonResponse?.metadata?.deviceId;

                return deviceId?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while extracting device ID");
                return null;
            }
        }


        private object ParseLabels(string labels)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(labels))
                {
                    _logger.LogDebug("No labels provided");
                    return new System.Collections.Generic.Dictionary<string, string>();
                }

                var labelsDictionary = new System.Collections.Generic.Dictionary<string, string>();
                foreach (var label in labels.Split(','))
                {
                    var parts = label.Split('=');
                    if (parts.Length == 2)
                    {
                        labelsDictionary.Add(parts[0].Trim(), parts[1].Trim());
                    }
                }

                _logger.LogDebug("Parsed labels: {@Labels}", labelsDictionary);
                return labelsDictionary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while parsing labels: {Labels}", labels);
                return new System.Collections.Generic.Dictionary<string, string>();
            }
        }
    }
}