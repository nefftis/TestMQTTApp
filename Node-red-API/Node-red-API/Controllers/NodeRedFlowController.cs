using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Node_red_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NodeRedFlowController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public NodeRedFlowController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<dynamic> GetFlow(string flowId)
        {
            var url = $"http://localhost:1880/flow/{flowId}";

            // Выполнение GET-запроса
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                // Сериализация JSON в динамический объект
                return JsonConvert.DeserializeObject<dynamic>(responseData);
            }

            throw new Exception($"Error retrieving flow: {response.StatusCode}");
        }

        [HttpPut("disableFlow")]
        public async Task<IActionResult> DisableFlow([FromQuery] string flowId)
        {
            // Получение объекта потока
            var flow = await GetFlow(flowId);

            // Изменение параметра disabled на true
            flow["disabled"] = true;

            var url = $"http://localhost:1880/flow/{flowId}";

            // Сериализация обновленного объекта обратно в JSON
            var json = JsonConvert.SerializeObject(flow);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Отправка PUT-запроса
            var response = await _httpClient.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                return Ok("Flow disabled successfully.");
            }

            // Если ошибка - возвращаем её описание
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        [HttpPut("enableFlow")]
        public async Task<IActionResult> EnableFlow([FromQuery] string flowId)
        {
            // Получение объекта потока
            var flow = await GetFlow(flowId);

            // Изменение параметра disabled на false
            flow["disabled"] = false;

            var url = $"http://localhost:1880/flow/{flowId}";

            // Сериализация объекта в JSON
            var json = JsonConvert.SerializeObject(flow);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Отправка PUT-запроса
            var response = await _httpClient.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                return Ok("Flow enabled successfully.");
            }

            // Если ошибка - возвращаем её описание
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        // Метод для добавления нового потока
        [HttpPost("addFlowFromTemplate")]
        public async Task<IActionResult> AddFlowFromTemplate()
        {
            // Путь для нового JSON файла
            string newFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "new-node-red-template.json");

            if (!System.IO.File.Exists(newFilePath))
            {
                return NotFound("JSON файл не найден.");
            }

            string flowJsonContent;

            // Чтение JSON из файла
            try
            {
                flowJsonContent = await System.IO.File.ReadAllTextAsync(newFilePath);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Ошибка при чтении файла: {ex.Message}");
            }

            var url = "http://localhost:1880/flow";

            // Подготовка контента для отправки
            var content = new StringContent(flowJsonContent, Encoding.UTF8, "application/json");

            // Отправка POST-запроса для добавления нового потока
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                // Получение ответа от Node-RED
                var responseData = await response.Content.ReadAsStringAsync();
                return Ok(JsonConvert.DeserializeObject<dynamic>(responseData));
            }

            // Если ошибка - возвращаем её описание
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        // Метод для удаления потока
        [HttpDelete("deleteFlow/{flowId}")]
        public async Task<IActionResult> DeleteFlow(string flowId)
        {
            // Проверяем, что flowId не пустой
            if (string.IsNullOrEmpty(flowId))
            {
                return BadRequest("Идентификатор потока не может быть пустым.");
            }

            var url = $"http://localhost:1880/flow/{flowId}";

            // Отправка DELETE-запроса для удаления потока
            var response = await _httpClient.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                // Получение ответа от Node-RED
                var responseData = await response.Content.ReadAsStringAsync();
                return Ok(JsonConvert.DeserializeObject<dynamic>(responseData));
            }

            // Если ошибка - возвращаем её описание
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}
