using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using BDCOM.OLT.Manager.Config;
using BDCOM.OLT.Manager.Core;
using BDCOM.OLT.Manager.Enums;
using BDCOM.OLT.Manager.Models;
using BDCOM.OLT.Manager.Parsers;
using BDCOM.OLT.Manager.UI;

namespace BDCOM.OLT.Manager
{
    public partial class MainForm : Form
    {
        private List<Device> _devices = new();
        private Device? _currentDevice;
        private TelnetClient? _telnetClient;
        private Logger _logger;

        private FlowLayoutPanel _devicesPanel = null!;
        private RichTextBox _logBox = null!;

        private TextBox txtSlot = null!, txtPort = null!, txtOnu = null!, txtMac = null!;

        private Button btnAddDevice = null!, btnEditDevice = null!, btnDeleteDevice = null!;
        private Button btnConnect = null!, btnDisconnect = null!, btnExtraFunctions = null!;
        private Button btnGetMac = null!, btnGetStatus = null!, btnGetOptical = null!, btnGetPortOptical = null!;
        private Button btnSetSpeed = null!, btnRebootOnu = null!, btnDeleteOnu = null!;

        public MainForm()
        {
            AppConfig.EnsureDirs();
            LoadDevices();
            InitializeComponent();
            _logger = new Logger(_logBox);
            _logger.Info("Приложение запущено");
            RefreshDeviceButtons();
            UpdateButtonsState();
        }

        private void InitializeComponent()
        {
            this.Text = $"{AppConfig.AppName} v{AppConfig.Version}";
            this.Size = new Size(1000, 880);
            this.MinimumSize = new Size(990, 750);
            this.BackColor = Theme.BG_PRIMARY;
            this.Font = new Font("Segoe UI", 10f);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ==================== Устройства ====================
            var gbDevices = new GroupBox 
            { 
                Text = "Устройства", 
                Location = new Point(12, 12), 
                Size = new Size(900, 160), 
                BackColor = Theme.BG_SECONDARY, 
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
            };

            _devicesPanel = new FlowLayoutPanel 
            { 
                Location = new Point(15, 30), 
                Size = new Size(900, 75), 
                AutoScroll = true 
            };
            gbDevices.Controls.Add(_devicesPanel);

            btnAddDevice = CreateButton("+ Добавить", Color.LimeGreen, new Point(15, 115), 120, AddDevice);
            btnEditDevice = CreateButton("✎ Изменить", Color.FromArgb(0, 122, 204), new Point(145, 115), 120, EditDevice);
            btnDeleteDevice = CreateButton("🗑 Удалить", Color.IndianRed, new Point(275, 115), 120, DeleteDevice);
            btnConnect = CreateButton("Подключиться", Color.LimeGreen, new Point(405, 115), 135, async () => await Connect());
            btnDisconnect = CreateButton("Отключиться", Color.Crimson, new Point(550, 115), 135, Disconnect);
            btnExtraFunctions = CreateButton("⚡ Доп. функции", Color.MediumPurple, new Point(695, 115), 150, OpenExtraFunctions);

            gbDevices.Controls.Add(btnAddDevice);
            gbDevices.Controls.Add(btnEditDevice);
            gbDevices.Controls.Add(btnDeleteDevice);
            gbDevices.Controls.Add(btnConnect);
            gbDevices.Controls.Add(btnDisconnect);
            gbDevices.Controls.Add(btnExtraFunctions);
            this.Controls.Add(gbDevices);

            // Параметры ONU
            var gbParams = new GroupBox 
            { 
                Text = "Параметры ONU", 
                Location = new Point(12, 185), 
                Size = new Size(900, 90), 
                BackColor = Theme.BG_SECONDARY, 
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
            };

            int x = 20;
            gbParams.Controls.Add(new Label { Text = "Слот:", Location = new Point(x, 35), AutoSize = true });
            txtSlot = new TextBox { Text = "0", Location = new Point(x + 50, 32), Width = 60, ReadOnly = true };
            x += 130;

            gbParams.Controls.Add(new Label { Text = "Порт:", Location = new Point(x, 35), AutoSize = true });
            txtPort = new TextBox { Location = new Point(x + 50, 32), Width = 80 };
            x += 150;

            gbParams.Controls.Add(new Label { Text = "ONU:", Location = new Point(x, 35), AutoSize = true });
            txtOnu = new TextBox { Location = new Point(x + 50, 32), Width = 80 };
            x += 150;

            gbParams.Controls.Add(new Label { Text = "MAC:", Location = new Point(x, 35), AutoSize = true });
            txtMac = new TextBox { Location = new Point(x + 50, 32), Width = 200 };
            x += 270;

            var btnFindMac = CreateButton("Найти по MAC", Color.Teal, new Point(x, 30), 160, SearchByMac);

            gbParams.Controls.Add(txtSlot);
            gbParams.Controls.Add(txtPort);
            gbParams.Controls.Add(txtOnu);
            gbParams.Controls.Add(txtMac);
            gbParams.Controls.Add(btnFindMac);
            this.Controls.Add(gbParams);

            // Операции
            var gbOps = new GroupBox 
            { 
                Text = "Операции", 
                Location = new Point(12, 290), 
                Size = new Size(900, 155), 
                BackColor = Theme.BG_SECONDARY, 
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
            };

            int opX = 20;
            btnGetMac = CreateOperationButton("MAC-адреса", Color.DodgerBlue, opX, 35, GetMac); opX += 165;
            btnGetStatus = CreateOperationButton("Статус LAN", Color.MediumSeaGreen, opX, 35, GetStatus); opX += 165;
            btnGetOptical = CreateOperationButton("Оптика ONU", Color.Teal, opX, 35, GetOptical); opX += 165;
            btnGetPortOptical = CreateOperationButton("Сигналы EPON", Color.Teal, opX, 35, GetPortOptical); opX += 165;

            opX = 20;
            btnSetSpeed = CreateOperationButton("1 Гбит/с", Color.Orange, opX, 90, SetSpeed); opX += 165;
            btnRebootOnu = CreateOperationButton("Перезагрузить ONU", Color.Crimson, opX, 90, RebootOnu); opX += 165;
            btnDeleteOnu = CreateOperationButton("Удалить ONU", Color.Crimson, opX, 90, DeleteOnu);

            gbOps.Controls.Add(btnGetMac);
            gbOps.Controls.Add(btnGetStatus);
            gbOps.Controls.Add(btnGetOptical);
            gbOps.Controls.Add(btnGetPortOptical);
            gbOps.Controls.Add(btnSetSpeed);
            gbOps.Controls.Add(btnRebootOnu);
            gbOps.Controls.Add(btnDeleteOnu);
            this.Controls.Add(gbOps);

            // Журнал
            var gbLog = new GroupBox 
            { 
                Text = "Журнал операций", 
                Location = new Point(12, 460), 
                Size = new Size(900, 390), 
                BackColor = Theme.BG_SECONDARY, 
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom 
            };

            _logBox = new RichTextBox 
            { 
                Location = new Point(15, 25), 
                Size = new Size(890, 350), 
                ReadOnly = true, 
                Font = new Font("Consolas", 9.75f), 
                BackColor = Color.White, 
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom 
            };
            gbLog.Controls.Add(_logBox);
            this.Controls.Add(gbLog);
        }

        private Button CreateButton(string text, Color color, Point loc, int width, Action action)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(width, 35),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => action();
            return btn;
        }

        private Button CreateOperationButton(string text, Color color, int x, int y, Action action)
        {
            return CreateButton(text, color, new Point(x, y), 155, action);
        }

        // ===================== Основная логика =====================
        private void RefreshDeviceButtons()
        {
            _devicesPanel.Controls.Clear();
            foreach (var dev in _devices)
            {
                var btn = new Button
                {
                    Text = dev.Name,
                    Width = 140,
                    Height = 38,
                    Margin = new Padding(6),
                    Tag = dev,
                    BackColor = dev == _currentDevice ? Color.DodgerBlue : Color.LightGray,
                    ForeColor = dev == _currentDevice ? Color.White : Color.Black
                };
                btn.Click += (s, e) => DeviceButtonClicked(dev);
                _devicesPanel.Controls.Add(btn);
            }
        }

        private async void DeviceButtonClicked(Device dev)
        {
            _currentDevice = dev;
            RefreshDeviceButtons();
            if (AppConfig.AutoConnect)
                await Connect();
        }

        public async Task Connect()
        {
            if (_currentDevice == null) return;
            _telnetClient = new TelnetClient(_currentDevice, _logger);
            _telnetClient.OnStateChanged += s => Invoke(() => UpdateButtonsState());
            await _telnetClient.ConnectAsync();
        }

        public void Disconnect()
        {
            _telnetClient?.Disconnect();
            _telnetClient = null;
            UpdateButtonsState();
        }

        private void UpdateButtonsState()
        {
            bool connected = _telnetClient?.State == ConnectionState.Connected;
            btnDisconnect.Enabled = connected;
            btnExtraFunctions.Enabled = connected;
            btnGetMac.Enabled = connected;
            btnGetStatus.Enabled = connected;
            btnGetOptical.Enabled = connected;
            btnGetPortOptical.Enabled = connected;
            btnSetSpeed.Enabled = connected;
            btnRebootOnu.Enabled = connected;
            btnDeleteOnu.Enabled = connected;
        }

        public ONUParams? GetCurrentParams()
        {
            string port = txtPort.Text.Trim();
            string onu = txtOnu.Text.Trim();
            if (string.IsNullOrEmpty(port) || string.IsNullOrEmpty(onu))
            {
                MessageBox.Show("Укажите порт и ONU ID", "Ошибка");
                return null;
            }
            return new ONUParams(txtSlot.Text, port, onu);
        }

        public string GetCurrentPort() => txtPort.Text.Trim();
        public TelnetClient? GetTelnetClient() => _telnetClient;

        // ===================== Оптика ONU =====================
        private async void GetOptical()
        {
            var p = GetCurrentParams();
            if (p == null) return;

            _logger.Info($"Запрос оптических параметров ONU {p.FullId}");

            string[] cmds = 
            {
                $"show epon optical-transceiver-diagnosis interface epon {p.Slot}/{p.Port}:{p.OnuId}",
                $"optical-transceiver-diagnosis interface epon {p.Slot}/{p.Port}:{p.OnuId}"
            };

            string raw = "";
            bool success = false;

            foreach (var cmd in cmds)
            {
                _logger.Info($"Выполнение: {cmd}");
                (raw, success) = await _telnetClient!.ExecuteAsync(cmd);
                if (success) break;
                await Task.Delay(300);
            }

            if (success && !string.IsNullOrWhiteSpace(raw))
            {
                string clean = raw.Replace("\b", "").Replace("^", "").Trim();
                clean = Regex.Replace(clean, @"show epon optical-transceiver-diagnosis.*", "", RegexOptions.IgnoreCase);
                clean = Regex.Replace(clean, @"^.*OLT_.*#$", "", RegexOptions.Multiline);

                int rxIndex = clean.IndexOf("RxPower", StringComparison.OrdinalIgnoreCase);
                if (rxIndex >= 0)
                    clean = clean.Substring(rxIndex);

                new ResultsDialog("Оптические параметры ONU", clean).ShowDialog(this);
            }
            else
            {
                MessageBox.Show("Не удалось получить данные оптических параметров", "Ошибка");
            }
        }

        // ===================== Сигналы EPON =====================
        private async void GetPortOptical()
        {
        string port = GetCurrentPort();
    if (string.IsNullOrEmpty(port))
    {
        MessageBox.Show("Укажите порт", "Ошибка");
        return;
    }

    string[] cmds = 
    {
        $"show epon optical-transceiver-diagnosis interface epon 0/{port}",
        $"optical-transceiver-diagnosis interface epon 0/{port}"
    };

    string rawOutput = "";
    bool success = false;

    _logger.Info($"Запрос оптики порта 0/{port} (может занять время)...");

    foreach (var cmd in cmds)
    {
        (rawOutput, success) = await _telnetClient!.ExecuteAsync(cmd, true);
        if (success && rawOutput.Length > 50) 
            break;
        await Task.Delay(500);
    }

    if (success && !string.IsNullOrWhiteSpace(rawOutput))
    {
        var list = OpticalParser.ParsePortOptical(rawOutput);

        if (list.Count > 0)
        {
            string tableText = "ONU ID\tRxPower (dBm)\n" +
                               "------\t-------------\n" +
                               string.Join("\n", list.Select(x => $"{x.OnuId}\t{x.RxPower}"));

            // Передаём действие "Повторить"
            new ResultsDialog($"Оптика порта 0/{port} ({list.Count} ONU)", tableText, () => GetPortOptical()).ShowDialog(this);
        }
        else
        {
            new ResultsDialog($"Оптика порта 0/{port}", "Не удалось разобрать данные.\n\n" + rawOutput).ShowDialog(this);
        }
    }
    else
    {
        MessageBox.Show("Не удалось получить данные по оптике порта", "Ошибка");
            }
        }

        // ===================== Остальные методы (сокращённо) =====================
        private async void GetMac()
        {
            var p = GetCurrentParams(); if (p == null) return;
            var (output, _) = await _telnetClient!.ExecuteAsync($"show mac address-table interface ePON {p.Slot}/{p.Port}:{p.OnuId}");
            var macs = MacParser.FindAll(output);
            if (macs.Count > 0)
                new MacResultsDialog(macs).ShowDialog(this);
            else
                MessageBox.Show("MAC-адреса не найдены", "Результат");
        }

        private async void SearchByMac()
        {
            string macInput = txtMac.Text.Trim();
            if (string.IsNullOrEmpty(macInput)) return;
            string normalized = MacParser.Normalize(macInput);
            _logger.Info($"Поиск по MAC: {normalized}");
            var (output, _) = await _telnetClient!.ExecuteAsync($"show mac address-table {normalized}");
            new ResultsDialog($"Результаты по MAC {normalized}", output).ShowDialog(this);
        }

        private async void GetStatus()
        {
            var p = GetCurrentParams(); if (p == null) return;
            var (output, _) = await _telnetClient!.ExecuteAsync($"show epon interface epon {p.Slot}/{p.Port}:{p.OnuId} onu port 1 state");
            new ResultsDialog("Статус LAN", output).ShowDialog(this);
        }

        private async void SetSpeed() { /* можно оставить как было */ }
        private async void RebootOnu() { /* можно оставить как было */ }
        private async void DeleteOnu() { /* можно оставить как было */ }

        private void OpenExtraFunctions()
        {
            if (_telnetClient == null || _telnetClient.State != ConnectionState.Connected)
            {
                MessageBox.Show("Нет активного подключения", "Ошибка");
                return;
            }
            new ExtraFunctionsDialog(this).ShowDialog();
        }

        private void AddDevice() { using var dlg = new DeviceDialog(); if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultDevice != null) { _devices.Add(dlg.ResultDevice); SaveDevices(); RefreshDeviceButtons(); } }
        private void EditDevice() { if (_currentDevice == null) return; using var dlg = new DeviceDialog(_currentDevice); if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultDevice != null) { int idx = _devices.FindIndex(d => d.Id == _currentDevice!.Id); if (idx >= 0) _devices[idx] = dlg.ResultDevice; _currentDevice = dlg.ResultDevice; SaveDevices(); RefreshDeviceButtons(); } }
        private void DeleteDevice() { if (_currentDevice == null || MessageBox.Show($"Удалить {_currentDevice.Name}?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return; _devices.Remove(_currentDevice); _currentDevice = null; SaveDevices(); RefreshDeviceButtons(); }

        private void LoadDevices()
        {
            try
            {
                if (System.IO.File.Exists(AppConfig.DevicesFile))
                {
                    string json = System.IO.File.ReadAllText(AppConfig.DevicesFile);
                    _devices = System.Text.Json.JsonSerializer.Deserialize<List<Device>>(json) ?? new();
                }
            }
            catch { }
        }

        private void SaveDevices()
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(_devices, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(AppConfig.DevicesFile, json);
            }
            catch (Exception ex) { _logger.Error($"Ошибка сохранения: {ex.Message}"); }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _telnetClient?.Disconnect();
            base.OnFormClosing(e);
        }
    }
}