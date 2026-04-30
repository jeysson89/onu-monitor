using System;
using System.Drawing;
using System.Windows.Forms;

namespace BDCOM.OLT.Manager.UI
{
    public partial class ResultsDialog : Form
    {
        public ResultsDialog(string title, string content)
        {
            InitializeComponent(title, content, null);
        }

        public ResultsDialog(string title, string content, Action repeatAction)
        {
            InitializeComponent(title, content, repeatAction);
        }

        private void InitializeComponent(string title, string content, Action repeatAction)
        {
            this.Text = title;
            this.Size = new Size(740, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Если это "Сигналы EPON" — делаем таблицу
            if (title.Contains("Оптика порта") && content.Contains("ONU ID"))
            {
                var listView = new ListView
                {
                    Location = new Point(20, 20),
                    Size = new Size(690, 460),
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    Font = new Font("Consolas", 10f)
                };

                listView.Columns.Add("ONU ID", 130);
                listView.Columns.Add("RxPower (dBm)", 200);

                var lines = content.Split('\n');
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var item = new ListViewItem(parts[0].Trim());
                        item.SubItems.Add(parts[1].Trim());
                        listView.Items.Add(item);
                    }
                }

                this.Controls.Add(listView);
            }
            else
            {
                // Для всех остальных окон (Оптика ONU, Статус LAN, MAC и т.д.) — обычный текст
                var txtContent = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new Font("Consolas", 10f),
                    Location = new Point(20, 20),
                    Size = new Size(690, 460),
                    Text = content
                };
                this.Controls.Add(txtContent);
            }

            // Кнопки
            var btnRepeat = new Button
            {
                Text = "Повторить запрос",
                Location = new Point(260, 500),
                Size = new Size(150, 40),
                Visible = repeatAction != null
            };
            if (repeatAction != null)
                btnRepeat.Click += (s, e) => repeatAction();

            var btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(430, 500),
                Size = new Size(110, 40)
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(btnRepeat);
            this.Controls.Add(btnClose);
        }
    }
}