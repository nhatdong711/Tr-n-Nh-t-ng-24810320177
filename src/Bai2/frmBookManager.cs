using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Bai2
{
    public class BookModel
    {
        public string BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string CoverPath { get; set; }

        // Chỉ hiển thị tên file ảnh trên lưới
        public string CoverFileName
        {
            get { return string.IsNullOrEmpty(CoverPath) ? "" : Path.GetFileName(CoverPath); }
        }

        public decimal TotalValue
        {
            get { return Price * Quantity; }
        }
    }

    public class frmBookManager : Form
    {
        private readonly BindingList<BookModel> _books = new BindingList<BookModel>();
        private readonly BindingSource _bs = new BindingSource();
        private string _currentCoverPath = "";

        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblTotalBooks, lblTotalValue, lblClock;
        private Timer clockTimer;

        private TextBox txtBookId, txtTitle, txtAuthor, txtPrice, txtQuantity;
        private PictureBox picBookCover;
        private Button btnLoadCover, btnAdd, btnUpdate, btnDelete, btnClear;
        private DataGridView dgvBooks;
        private ErrorProvider errorProvider;

        public frmBookManager()
        {
            BuildUi();
            BuildGrid();
            SeedData();
            _bs.DataSource = _books;
            dgvBooks.DataSource = _bs;
            _bs.CurrentChanged += (s, e) => ShowCurrentBook();
            _books.ListChanged += (s, e) => UpdateStatus();
            UpdateStatus();
            ShowCurrentBook();
        }

        #region Giao diện
        private void BuildUi()
        {
            Text = "Quản Lý Tài Liệu Sách & Xuất Báo Cáo Nhà Sách";
            Font = new Font("Segoe UI", 9.5f);
            Size = new Size(1100, 640);
            StartPosition = FormStartPosition.CenterScreen;
            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            // MenuStrip
            menuStrip = new MenuStrip();
            var mFile = new ToolStripMenuItem("File");
            mFile.DropDownItems.Add("Mở file CSV", null, (s, e) => ImportCsv());
            mFile.DropDownItems.Add("Lưu file CSV", null, (s, e) => ExportCsv());
            mFile.DropDownItems.Add(new ToolStripSeparator());
            mFile.DropDownItems.Add("Thoát", null, (s, e) => Close());
            var mEdit = new ToolStripMenuItem("Edit");
            mEdit.DropDownItems.Add("Thêm", null, (s, e) => AddBook());
            mEdit.DropDownItems.Add("Xóa", null, (s, e) => DeleteBook());
            mEdit.DropDownItems.Add("Tìm kiếm", null, (s, e) => SearchBook());
            menuStrip.Items.Add(mFile);
            menuStrip.Items.Add(mEdit);

            // ToolStrip có icon
            toolStrip = new ToolStrip { ImageScalingSize = new Size(20, 20) };
            toolStrip.Items.Add(new ToolStripButton("Mở file", MakeIcon(Color.FromArgb(230, 160, 40), "O"), (s, e) => ImportCsv()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripButton("Lưu file", MakeIcon(Color.FromArgb(40, 120, 200), "S"), (s, e) => ExportCsv()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Thêm mới", MakeIcon(Color.FromArgb(40, 160, 80), "+"), (s, e) => AddBook()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });

            // StatusStrip
            statusStrip = new StatusStrip();
            lblTotalBooks = new ToolStripStatusLabel();
            lblTotalValue = new ToolStripStatusLabel { BorderSides = ToolStripStatusLabelBorderSides.Left };
            lblClock = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblTotalBooks, lblTotalValue, lblClock });
            clockTimer = new Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => lblClock.Text = "Thời gian: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            clockTimer.Start();
            lblClock.Text = "Thời gian: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // Khu vực chi tiết
            var pnlDetail = new Panel { Dock = DockStyle.Left, Width = 400, Padding = new Padding(12) };
            int y = 15;
            txtBookId = AddField(pnlDetail, "Mã sách", ref y);
            txtTitle = AddField(pnlDetail, "Tên sách", ref y);
            txtAuthor = AddField(pnlDetail, "Tác giả", ref y);
            txtPrice = AddField(pnlDetail, "Giá bán (VNĐ)", ref y);
            txtQuantity = AddField(pnlDetail, "Số lượng", ref y);

            picBookCover = new PictureBox
            {
                Location = new Point(130, y + 5),
                Size = new Size(150, 190),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };
            pnlDetail.Controls.Add(new Label { Text = "Ảnh bìa", Location = new Point(15, y + 8), AutoSize = true });
            pnlDetail.Controls.Add(picBookCover);
            btnLoadCover = new Button { Text = "Tải ảnh bìa", Location = new Point(290, y + 5), Size = new Size(95, 30) };
            btnLoadCover.Click += (s, e) => LoadCover();
            pnlDetail.Controls.Add(btnLoadCover);
            y += 210;

            btnAdd = MakeButton(pnlDetail, "Thêm", 15, y, (s, e) => AddBook());
            btnUpdate = MakeButton(pnlDetail, "Sửa", 108, y, (s, e) => UpdateBook());
            btnDelete = MakeButton(pnlDetail, "Xóa", 201, y, (s, e) => DeleteBook());
            btnClear = MakeButton(pnlDetail, "Làm mới", 294, y, (s, e) => ClearForm());

            dgvBooks = new DataGridView { Dock = DockStyle.Fill };

            Controls.Add(dgvBooks);
            Controls.Add(pnlDetail);
            Controls.Add(toolStrip);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            MainMenuStrip = menuStrip;
        }

        private TextBox AddField(Control parent, string label, ref int y)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(15, y + 3), AutoSize = true });
            var tb = new TextBox { Location = new Point(130, y), Width = 240 };
            parent.Controls.Add(tb);
            y += 36;
            return tb;
        }

        private Button MakeButton(Control parent, string text, int x, int y, EventHandler click)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(85, 32) };
            b.Click += click;
            parent.Controls.Add(b);
            return b;
        }

        private static Bitmap MakeIcon(Color color, string glyph)
        {
            var bmp = new Bitmap(20, 20);
            using (var g = Graphics.FromImage(bmp))
            using (var br = new SolidBrush(color))
            using (var f = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(br, 1, 1, 18, 18);
                var sz = g.MeasureString(glyph, f);
                g.DrawString(glyph, f, Brushes.White, (20 - sz.Width) / 2, (20 - sz.Height) / 2);
            }
            return bmp;
        }

        private void BuildGrid()
        {
            dgvBooks.AutoGenerateColumns = false;
            dgvBooks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvBooks.MultiSelect = false;
            dgvBooks.ReadOnly = true;
            dgvBooks.AllowUserToAddRows = false;
            dgvBooks.RowHeadersVisible = false;
            dgvBooks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBooks.BackgroundColor = Color.White;

            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BookId", HeaderText = "Mã sách", FillWeight = 55 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Title", HeaderText = "Tên sách", FillWeight = 130 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Author", HeaderText = "Tác giả", FillWeight = 100 });
            var colPrice = new DataGridViewTextBoxColumn { DataPropertyName = "Price", HeaderText = "Giá", FillWeight = 65 };
            colPrice.DefaultCellStyle.Format = "N0";
            colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvBooks.Columns.Add(colPrice);
            var colQty = new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "SL", FillWeight = 35 };
            colQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvBooks.Columns.Add(colQty);
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CoverFileName", HeaderText = "Ảnh bìa", FillWeight = 85 });
        }

        private void SeedData()
        {
            _books.Add(new BookModel { BookId = "S001", Title = "Lập trình C# căn bản", Author = "Phạm Hữu Khang", Price = 125000, Quantity = 12 });
            _books.Add(new BookModel { BookId = "S002", Title = "Cấu trúc dữ liệu và giải thuật", Author = "Lê Minh Hoàng", Price = 98000, Quantity = 8 });
            _books.Add(new BookModel { BookId = "S003", Title = "Mạng máy tính", Author = "Nguyễn Thúc Hải", Price = 145000, Quantity = 5 });
        }
        #endregion

        #region Hiển thị & trạng thái
        private void ShowCurrentBook()
        {
            errorProvider.Clear();
            var b = _bs.Current as BookModel;
            if (b == null) return;
            txtBookId.Text = b.BookId;
            txtTitle.Text = b.Title;
            txtAuthor.Text = b.Author;
            txtPrice.Text = b.Price.ToString("0");
            txtQuantity.Text = b.Quantity.ToString();
            _currentCoverPath = b.CoverPath ?? "";
            ShowCover(_currentCoverPath);
        }

        private void ShowCover(string path)
        {
            var old = picBookCover.Image;
            picBookCover.Image = null;
            if (old != null) old.Dispose();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            // Đọc ảnh vào bộ nhớ để không khóa file trên đĩa
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var img = Image.FromStream(fs))
            {
                picBookCover.Image = new Bitmap(img);
            }
        }

        private void UpdateStatus()
        {
            int totalQty = _books.Sum(b => b.Quantity);
            decimal totalValue = _books.Sum(b => b.TotalValue);
            lblTotalBooks.Text = string.Format("Tổng số đầu sách: {0} ({1} cuốn)", _books.Count, totalQty);
            lblTotalValue.Text = "Tổng giá trị kho: " + totalValue.ToString("N0") + " VNĐ";
        }
        #endregion

        #region Ảnh bìa (OpenFileDialog)
        private void LoadCover()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Chọn ảnh bìa sách";
                dlg.Filter = "Ảnh bìa (*.png;*.jpg)|*.png;*.jpg;*.jpeg";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _currentCoverPath = dlg.FileName;
                ShowCover(_currentCoverPath);

                // Nếu đang chọn một cuốn sách thì gán luôn đường dẫn ảnh cho đối tượng đó
                var b = _bs.Current as BookModel;
                if (b != null && b.BookId == txtBookId.Text.Trim())
                {
                    b.CoverPath = _currentCoverPath;
                    _bs.ResetCurrentItem();
                }
            }
        }
        #endregion

        #region Thêm / Sửa / Xóa / Tìm kiếm
        private bool ValidateInput(out decimal price, out int qty)
        {
            errorProvider.Clear();
            bool ok = true;
            price = 0; qty = 0;
            if (string.IsNullOrWhiteSpace(txtBookId.Text)) { errorProvider.SetError(txtBookId, "Mã sách không được để trống"); ok = false; }
            if (string.IsNullOrWhiteSpace(txtTitle.Text)) { errorProvider.SetError(txtTitle, "Tên sách không được để trống"); ok = false; }
            if (!decimal.TryParse(txtPrice.Text.Trim(), out price) || price <= 0) { errorProvider.SetError(txtPrice, "Giá phải là số dương"); ok = false; }
            if (!int.TryParse(txtQuantity.Text.Trim(), out qty) || qty < 0) { errorProvider.SetError(txtQuantity, "Số lượng phải là số nguyên không âm"); ok = false; }
            return ok;
        }

        private void AddBook()
        {
            decimal price; int qty;
            if (!ValidateInput(out price, out qty)) return;
            string id = txtBookId.Text.Trim();
            if (_books.Any(b => b.BookId.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                errorProvider.SetError(txtBookId, "Mã sách đã tồn tại");
                return;
            }
            var book = new BookModel { BookId = id, Title = txtTitle.Text.Trim(), Author = txtAuthor.Text.Trim(), Price = price, Quantity = qty, CoverPath = _currentCoverPath };
            _books.Add(book);
            _bs.Position = _books.IndexOf(book);
        }

        private void UpdateBook()
        {
            var b = _bs.Current as BookModel;
            if (b == null) return;
            decimal price; int qty;
            if (!ValidateInput(out price, out qty)) return;
            b.Title = txtTitle.Text.Trim();
            b.Author = txtAuthor.Text.Trim();
            b.Price = price;
            b.Quantity = qty;
            b.CoverPath = _currentCoverPath;
            _bs.ResetCurrentItem(); // kích hoạt ListChanged => cập nhật StatusStrip
        }

        private void DeleteBook()
        {
            var b = _bs.Current as BookModel;
            if (b == null) return;
            if (MessageBox.Show("Xóa sách [" + b.BookId + "] " + b.Title + "?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _books.Remove(b);
            }
        }

        private void ClearForm()
        {
            _bs.SuspendBinding();
            txtBookId.Text = "S" + (_books.Count + 1).ToString("000");
            txtTitle.Clear(); txtAuthor.Clear(); txtPrice.Clear(); txtQuantity.Clear();
            _currentCoverPath = "";
            ShowCover(null);
            errorProvider.Clear();
            _bs.ResumeBinding();
            txtTitle.Focus();
        }

        private void SearchBook()
        {
            string keyword = Prompt("Nhập tên sách hoặc tác giả cần tìm:", "Tìm kiếm");
            if (string.IsNullOrWhiteSpace(keyword)) return;
            keyword = keyword.Trim().ToLower();
            for (int i = 0; i < _books.Count; i++)
            {
                if (_books[i].Title.ToLower().Contains(keyword) || _books[i].Author.ToLower().Contains(keyword))
                {
                    _bs.Position = i;
                    return;
                }
            }
            MessageBox.Show("Không tìm thấy sách phù hợp.", "Tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string Prompt(string text, string caption)
        {
            using (var f = new Form { Text = caption, Size = new Size(380, 150), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, Font = Font })
            {
                var lbl = new Label { Text = text, Location = new Point(12, 12), AutoSize = true };
                var tb = new TextBox { Location = new Point(12, 38), Width = 340 };
                var ok = new Button { Text = "Tìm", DialogResult = DialogResult.OK, Location = new Point(277, 70) };
                f.Controls.AddRange(new Control[] { lbl, tb, ok });
                f.AcceptButton = ok;
                return f.ShowDialog(this) == DialogResult.OK ? tb.Text : null;
            }
        }
        #endregion

        #region CSV (SaveFileDialog / OpenFileDialog)
        private void ExportCsv()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Lưu báo cáo sách";
                dlg.Filter = "CSV file (*.csv)|*.csv";
                dlg.FileName = "BaoCaoNhaSach_" + DateTime.Now.ToString("yyyyMMdd") + ".csv";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("BookId,Title,Author,Price,Quantity,CoverPath");
                foreach (var b in _books)
                {
                    sb.AppendLine(string.Join(",", new[]
                    {
                        Csv(b.BookId), Csv(b.Title), Csv(b.Author),
                        b.Price.ToString(CultureInfo.InvariantCulture), b.Quantity.ToString(), Csv(b.CoverPath)
                    }));
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                MessageBox.Show("Đã lưu " + _books.Count + " đầu sách ra file:\n" + dlg.FileName, "Lưu file CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ImportCsv()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Mở file dữ liệu sách";
                dlg.Filter = "CSV file (*.csv)|*.csv";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var loaded = new List<BookModel>();
                int lineNo = 0, skipped = 0;
                foreach (var line in File.ReadAllLines(dlg.FileName, Encoding.UTF8))
                {
                    lineNo++;
                    if (lineNo == 1 || string.IsNullOrWhiteSpace(line)) continue; // bỏ dòng tiêu đề
                    var f = ParseCsvLine(line);
                    decimal price; int qty;
                    if (f.Count < 5 || !decimal.TryParse(f[3], NumberStyles.Any, CultureInfo.InvariantCulture, out price) || !int.TryParse(f[4], out qty))
                    {
                        skipped++;
                        continue;
                    }
                    loaded.Add(new BookModel { BookId = f[0], Title = f[1], Author = f[2], Price = price, Quantity = qty, CoverPath = f.Count > 5 ? f[5] : "" });
                }

                _books.RaiseListChangedEvents = false;
                _books.Clear();
                foreach (var b in loaded) _books.Add(b);
                _books.RaiseListChangedEvents = true;
                _books.ResetBindings(); // cập nhật lưới + StatusStrip một lần
                MessageBox.Show("Đã nạp " + loaded.Count + " đầu sách" + (skipped > 0 ? " (bỏ qua " + skipped + " dòng lỗi)" : "") + ".",
                    "Mở file CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string Csv(string s)
        {
            s = s ?? "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var cur = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else if (c == '"') inQuotes = false;
                    else cur.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { result.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(c);
            }
            result.Add(cur.ToString());
            return result;
        }
        #endregion
    }
}
