using System;
using System.Windows.Forms;
using BDCOM.OLT.Manager.Core;
using BDCOM.OLT.Manager.UI;

namespace BDCOM.OLT.Manager
{
    public partial class ExtraFunctionsDialog : Form
    {
        private readonly MainForm _mainForm;
        private readonly TelnetClient? _telnetClient;

        public ExtraFunctionsDialog(MainForm mainForm)
        {
            _mainForm = mainForm;
            _telnetClient = mainForm.GetTelnetClient();

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Дополнительные функции";
            this.Size = new System.Drawing.Size(520, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Кнопка перезагрузки OLT
            var btnRebootOlt = new Button
            {
                Text = "Перезагрузить OLT",
                Location = new System.Drawing.Point(60, 50),
                Size = new System.Drawing.Size(400, 55),
                BackColor = System.Drawing.Color.Crimson,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold)
            };
            btnRebootOlt.Click += BtnRebootOlt_Click;

            // Кнопка сохранения конфигурации
            var btnSaveConfig = new Button
            {
                Text = "Сохранить конфигурацию (write all)",
                Location = new System.Drawing.Point(60, 130),
                Size = new System.Drawing.Size(400, 55),
                BackColor = System.Drawing.Color.DarkGreen,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 10f)
            };
            btnSaveConfig.Click += BtnSaveConfig_Click;

            // Кнопка закрытия
            var btnClose = new Button
            {
                Text = "Закрыть",
                Location = new System.Drawing.Point(60, 320),
                Size = new System.Drawing.Size(400, 45),
                Font = new System.Drawing.Font("Segoe UI", 10f)
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(btnRebootOlt);
            this.Controls.Add(btnSaveConfig);
            this.Controls.Add(btnClose);
        }

        private async void BtnRebootOlt_Click(object? sender, EventArgs e)
        {
            if (_telnetClient == null)
            {
                MessageBox.Show("Нет активного подключения к OLT", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Первое подтверждение
            var result = MessageBox.Show(
                "ВНИМАНИЕ!\n\nOLT будет перезагружен!\n\nЭто действие приведёт к отключению ВСЕХ абонентов!\n\nПродолжить?",
                "Критическое действие — Перезагрузка OLT",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            // Второе подтверждение (ввод слова "АДЕКВАТНЫЙ")
            var secondConfirm = new SecondConfirmDialog("OLT", "перезагрузку OLT");
            if (secondConfirm.ShowDialog(this) != DialogResult.OK) return;

            // Выполняем команду
            var (output, success) = await _telnetClient.ExecuteAsync("reload");

            if (success)
                MessageBox.Show("Команда на перезагрузку OLT отправлена.\nОборудование будет перезагружено.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Не удалось отправить команду перезагрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void BtnSaveConfig_Click(object? sender, EventArgs e)
        {
            if (_telnetClient == null)
            {
                MessageBox.Show("Нет активного подключения", "Ошибка");
                return;
            }

            var (output, success) = await _telnetClient.ExecuteAsync("write all");

            if (success)
                MessageBox.Show("Команда 'write all' выполнена успешно.\nКонфигурация сохранена.", "Успешно");
            else
                MessageBox.Show("Не удалось выполнить команду 'write all'.", "Ошибка");
        }
    }
}