using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Node_red_FORMS
{
    public partial class Browse : Form
    {
        private Form1 _form1;
        public Browse(Form1 form1)
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
            string url = "http://127.0.0.1:1880/ui/#!/0?socketid=XjJUEjVnQaT5rRUiAAAN";
            await webView21.EnsureCoreWebView2Async(); // Инициализация
            webView21.CoreWebView2.Navigate(url);
        }

        private async void Browse_Load(object sender, EventArgs e)
        {
            // Инициализация WebView2
            await webView21.EnsureCoreWebView2Async();

            // Устанавливаем начальную HTML-страницу с сообщением "Ожидание..."
            webView21.CoreWebView2.NavigateToString("<html><body style='font-family: Arial; text-align: center; margin-top: 50px;'><h1>Ожидание...</h1></body></html>");
        }
    }
}
