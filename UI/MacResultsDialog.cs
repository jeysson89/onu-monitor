using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BDCOM.OLT.Manager.UI
{
    public partial class MacResultsDialog : Form
    {
        private readonly List<string> _macs;

        public MacResultsDialog(List<string> macs)
        {
            _macs = macs ?? new List<string>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Найденные MAC-адреса";
            this.Size = new Size(520, 480);           // уже и компактнее
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var listView = new ListView
            {
                Location = new Point(20, 20),
                Size = new Size(460, 340),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Consolas", 10f)
            };

            listView.Columns.Add("MAC-адрес", 280);
            listView.Columns.Add("Действие", 140);

            foreach (var mac in _macs)
            {
                var item = new ListViewItem(mac);
                var copyBtn = new Button { Text = "Копировать", Width = 110, Height = 28 };
                copyBtn.Click += (s, e) => 
                {
                    Clipboard.SetText(mac);
                    MessageBox.Show($"MAC {mac} скопирован в буфер обмена", "Копировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                // Используем SubItem для имитации кнопки
                item.SubItems.Add("Копировать");
                listView.Items.Add(item);

                // Простой способ — копировать по двойному клику
                listView.DoubleClick += (s, e) =>
                {
                    if (listView.SelectedItems.Count > 0)
                    {
                        Clipboard.SetText(listView.SelectedItems[0].Text);
                        MessageBox.Show("MAC скопирован в буфер обмена", "Успешно");
                    }
                };
            }

            var btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(200, 390),
                Size = new Size(100, 35)
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(listView);
            this.Controls.Add(btnClose);
        }
    }
}