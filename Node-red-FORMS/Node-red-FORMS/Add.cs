using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Newtonsoft.Json;

namespace Node_red_FORMS
{
    public partial class Add : Form
    {
        private Form1 _form1;
        public class ApiResponse
        {
            public string Message { get; set; }
            public string RegistryName { get; set; }
            public string RegistryId { get; set; }
            public string DeviceName { get; set; }
            public string DeviceID { get; set; }
            public Labels LabelsReg { get; set; }
            public Labels LabelsDev { get; set; }
            public string MinValue { get; set; }
            public string MaxValue { get; set; }
        }

        public class Labels
        {
            public string Location { get; set; }
            public string Work { get; set; } // Только для LabelsDev
        }
        private ApiResponse _lastApiResponse;
        public Add(Form1 form1)
        {
            InitializeComponent();
            _form1 = form1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _form1.Show();
            this.Close();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            string min = null;
            string max = null;
            // Получаем выбранный элемент из ComboBox
            string selectedItem = comboBox1.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Выберите датчик из списка!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Сопоставляем выбранный элемент с его типом данных
            var comboBoxItems = new List<(string Item, string DataType)>
            {
                ("Счетчик посетителей", "int"),
                ("Датчик температуры упрощенный", "int"),
                ("Датчик влажности упрощенный", "int"),
                ("Датчик температуры", "float"),
                ("Датчик освещености", "float"),
                ("Датчик влажности", "float"),
                ("Датчик газа", "float"),
                ("Датчик света", "float"),
                ("Лампочка", "bool"),
                ("Выключатель", "bool"),
                ("Датчик протечки воды", "bool")
            };

            // Ищем выбранный элемент в списке и получаем его тип
            var selectedSensor = comboBoxItems.FirstOrDefault(x => x.Item == selectedItem);

            if (selectedSensor.Item == null)
            {
                MessageBox.Show("Неизвестный датчик!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string sensorType = selectedSensor.DataType;
            if (sensorType == "int" || sensorType == "float")
            {

            }
            else
            {
                min = "0";
                max = "0";
            }

            // Получаем остальные данные из TextBox'ов
            string RegistrName = textBoxRegistrName.Text;
            string RegistrDescription = textBoxRegistrDescription.Text;
            string DeviceName = textBoxDeviceName.Text;
            string DeviceDescription = textBoxDeviceDescription.Text;
            string House = textBoxHouse.Text;
            string State = textBoxState.Text;
            string Room = textBoxRoom.Text;
            if (min != "0" && max != "0")
            {
                min = textBoxMIN.Text;
                max = textBoxMAX.Text;
            }

            // Формируем сообщение с учетом типа датчика
            string message =
                "registryName: " + RegistrName + "\n" +
                "registryDescription: " + RegistrDescription + "\n" +
                "deviceName: " + DeviceName + "\n" +
                "deviceDescription: " + DeviceDescription + "\n" +
                "sensorType: " + sensorType + "\n" +  // Добавляем тип датчика
                "labelsRegistr:" + "location=" + House + "\n" +
                "labelsDevice:" + "work=" + State + ",location=" + Room + "\n" +
                "min value" + min + "\n" +
                "max value" + max;

            MessageBox.Show(message, "Регистрационные данные", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Формируем URL с параметрами
            string apiUrl = $"https://localhost:7101/api/Registration/AddRegistrANDDevice?" +
                $"registryName={Uri.EscapeDataString(RegistrName)}" +
                $"&registryDescription={Uri.EscapeDataString(RegistrDescription)}" +
                $"&deviceName={Uri.EscapeDataString(DeviceName)}" +
                $"&deviceDescription={Uri.EscapeDataString(DeviceDescription)}" +
                $"&labelsRegistr=location%3D{Uri.EscapeDataString(House)}" +
                $"&labelsDevice=work%3D{Uri.EscapeDataString(State)}%2Clocation%3D{Uri.EscapeDataString(Room)}" +
                $"&minValue={min}" +
                $"&maxValue={max}";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                    // 1. Вызываем первый API
                    HttpResponseMessage response = await client.PostAsync(apiUrl, null);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        _lastApiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseContent);

                        MessageBox.Show(_lastApiResponse.Message, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // 2. Теперь вызываем второй API ConvertToMainJson
                        await CallConvertToMainJsonApi(_lastApiResponse, selectedSensor.DataType, 1);

                        // Вызов загрузки в Node-red после второго API
                        HttpResponseMessage nodeRedResponse1 = await client.PostAsync("https://localhost:7101/api/NodeRedFlow/addFlowFromTemplate", null);

                        if (nodeRedResponse1.IsSuccessStatusCode)
                        {
                            string nodeRedContent1 = await nodeRedResponse1.Content.ReadAsStringAsync();
                            MessageBox.Show("Загрузка в Node-RED успешна: " + nodeRedContent1, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка загрузки в Node-RED: {nodeRedResponse1.StatusCode}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        // 3. Теперь вызываем третий API ConvertToMainJson
                        await CallConvertToMainJsonApi(_lastApiResponse, selectedSensor.DataType, 2);

                        // Вызов загрузки в Node-red после третьего API
                        HttpResponseMessage nodeRedResponse2 = await client.PostAsync("https://localhost:7101/api/NodeRedFlow/addFlowFromTemplate", null);

                        if (nodeRedResponse2.IsSuccessStatusCode)
                        {
                            string nodeRedContent2 = await nodeRedResponse2.Content.ReadAsStringAsync();
                            MessageBox.Show("Загрузка в Node-RED успешна: " + nodeRedContent2, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка загрузки в Node-RED: {nodeRedResponse2.StatusCode}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка первого API: {response.StatusCode}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CallConvertToMainJsonApi(ApiResponse apiResponse, string deviceType, int numberFile)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // URL для второго API
                    string convertApiUrl = $"https://localhost:7101/ConvertToMainJson?NumberFile={numberFile}&TypeDevice={deviceType}";

                    // Настройка заголовков
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                    // Сериализуем тело запроса (ответ первого API)
                    string jsonContent = JsonConvert.SerializeObject(apiResponse);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Отправляем POST-запрос
                    HttpResponseMessage response = await client.PostAsync(convertApiUrl, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Второй API успешно выполнен!\n", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка второго API: {response.StatusCode}\n{await response.Content.ReadAsStringAsync()}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вызове второго API: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void Add_Load(object sender, EventArgs e)
        {
            var comboBoxItems = new List<(string Item, string DataType)>
            {
                ("Счетчик посетителей", "int"),
                ("Датчик температуры упрощенный", "int"),
                ("Датчик влажности упрощенный", "int"),
                ("Датчик температуры", "float"),
                ("Датчик освещености", "float"),
                ("Датчик влажности", "float"),
                ("Датчик газа", "float"),
                ("Датчик света", "float"),
                ("Лампочка", "bool"),
                ("Выключатель", "bool"),
                ("Датчик протечки воды", "bool")
            };

            foreach (var (item, dataType) in comboBoxItems)
            {
                comboBox1.Items.Add($"{item}");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {   
            // Получаем выбранный элемент из ComboBox
            string selectedItem = comboBox1.SelectedItem?.ToString();

            // Сопоставляем выбранный элемент с его типом данных
            var comboBoxItems = new List<(string Item, string DataType)>
            {
                ("Счетчик посетителей", "int"),
                ("Датчик температуры упрощенный", "int"),
                ("Датчик влажности упрощенный", "int"),
                ("Датчик температуры", "float"),
                ("Датчик освещености", "float"),
                ("Датчик влажности", "float"),
                ("Датчик газа", "float"),
                ("Датчик света", "float"),
                ("Лампочка", "bool"),
                ("Выключатель", "bool"),
                ("Датчик протечки воды", "bool")
            };

            // Ищем выбранный элемент в списке и получаем его тип
            var selectedSensor = comboBoxItems.FirstOrDefault(x => x.Item == selectedItem);

            if (selectedSensor.Item == null)
            {
                MessageBox.Show("Неизвестный датчик!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string sensorType = selectedSensor.DataType;
            if (sensorType == "int" || sensorType == "float")
            {
                label10.Visible = true;
                label9.Visible = true;
                textBoxMIN.Visible = true;
                textBoxMAX.Visible = true;
            }
            else
            {
                label10.Visible = false;
                label9.Visible = false;
                textBoxMIN.Visible = false;
                textBoxMAX.Visible = false;
            }
        }
    }
}
