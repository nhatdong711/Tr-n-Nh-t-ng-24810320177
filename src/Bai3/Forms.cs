using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai3
{
    // ===================== FORM CHA MDI =====================
    public class frmHospitalMain : Form
    {
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblDoctorOnDuty, lblMessage;
        private ToolStripProgressBar progressBar;
        private bool _isLoading;

        public frmHospitalMain()
        {
            Text = "HỆ THỐNG QUẢN LÝ BỆNH VIỆN - MDI";
            Font = new Font("Segoe UI", 9.5f);
            Size = new Size(1150, 700);
            StartPosition = FormStartPosition.CenterScreen;
            IsMdiContainer = true;

            menuStrip = new MenuStrip();

            var mSystem = new ToolStripMenuItem("Hệ thống");
            mSystem.DropDownItems.Add("Đăng xuất", null, (s, e) => Logout());
            mSystem.DropDownItems.Add("Nạp lại dữ liệu", null, async (s, e) => await ReloadDataAsync());
            mSystem.DropDownItems.Add(new ToolStripSeparator());
            mSystem.DropDownItems.Add("Thoát", null, (s, e) => Close());

            var mManage = new ToolStripMenuItem("Quản lý");
            mManage.DropDownItems.Add("Hồ sơ Bệnh nhân", null, (s, e) => OpenChild<frmPatientProfiles>());
            mManage.DropDownItems.Add("Danh mục Bác sĩ", null, (s, e) => OpenChild<frmDoctorSchedule>());
            mManage.DropDownItems.Add("Danh mục Khoa/Phòng", null, (s, e) => OpenChild<frmDepartments>());

            var mWindow = new ToolStripMenuItem("Cửa sổ");
            mWindow.DropDownItems.Add("Xếp tầng (Cascade)", null, (s, e) => LayoutMdi(MdiLayout.Cascade));
            mWindow.DropDownItems.Add("Ngang (Tile Horizontal)", null, (s, e) => LayoutMdi(MdiLayout.TileHorizontal));
            mWindow.DropDownItems.Add("Dọc (Tile Vertical)", null, (s, e) => LayoutMdi(MdiLayout.TileVertical));
            mWindow.DropDownItems.Add(new ToolStripSeparator());
            mWindow.DropDownItems.Add("Đóng tất cả", null, (s, e) => CloseAllChildren());

            menuStrip.Items.AddRange(new ToolStripItem[] { mSystem, mManage, mWindow });
            menuStrip.MdiWindowListItem = mWindow; // liệt kê các form con đang mở
            MainMenuStrip = menuStrip;

            statusStrip = new StatusStrip();
            lblDoctorOnDuty = new ToolStripStatusLabel("Bác sĩ trực: BS. Trần Nhật Đông - Khoa Nội");
            lblMessage = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight, Text = "Sẵn sàng" };
            progressBar = new ToolStripProgressBar { Width = 200, Visible = true };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblDoctorOnDuty, lblMessage, progressBar });

            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
        }

        /// <summary>
        /// Mở form con: duyệt MdiChildren, nếu đã mở thì Activate(), chưa có thì tạo mới.
        /// </summary>
        private void OpenChild<T>() where T : Form, new()
        {
            foreach (Form child in MdiChildren)
            {
                if (child is T)
                {
                    if (child.WindowState == FormWindowState.Minimized)
                        child.WindowState = FormWindowState.Normal;
                    child.Activate();
                    lblMessage.Text = "Form \"" + child.Text + "\" đã mở - chuyển lên trên cùng";
                    return;
                }
            }
            var f = new T();
            f.MdiParent = this;
            f.Show();
            lblMessage.Text = "Đã mở: " + f.Text;
        }

        private void CloseAllChildren()
        {
            foreach (Form child in MdiChildren) child.Close();
            lblMessage.Text = "Đã đóng tất cả cửa sổ";
        }

        private void Logout()
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Đăng xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            CloseAllChildren();
            lblDoctorOnDuty.Text = "Bác sĩ trực: (chưa đăng nhập)";
            lblMessage.Text = "Đã đăng xuất";
        }

        /// <summary>
        /// Nạp lại dữ liệu bằng async/await: tác vụ nặng chạy trên Task.Run, UI vẫn thao tác được.
        /// </summary>
        private async Task ReloadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            progressBar.Value = 0;
            var progress = new Progress<int>(p =>
            {
                progressBar.Value = p;
                lblMessage.Text = "Đang nạp lại dữ liệu bệnh án... " + p + "%";
            });

            await Task.Run(() =>
            {
                IProgress<int> rp = progress;
                for (int i = 1; i <= 100; i++)
                {
                    System.Threading.Thread.Sleep(50); // giả lập truy xuất CSDL ~5 giây
                    rp.Report(i);
                }
            });

            foreach (Form child in MdiChildren)
            {
                var r = child as IReloadable;
                if (r != null) r.ReloadData();
            }
            lblMessage.Text = "Nạp lại dữ liệu hoàn tất lúc " + DateTime.Now.ToString("HH:mm:ss");
            _isLoading = false;
        }
    }

    public interface IReloadable { void ReloadData(); }

    // ===================== CÁC MODEL =====================
    public class Patient
    {
        public string PatientId { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Diagnosis { get; set; }
        public DateTime AdmissionDate { get; set; }
    }

    public class DutySchedule
    {
        public string Doctor { get; set; }
        public string Department { get; set; }
        public string Shift { get; set; }
        public DateTime Date { get; set; }
    }

    public class Department
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Room { get; set; }
        public int Beds { get; set; }
    }

    // ===================== HELPER TẠO LƯỚI =====================
    internal static class GridHelper
    {
        public static DataGridView Create()
        {
            return new DataGridView
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
        }

        public static void Col(DataGridView g, string prop, string header, string format = null)
        {
            var c = new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = header };
            if (format != null) c.DefaultCellStyle.Format = format;
            g.Columns.Add(c);
        }
    }

    // ===================== FORM CON 1: HỒ SƠ BỆNH NHÂN =====================
    public class frmPatientProfiles : Form, IReloadable
    {
        private readonly BindingList<Patient> _patients = new BindingList<Patient>();
        private DataGridView dgv;
        private TextBox txtName, txtAge, txtDiagnosis;
        private ComboBox cboGender;

        public frmPatientProfiles()
        {
            Text = "Hồ sơ Bệnh nhân";
            Size = new Size(640, 380);

            var top = new Panel { Dock = DockStyle.Top, Height = 78 };
            top.Controls.Add(new Label { Text = "Họ tên", Location = new Point(8, 10), AutoSize = true });
            txtName = new TextBox { Location = new Point(70, 7), Width = 170 };
            top.Controls.Add(txtName);
            top.Controls.Add(new Label { Text = "Tuổi", Location = new Point(250, 10), AutoSize = true });
            txtAge = new TextBox { Location = new Point(290, 7), Width = 45 };
            top.Controls.Add(txtAge);
            top.Controls.Add(new Label { Text = "Giới", Location = new Point(345, 10), AutoSize = true });
            cboGender = new ComboBox { Location = new Point(385, 7), Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
            cboGender.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGender.SelectedIndex = 0;
            top.Controls.Add(cboGender);
            top.Controls.Add(new Label { Text = "Chẩn đoán", Location = new Point(8, 45), AutoSize = true });
            txtDiagnosis = new TextBox { Location = new Point(90, 42), Width = 365 };
            top.Controls.Add(txtDiagnosis);
            var btnAdd = new Button { Text = "Thêm BN", Location = new Point(470, 6), Size = new Size(90, 30) };
            btnAdd.Click += BtnAdd_Click;
            top.Controls.Add(btnAdd);
            var btnDel = new Button { Text = "Xóa BN", Location = new Point(470, 41), Size = new Size(90, 30) };
            btnDel.Click += (s, e) =>
            {
                var p = dgv.CurrentRow == null ? null : dgv.CurrentRow.DataBoundItem as Patient;
                if (p != null && MessageBox.Show("Xóa hồ sơ " + p.FullName + "?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    _patients.Remove(p);
            };
            top.Controls.Add(btnDel);

            dgv = GridHelper.Create();
            GridHelper.Col(dgv, "PatientId", "Mã BN");
            GridHelper.Col(dgv, "FullName", "Họ tên");
            GridHelper.Col(dgv, "Age", "Tuổi");
            GridHelper.Col(dgv, "Gender", "Giới");
            GridHelper.Col(dgv, "Diagnosis", "Chẩn đoán");
            GridHelper.Col(dgv, "AdmissionDate", "Ngày nhập viện", "dd/MM/yyyy");
            dgv.DataSource = _patients;

            Controls.Add(dgv);
            Controls.Add(top);
            ReloadData();
        }

        public void ReloadData()
        {
            _patients.Clear();
            _patients.Add(new Patient { PatientId = "BN001", FullName = "Nguyễn Văn Nam", Age = 45, Gender = "Nam", Diagnosis = "Tăng huyết áp", AdmissionDate = DateTime.Today.AddDays(-3) });
            _patients.Add(new Patient { PatientId = "BN002", FullName = "Lê Thị Mai", Age = 32, Gender = "Nữ", Diagnosis = "Viêm phổi", AdmissionDate = DateTime.Today.AddDays(-1) });
            _patients.Add(new Patient { PatientId = "BN003", FullName = "Phạm Quốc Huy", Age = 60, Gender = "Nam", Diagnosis = "Tiểu đường type 2", AdmissionDate = DateTime.Today });
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            int age;
            if (string.IsNullOrWhiteSpace(txtName.Text) || !int.TryParse(txtAge.Text, out age) || age <= 0)
            {
                MessageBox.Show("Vui lòng nhập họ tên và tuổi hợp lệ.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int next = _patients.Count == 0 ? 1 : _patients.Max(p => int.Parse(p.PatientId.Substring(2))) + 1;
            _patients.Add(new Patient
            {
                PatientId = "BN" + next.ToString("000"),
                FullName = txtName.Text.Trim(),
                Age = age,
                Gender = cboGender.Text,
                Diagnosis = txtDiagnosis.Text.Trim(),
                AdmissionDate = DateTime.Today
            });
            txtName.Clear(); txtAge.Clear(); txtDiagnosis.Clear();
        }
    }

    // ===================== FORM CON 2: LỊCH TRỰC BÁC SĨ =====================
    public class frmDoctorSchedule : Form, IReloadable
    {
        private readonly BindingList<DutySchedule> _items = new BindingList<DutySchedule>();

        public frmDoctorSchedule()
        {
            Text = "Lịch trực Bác sĩ";
            Size = new Size(560, 300);
            var dgv = GridHelper.Create();
            GridHelper.Col(dgv, "Date", "Ngày trực", "dd/MM/yyyy");
            GridHelper.Col(dgv, "Shift", "Ca trực");
            GridHelper.Col(dgv, "Doctor", "Bác sĩ");
            GridHelper.Col(dgv, "Department", "Khoa");
            dgv.DataSource = _items;
            Controls.Add(dgv);
            ReloadData();
        }

        public void ReloadData()
        {
            _items.Clear();
            string[] doctors = { "BS. Trần Nhật Đông", "BS. Nguyễn Thị Lan", "BS. Hoàng Minh Tuấn", "BS. Vũ Thu Trang" };
            string[] depts = { "Khoa Nội", "Khoa Ngoại", "Khoa Nhi", "Cấp cứu" };
            string[] shifts = { "Sáng (7h-13h)", "Chiều (13h-19h)", "Đêm (19h-7h)" };
            for (int i = 0; i < 8; i++)
            {
                _items.Add(new DutySchedule { Date = DateTime.Today.AddDays(i / 3), Shift = shifts[i % 3], Doctor = doctors[i % 4], Department = depts[(i + 1) % 4] });
            }
        }
    }

    // ===================== FORM CON 3: DANH MỤC KHOA/PHÒNG =====================
    public class frmDepartments : Form
    {
        public frmDepartments()
        {
            Text = "Danh mục Khoa/Phòng";
            Size = new Size(500, 260);
            var list = new BindingList<Department>
            {
                new Department { Code = "NOI", Name = "Khoa Nội tổng hợp", Room = "Tầng 2 - Nhà A", Beds = 40 },
                new Department { Code = "NGOAI", Name = "Khoa Ngoại", Room = "Tầng 3 - Nhà A", Beds = 35 },
                new Department { Code = "NHI", Name = "Khoa Nhi", Room = "Tầng 1 - Nhà B", Beds = 30 },
                new Department { Code = "CC", Name = "Khoa Cấp cứu", Room = "Tầng 1 - Nhà A", Beds = 20 }
            };
            var dgv = GridHelper.Create();
            GridHelper.Col(dgv, "Code", "Mã khoa");
            GridHelper.Col(dgv, "Name", "Tên khoa");
            GridHelper.Col(dgv, "Room", "Vị trí");
            GridHelper.Col(dgv, "Beds", "Số giường");
            dgv.DataSource = list;
            Controls.Add(dgv);
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmHospitalMain());
        }
    }
}
