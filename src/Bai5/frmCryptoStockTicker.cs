using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai5
{
    public class PriceLog
    {
        public int No { get; set; }
        public string Time { get; set; }
        public decimal Price { get; set; }
        public decimal Change { get; set; }
        public string ChangePercent { get; set; }
        public string Trend { get; set; }
    }

    public class frmCryptoStockTicker : Form
    {
        private Button btnStart, btnPause;
        private Label lblBTCPrice, lblChange, lblTitle;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblLastUpdate, lblSyncCount, lblState;
        private DataGridView dgvHistoryLog;
        private readonly BindingList<PriceLog> _logs = new BindingList<PriceLog>();

        private CancellationTokenSource _cts;
        private decimal _lastPrice = 67500m;
        private int _syncCount;
        private readonly Random _rnd = new Random();

        public frmCryptoStockTicker()
        {
            BuildUi();
            FormClosing += (s, e) => { if (_cts != null) _cts.Cancel(); };
        }

        private void BuildUi()
        {
            Text = "Giám Sát Tỷ Giá BTC/USD Thời Gian Thực";
            Font = new Font("Segoe UI", 9.5f);
            Size = new Size(820, 600);
            StartPosition = FormStartPosition.CenterScreen;

            // Khu vực điều khiển + hiển thị giá
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 150, Width = 800, BackColor = Color.FromArgb(24, 28, 38) };
            lblTitle = new Label { Text = "BTC / USD", ForeColor = Color.Silver, Font = new Font("Segoe UI", 12f, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
            lblBTCPrice = new Label { Text = _lastPrice.ToString("N2"), ForeColor = Color.White, Font = new Font("Consolas", 34f, FontStyle.Bold), Location = new Point(15, 42), AutoSize = true };
            lblChange = new Label { Text = "Chưa bắt đầu giám sát", ForeColor = Color.Silver, Font = new Font("Segoe UI", 11f), Location = new Point(22, 105), AutoSize = true };
            btnStart = new Button { Text = "Bắt đầu Giám sát", Location = new Point(560, 30), Size = new Size(220, 40), BackColor = Color.FromArgb(22, 163, 74), ForeColor = Color.White, UseVisualStyleBackColor = false };
            btnPause = new Button { Text = "Tạm dừng", Location = new Point(560, 80), Size = new Size(220, 40), BackColor = Color.FromArgb(220, 38, 38), ForeColor = Color.White, UseVisualStyleBackColor = false, Enabled = false };
            btnStart.Click += BtnStart_Click;
            btnPause.Click += BtnPause_Click;
            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblBTCPrice, lblChange, btnStart, btnPause });

            // Nhật ký lịch sử
            dgvHistoryLog = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            AddCol("No", "Lượt", null, 40);
            AddCol("Time", "Thời gian", null, 80);
            AddCol("Price", "Giá BTC (USD)", "N2", 110);
            AddCol("Change", "Biến động", "+#,##0.00;-#,##0.00;0.00", 90);
            AddCol("ChangePercent", "%", null, 70);
            AddCol("Trend", "Xu hướng", null, 70);
            dgvHistoryLog.DataSource = _logs;
            dgvHistoryLog.CellFormatting += DgvHistoryLog_CellFormatting;

            // StatusStrip
            statusStrip = new StatusStrip();
            lblState = new ToolStripStatusLabel("Trạng thái: Dừng");
            lblLastUpdate = new ToolStripStatusLabel("Cập nhật cuối: --:--:--") { BorderSides = ToolStripStatusLabelBorderSides.Left };
            lblSyncCount = new ToolStripStatusLabel("Số lượt đồng bộ thành công: 0") { BorderSides = ToolStripStatusLabelBorderSides.Left };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblState, lblLastUpdate, lblSyncCount });

            Controls.Add(dgvHistoryLog);
            Controls.Add(pnlTop);
            Controls.Add(statusStrip);
        }

        private void AddCol(string prop, string header, string format, int weight)
        {
            var c = new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = header, FillWeight = weight };
            if (format != null)
            {
                c.DefaultCellStyle.Format = format;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            dgvHistoryLog.Columns.Add(c);
        }

        private void DgvHistoryLog_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _logs.Count) return;
            var log = _logs[e.RowIndex];
            string col = dgvHistoryLog.Columns[e.ColumnIndex].DataPropertyName;
            if (col == "Change" || col == "ChangePercent" || col == "Trend")
                e.CellStyle.ForeColor = log.Change > 0 ? Color.Green : log.Change < 0 ? Color.Red : Color.Black;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            btnStart.Enabled = false;
            btnPause.Enabled = true;
            lblState.Text = "Trạng thái: Đang giám sát";
            CancellationToken token = _cts.Token;

            // Vòng lặp vô hạn chạy trên Background Thread
            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(2000, token);
                        // Sinh giá mới (random walk ±1.5%)
                        decimal pct = (decimal)(_rnd.NextDouble() * 3.0 - 1.5) / 100m;
                        decimal newPrice = Math.Round(_lastPrice * (1 + pct), 2);
                        UpdateUi(newPrice);
                    }
                }
                catch (OperationCanceledException) { /* dừng an toàn */ }
            }, token);
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            lblState.Text = "Trạng thái: Tạm dừng";
        }

        /// <summary>
        /// Được gọi từ Background Thread. Kiểm tra InvokeRequired và đẩy việc cập nhật
        /// Control về Main UI Thread bằng this.Invoke để tránh lỗi Cross-thread.
        /// </summary>
        private void UpdateUi(decimal newPrice)
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired)
            {
                try { Invoke(new Action<decimal>(UpdateUi), newPrice); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { } // form đang đóng
                return;
            }

            // Từ đây trở xuống chắc chắn đang chạy trên UI Thread
            decimal change = newPrice - _lastPrice;
            decimal percent = _lastPrice == 0 ? 0 : change / _lastPrice * 100m;
            _lastPrice = newPrice;
            _syncCount++;

            lblBTCPrice.Text = newPrice.ToString("N2");
            lblBTCPrice.ForeColor = change > 0 ? Color.LimeGreen : change < 0 ? Color.Red : Color.White;
            lblChange.ForeColor = lblBTCPrice.ForeColor;
            lblChange.Text = string.Format("{0}{1:N2} USD ({0}{2:0.00}%)", change >= 0 ? "+" : "-", Math.Abs(change), Math.Abs(percent));

            lblLastUpdate.Text = "Cập nhật cuối: " + DateTime.Now.ToString("HH:mm:ss");
            lblSyncCount.Text = "Số lượt đồng bộ thành công: " + _syncCount;

            _logs.Insert(0, new PriceLog
            {
                No = _syncCount,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Price = newPrice,
                Change = change,
                ChangePercent = (percent >= 0 ? "+" : "") + percent.ToString("0.00") + "%",
                Trend = change > 0 ? "▲ Tăng" : change < 0 ? "▼ Giảm" : "= Đứng"
            });
            if (_logs.Count > 200) _logs.RemoveAt(_logs.Count - 1);
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmCryptoStockTicker());
        }
    }
}
