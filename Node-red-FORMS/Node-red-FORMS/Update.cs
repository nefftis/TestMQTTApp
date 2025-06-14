using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Node_red_FORMS.Update;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Node_red_FORMS
{
    public partial class Update : Form
    {
        private Form1 _form1;
        private List<Registry> _registries = new List<Registry>();
        private List<Device> _devices = new List<Device>();

        public Update(Form1 form1)
        {
            InitializeComponent();
            _form1 = form1;
            CreateColumns();
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
        }
        // Метод для выполнения API-запроса
        private async Task PerformApiRequest(string apiUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, null);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("API-запрос успешно выполнен!\nОтвет сервера: " + responseContent,
                                        "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Ошибка выполнения API-запроса. Код ответа: " + response.StatusCode,
                                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошла ошибка при отправке API-запроса: " + ex.Message,
                                    "Ошибка соединения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Проверяем, что нажатие произошло по кнопке и не заголовке
            if (e.ColumnIndex == dataGridView1.Columns["ToggleColumn"].Index && e.RowIndex >= 0)
            {
                // Получение RegistryId из Tag строки
                string registryId = dataGridView1.Rows[e.RowIndex].Tag as string;

                // Выводим RegistryId
                MessageBox.Show($"Registry ID: {registryId}", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Получаем значение состояния из соответствующей ячейки
                string stateValue = dataGridView1.Rows[e.RowIndex].Cells["StateColumn"].Value.ToString();

                if (stateValue.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    string apiUrlDisableRegistry = "https://localhost:7101/api/registries/are3qc4h8vm0bkm91il8/disable";
                    await PerformApiRequest(apiUrlDisableRegistry);

                    string apiUrlDisableFlow = $"https://localhost:7101/api/NodeRedFlow/disableFlow?flowId=bbf6d7c6.937b78%20{registryId}";
                    await PerformApiRequest(apiUrlDisableFlow);
                }
                else if (stateValue.Equals("DISABLE", StringComparison.OrdinalIgnoreCase))
                {
                    string apiUrlActivateRegistry = "https://localhost:7101/api/registries/are3qc4h8vm0bkm91il8/activate";
                    await PerformApiRequest(apiUrlActivateRegistry);

                    string apiUrlEnableFlow = $"https://localhost:7101/api/NodeRedFlow/enableFlow?flowId=bbf6d7c6.937b78%20{registryId}";
                    await PerformApiRequest(apiUrlEnableFlow);
                }
                else
                {
                    MessageBox.Show("Неизвестный статус: " + stateValue, "Информация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                await UpdateDataGridView();
                MessageBox.Show($"Registry ID: {registryId}", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _form1.Show();
            this.Close();
        }
        private void CreateColumns()
        {
            dataGridView1.Columns.Clear();

            // Добавляем столбцы в DataGridView
            dataGridView1.Columns.Add("RegistryNameColumn", "Registry Name");
            dataGridView1.Columns.Add("DeviceNameColumn", "Device Name");
            dataGridView1.Columns.Add("LabelsColumn", "Labels");
            dataGridView1.Columns.Add("StateColumn", "State");
        }

        private async void Update_Load(object sender, EventArgs e)
        {
            await UpdateDataGridView();
            // Добавляем обработчик события для изменения выбранного элемента в comboBox1
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += ComboBox2_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged += ComboBox3_SelectedIndexChanged;
        }


        private async Task UpdateDataGridView()
        {
            string apiUrl = "https://localhost:7101/api/RegistrAndDevice";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Выполняем GET-запрос к API
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    // Читаем содержание ответа
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Десериализуем JSON-ответ
                    _registries = JsonConvert.DeserializeObject<List<Registry>>(responseBody);

                    // Очищаем текущие данные DataGridView и ComboBox
                    dataGridView1.Rows.Clear();
                    comboBox1.Items.Clear();
                    comboBox2.Items.Clear();
                    comboBox3.Items.Clear();

                    // Добавляем столбец с кнопкой, если его еще нет
                    if (dataGridView1.Columns["ToggleColumn"] == null)
                    {
                        DataGridViewButtonColumn toggleColumn = new DataGridViewButtonColumn();
                        toggleColumn.Name = "ToggleColumn";
                        toggleColumn.HeaderText = "Состояние";
                        toggleColumn.Text = "Переключить";
                        toggleColumn.UseColumnTextForButtonValue = true;
                        dataGridView1.Columns.Add(toggleColumn);
                    }

                    // Подготовка данных для отображения
                    foreach (var registry in _registries)
                    {
                        // Добавляем имя реестра в ComboBox1
                        comboBox1.Items.Add(registry.RegistryName);

                        foreach (var device in registry.Devices)
                        {
                            // Добавляем устройство в список устройств
                            _devices.Add(device);

                            // Добавляем имя устройства в ComboBox2
                            comboBox2.Items.Add(device.DeviceName);

                            // Добавляем метки устройства в ComboBox3
                            foreach (var labelKey in device.Labels.Keys)
                            {
                                if (!comboBox3.Items.Contains(labelKey))
                                {
                                    comboBox3.Items.Add(labelKey);
                                }
                            }

                            // Получаем строку меток
                            string labels = string.Join(", ", device.Labels.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

                            int rowIndex = dataGridView1.Rows.Add(registry.RegistryName, device.DeviceName, labels, registry.RegistryState, "Переключить");

                            // Сохраняем RegistryId в Tag строки
                            dataGridView1.Rows[rowIndex].Tag = registry.RegistryId;
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    MessageBox.Show($"Произошла ошибка при выполнении запроса: {e.Message}");
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Произошла ошибка: {e.Message}");
                }
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedRegistryName = comboBox1.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(selectedRegistryName))
            {
                // Находим выбранный реестр
                var selectedRegistry = _registries.FirstOrDefault(r => r.RegistryName == selectedRegistryName);
                if (selectedRegistry != null)
                {
                    // Очищаем comboBox2 и добавляем устройства выбранного реестра
                    comboBox2.Items.Clear();

                    foreach (var device in selectedRegistry.Devices)
                    {
                        comboBox2.Items.Add(device.DeviceName);
                    }
                }
            }
        }
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedDeviceName = comboBox2.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(selectedDeviceName))
            {
                // Находим реестр, в котором находится выбранное устройство
                var selectedRegistry = _registries.FirstOrDefault(r => r.Devices.Any(d => d.DeviceName == selectedDeviceName));

                if (selectedRegistry != null)
                {
                    // Находим выбранное устройство
                    var selectedDevice = selectedRegistry.Devices.FirstOrDefault(d => d.DeviceName == selectedDeviceName);

                    if (selectedDevice != null)
                    {
                        // Очищаем comboBox3 и добавляем названия меток выбранного устройства
                        comboBox3.Items.Clear();

                        foreach (var labelKey in selectedDevice.Labels.Keys)
                        {
                            comboBox3.Items.Add(labelKey);
                        }

                        // Добавляем элемент "Создать новый" последним
                        comboBox3.Items.Add("Создать новый");
                    }
                }
            }
        }
        private void ComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedItem?.ToString() == "Создать новый")
            {
                // Устанавливаем свойство Visible в true для label5 и textbox2
                label5.Visible = true;
                textBox2.Visible = true;
            }
            else
            {
                // Устанавливаем свойство Visible в false, если выбран другой элемент
                label5.Visible = false;
                textBox2.Visible = false;
            }
        }
        public class Registry
        {
            public string RegistryId { get; set; }
            public string RegistryName { get; set; }
            public string RegistryState {  get; set; }
            public List<Device> Devices { get; set; }
        }

        public class Device
        {
            public string DeviceId { get; set; }
            public string DeviceName { get; set; }
            public Dictionary<string, string> Labels { get; set; }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            string selectedDeviceId = GetSelectedDeviceId();

            Device selectedDevice = _devices.FirstOrDefault(device => device.DeviceId == selectedDeviceId);

            if (selectedDevice != null)
            {
                string newLabelName;
                if (comboBox3.SelectedItem?.ToString() == "Создать новый")
                {
                    newLabelName = textBox2.Text;
                }
                else
                {
                    newLabelName = comboBox3.SelectedItem.ToString();
                }

                string newValue = textBox1.Text;

                Dictionary<string, string> labels = new Dictionary<string, string>();

                if (selectedDevice.Labels != null)
                {
                    foreach (var label in selectedDevice.Labels)
                    {
                        labels[label.Key] = label.Value;
                    }
                }

                labels[newLabelName] = newValue;

                var jsonStructure = new
                {
                    labels
                };

                string jsonContent = JsonConvert.SerializeObject(jsonStructure);

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string url = $"https://localhost:7101/api/Device/{selectedDeviceId}";

                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                        {
                            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                        };

                        HttpResponseMessage response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Запрос успешно выполнен.");
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка: {response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show($"Устройство с ID {selectedDeviceId} не найдено или метки отсутствуют.");
            }
        }



        private string GetSelectedDeviceId()
        {
            // Найдем выбранный объект Device на основе выбранного элемента ComboBox3
            string selectedDeviceName = comboBox2.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedDeviceName))
            {
                foreach (var device in _devices)
                {
                    if (device.DeviceName == selectedDeviceName)
                    {
                        return device.DeviceId;
                    }
                }
            }

            return string.Empty; // Если ничего не найдено
        }
    }
}
