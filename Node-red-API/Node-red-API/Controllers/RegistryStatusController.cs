using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Node_red_API.Controllers
{
    [Route("api/registries")]
    [ApiController]
    public class RegistryStatusController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string YandexCloudIotApiUrl = "https://iot-devices.api.cloud.yandex.net/iot-devices/v1/registries";
        private const string NodeRedApiUrl = "http://localhost:1880"; // Update with your Node-RED URL

        public RegistryStatusController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Активирует реестр устройств
        /// </summary>
        [HttpPost("{registryId}/activate")]
        public async Task<IActionResult> ActivateRegistry(string registryId)
        {
            return await ChangeRegistryStatus(registryId, "enable");
        }

        /// <summary>
        /// Деактивирует реестр устройств
        /// </summary>
        [HttpPost("{registryId}/disable")]
        public async Task<IActionResult> DisableRegistry(string registryId)
        {
            return await ChangeRegistryStatus(registryId, "disable");
        }



        private async Task<IActionResult> ChangeRegistryStatus(string registryId, string action)
        {
            if (string.IsNullOrEmpty(registryId))
                return BadRequest("Registry ID is required");

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", "t1.9euelZqdnc2JjYmLm8nMx52Zk4vPlO3rnpWakJWKjpOejsycx82Jj8-Tz8nl9PcHc0o9-e9ZbCLl3fT3RyFIPfnvWWwi5c3n9euelZqPmJuMnZfIkZjOlpaVzs2bk-_8xeuelZqPmJuMnZfIkZjOlpaVzs2bkw.9Tf6BnrVp75L2pI8PhamFSPZY6sZYmKq47gnBhaHPzm7RWspdsKmARRckg3GXXVUYVb2SKCacClFohaukTUwCA");

                var requestUrl = $"{YandexCloudIotApiUrl}/{registryId}:{action}";

                // Для POST запроса с пустым телом
                var response = await client.PostAsync(requestUrl, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode,
                        $"Yandex Cloud API error: {errorContent}");
                }

                return Ok(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"Error connecting to Yandex Cloud API: {ex.Message}");
            }
        }
    }
}