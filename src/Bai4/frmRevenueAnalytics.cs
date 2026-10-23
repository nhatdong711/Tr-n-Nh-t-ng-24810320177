using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai4
{
    public class Transaction
    {
        public string TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string Branch { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get { return Quantity * UnitPrice; } }
    }

    /// <summary>Thông tin tiến độ gửi từ Background Thread về UI.</summary>
    public class ProgressInfo
    {
        public int Percent { get; set; }
        public string Message { get; set; }
    }

    public class frmRevenueAnalytics : Form
    {
        private static readonly string[] Branches = { "Tất cả chi nhánh", "Hà Nội", "Đà Nẵng", "TP. Hồ Chí Minh", "Cần Thơ" };
        private static readonly string[] Products = { "Laptop", "Điện thoại", "Tai nghe", "Màn hình", "Bàn phím", "Chuột", "Máy tính bảng" };

        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboBranch;
        private Button btnAnalyze, btnCancel;
        private ProgressBar pgbLoading;
        private Label lblProcessingStatus, lblSummary;
        private TextBox txtNote;
        private DataGridView dgvRevenueData;

        private CancellationTokenSource _cts;

        public frmRevenueAnalytics()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Bảng Điều Khiển Thống Kê Doanh Thu (Async/Await)";
            Font = new Font("Segoe UI", 9.5f);
            Size = new Size(1000, 620);
            StartPosition = FormStartPosition.CenterScreen;

            // Khu vực tiêu chí
            var grpCriteria = new GroupBox { Text = "Tiêu chí thống kê", Dock = DockStyle.Top, Height = 105 };
            grpCriteria.Controls.Add(new Label { Text = "Từ ngày", Location = new Point(15, 30), AutoSize = true });
            dtpFrom = new DateTimePicker { Location = new Point(80, 27), Width = 130, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = DateTime.Today.AddDays(-30) };
            grpCriteria.Controls.Add(dtpFrom);
            grpCriteria.Controls.Add(new Label { Text = "Đến ngày", Location = new Point(225, 30), AutoSize = true });
            dtpTo = new DateTimePicker { Location = new Point(295, 27), Width = 130, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = DateTime.Today };
            grpCriteria.Controls.Add(dtpTo);
            grpCriteria.Controls.Add(new Label { Text = "Chi nhánh", Location = new Point(440, 30), AutoSize = true });
            cboBranch = new ComboBox { Location = new Point(515, 27), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            cboBranch.Items.AddRange(Branches);
            cboBranch.SelectedIndex = 0;
            grpCriteria.Controls.Add(cboBranch);

            btnAnalyze = new Button { Text = "Thống Kê Doanh Thu", Location = new Point(700, 22), Size = new Size(150, 34), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnAnalyze.Click += BtnAnalyze_Click;
            grpCriteria.Controls.Add(btnAnalyze);
            btnCancel = new Button { Text = "Hủy Thao Tác", Location = new Point(860, 22), Size = new Size(110, 34), Enabled = false };
            btnCancel.Click += BtnCancel_Click;
            grpCriteria.Controls.Add(btnCancel);

            grpCriteria.Controls.Add(new Label { Text = "Ghi chú", Location = new Point(15, 68), AutoSize = true });
            txtNote = new TextBox { Location = new Point(80, 65), Width = 605 };
            grpCriteria.Controls.Add(txtNote);

            // Khu vực trạng thái
            var pnlStatus = new Panel { Dock = DockStyle.Top, Height = 62, Padding = new Padding(10, 6, 10, 6) };
            pgbLoading = new ProgressBar { Dock = DockStyle.Top, Height = 22 };
            lblProcessingStatus = new Label { Dock = DockStyle.Top, Height = 26, TextAlign = ContentAlignment.MiddleLeft, Text = "Sẵn sàng. Chọn tiêu chí và bấm [Thống Kê Doanh Thu]." };
            pnlStatus.Controls.Add(lblProcessingStatus);
            pnlStatus.Controls.Add(pgbLoading);

            // Khu vực kết quả
            dgvRevenueData = new DataGridView
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
            AddCol("TransactionId", "Mã GD", null);
            AddCol("Date", "Ngày", "dd/MM/yyyy");
            AddCol("Branch", "Chi nhánh", null);
            AddCol("Product", "Sản phẩm", null);
            AddCol("Quantity", "SL", null);
            AddCol("UnitPrice", "Đơn giá", "N0");
            AddCol("Amount", "Thành tiền", "N0");

            lblSummary = new Label { Dock = DockStyle.Bottom, Height = 32, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Padding = new Padding(10, 0, 0, 0) };

            Controls.Add(dgvRevenueData);
            Controls.Add(lblSummary);
            Controls.Add(pnlStatus);
            Controls.Add(grpCriteria);
        }

        private void AddCol(string prop, string header, string format)
        {
            var c = new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = header };
            if (format != null)
            {
                c.DefaultCellStyle.Format = format;
                if (format == "N0") c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            dgvRevenueData.Columns.Add(c);
        }

        private async void BtnAnalyze_Click(object sender, EventArgs e)
        {
            if (dtpFrom.Value.Date > dtpTo.Value.Date)
            {
                MessageBox.Show("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.", "Tiêu chí không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime from = dtpFrom.Value.Date, to = dtpTo.Value.Date;
            string branch = cboBranch.SelectedIndex == 0 ? null : cboBranch.Text;

            _cts = new CancellationTokenSource();
            SetBusy(true);
            dgvRevenueData.DataSource = null;
            lblSummary.Text = "";

            // Progress<T> tự động đưa callback về UI Thread
            var progress = new Progress<ProgressInfo>(p =>
            {
                pgbLoading.Value = p.Percent;
                lblProcessingStatus.Text = p.Message + " (" + p.Percent + "%)";
            });

            try
            {
                // Đẩy toàn bộ công việc tổng hợp xuống Background Thread
                List<Transaction> data = await Task.Run(() => LoadRevenue(from, to, branch, progress, _cts.Token), _cts.Token);

                dgvRevenueData.DataSource = data;
                decimal total = data.Sum(t => t.Amount);
                lblSummary.Text = string.Format("Tổng: {0:N0} giao dịch  |  Doanh thu: {1:N0} VNĐ", data.Count, total);
                lblProcessingStatus.Text = "Hoàn tất thống kê lúc " + DateTime.Now.ToString("HH:mm:ss") + ".";
            }
            catch (OperationCanceledException)
            {
                pgbLoading.Value = 0;
                lblProcessingStatus.Text = "Đã hủy thao tác!";
                MessageBox.Show("Đã hủy thao tác!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetBusy(false);
                _cts.Dispose();
                _cts = null;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (_cts != null)
            {
                btnCancel.Enabled = false;
                lblProcessingStatus.Text = "Đang hủy...";
                _cts.Cancel();
            }
        }

        private void SetBusy(bool busy)
        {
            btnAnalyze.Enabled = !busy;
            btnCancel.Enabled = busy;
            UseWaitCursor = false; // không khóa giao diện
        }

        /// <summary>
        /// Chạy trên Background Thread: giả lập truy vấn CSDL và tổng hợp dữ liệu (~5 giây).
        /// Không chạm vào bất kỳ Control nào ở đây.
        /// </summary>
        private static List<Transaction> LoadRevenue(DateTime from, DateTime to, string branch,
            IProgress<ProgressInfo> progress, CancellationToken token)
        {
            var rnd = new Random(from.DayOfYear * 31 + to.DayOfYear);
            var result = new List<Transaction>();
            const int steps = 50;
            int days = (to - from).Days + 1;

            for (int step = 1; step <= steps; step++)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(100); // 50 x 100ms = 5 giây

                // Mỗi bước sinh một lô giao dịch giả lập
                for (int i = 0; i < 6; i++)
                {
                    string b = branch ?? Branches[1 + rnd.Next(Branches.Length - 1)];
                    if (branch == null || b == branch)
                    {
                        result.Add(new Transaction
                        {
                            TransactionId = "GD" + (result.Count + 1).ToString("0000"),
                            Date = from.AddDays(rnd.Next(days)),
                            Branch = b,
                            Product = Products[rnd.Next(Products.Length)],
                            Quantity = 1 + rnd.Next(5),
                            UnitPrice = (5 + rnd.Next(300)) * 100000m
                        });
                    }
                }

                string msg = step < 20 ? "Đang truy xuất dữ liệu giao dịch..."
                           : step < 40 ? "Đang tổng hợp doanh thu theo chi nhánh..."
                           : "Đang sắp xếp kết quả...";
                progress.Report(new ProgressInfo { Percent = step * 100 / steps, Message = msg });
            }
            return result.OrderBy(t => t.Date).ToList();
        }
    }
}
