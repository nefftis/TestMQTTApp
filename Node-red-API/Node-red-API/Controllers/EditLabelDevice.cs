using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Node_red_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;

        public DeviceController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpPatch("{deviceId}")]
        public async Task<IActionResult> UpdateLabels(
            string deviceId,
            [FromBody] LabelUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return BadRequest("deviceId обязателен");
            if (request == null || request.Labels == null || request.Labels.Count == 0)
                return BadRequest("Отсутствуют labels в запросе");

            var iamToken = "t1.9euelZqdnc2JjYmLm8nMx52Zk4vPlO3rnpWakJWKjpOejsycx82Jj8-Tz8nl9PcHc0o9-e9ZbCLl3fT3RyFIPfnvWWwi5c3n9euelZqPmJuMnZfIkZjOlpaVzs2bk-_8xeuelZqPmJuMnZfIkZjOlpaVzs2bkw.9Tf6BnrVp75L2pI8PhamFSPZY6sZYmKq47gnBhaHPzm7RWspdsKmARRckg3GXXVUYVb2SKCacClFohaukTUwCA";
            var ycEndpoint = $"https://iot-devices.api.cloud.yandex.net/iot-devices/v1/devices/{deviceId}";
            var client = _clientFactory.CreateClient();

            var bodyObject = new
            {
                labels = request.Labels,
                updateMask = "labels"
            };

            var jsonBody = JsonSerializer.Serialize(bodyObject);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Patch, ycEndpoint)
            {
                Content = content
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", iamToken);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(httpRequest);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, $"Ошибка HTTP запроса: {ex.Message}");
            }

            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, result);

            return Ok(result);
        }
    }

    public class LabelUpdateRequest
    {
        public Dictionary<string, string> Labels { get; set; }
    }
}
