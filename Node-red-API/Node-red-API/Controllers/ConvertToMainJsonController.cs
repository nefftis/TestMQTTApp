using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node_red_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConvertToMainJsonController : ControllerBase
    {
        private readonly ILogger<ConvertToMainJsonController> _logger;

        public ConvertToMainJsonController(ILogger<ConvertToMainJsonController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult ConvertToMainJson([FromBody] InputModel input, string NumberFile,string TypeDevice)
        {
            try
            {
                string filePath = null;
                if (TypeDevice == "int")
                {
                    if (NumberFile == "1")
                    {
                        filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "int", "node-red-template.json");
                    }
                    else if (NumberFile == "2")
                    {
                        filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "int", "node-red-secondary.json");
                    }
                    else
                    {
                        return NotFound("JSON файл не найден.");
                    }
                }
                else if (TypeDevice == "float")
                {
                    if (NumberFile == "1")
                    {
                        filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "float", "node-red-template.json");
                    }
                    else if (NumberFile == "2")
                    {
                        filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "float", "node-red-secondary.json");
                    }
                    else
                    {
                        return NotFound("JSON файл не найден.");
                    }
                }
                else if (TypeDevice == "bool")
                {
                    if (NumberFile == "1")
                    {
                        filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "bool", "node-red-template.json");
                    }
                    else if (NumberFile == "2")
                    {
                        filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "bool", "node-red-secondary.json");
                    }
                    else
                    {
                        return NotFound("JSON файл не найден.");
                    }
                }
                else
                {
                    return StatusCode(500, "Произошла ошибка такого типа устройств нет");
                }
                // Проверка существования файла
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("JSON файл не найден.");
                }

                // Чтение JSON файла
                var jsonData = System.IO.File.ReadAllText(filePath);
                JToken jsonObject = JToken.Parse(jsonData);

                // Замена значений по плейсхолдерам
                jsonData = jsonData
                    .Replace("{{MESSAGE}}", input.Message)
                    .Replace("{{REGISTRY-NAME}}", input.RegistryName)
                    .Replace("{{REGISTRY-ID}}", input.RegistryId)
                    .Replace("{{Zdanie}}", input.LabelsReg.Location)
                    .Replace("{{ROMM}}", input.LabelsDev.Location)
                    .Replace("{{DEVICE-NAME}}", input.DeviceName)
                    .Replace("{{MIN}}", input.MinValue)
                    .Replace("{{MAX}}", input.MaxValue)
                    .Replace("{{DEVICE-ID}}", input.DeviceID);

                jsonObject = JToken.Parse(jsonData);

                // Обновление значения location в labels
                if (jsonObject.Type == JTokenType.Object && jsonObject["labels"] != null)
                {
                    ((JObject)jsonObject)["labels"]["location"] = input.LabelsDev.Location;
                }

                // Путь для нового JSON файла
                string newFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "new-node-red-template.json");

                // Запись обновленного JSON в новый файл
                System.IO.File.WriteAllText(newFilePath, jsonObject.ToString());

                // Возврат нового JSON файла
                return PhysicalFile(newFilePath, "application/json", "new-node-red-template.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении JSON файла.");
                return StatusCode(500, "Произошла ошибка при обновлении JSON файла.");
            }
        }
    }

    public class InputModel
    {
        public string Message { get; set; }
        public string RegistryName { get; set; }
        public string RegistryId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceID { get; set; }

        public labelsRegistr LabelsReg { get; set; }
        public labelsDevice LabelsDev { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }
    }

    public class labelsRegistr
    {
        public string Location { get; set; }
    }

    public class labelsDevice
    {
        public string Work { get; set; }
        public string Location { get; set; }
    }
}
