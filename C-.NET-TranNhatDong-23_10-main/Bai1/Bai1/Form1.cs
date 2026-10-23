using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Bai1
{
    public partial class Form1 : Form
    {
        // 1. Danh sách lưu trữ dữ liệu hội viên hỗ trợ tự động thông báo thay đổi
        private BindingList<MemberModel> _memberList = new BindingList<MemberModel>();

        // 2. BindingSource điều hướng dữ liệu và đồng bộ 2 chiều
        private BindingSource _bindingSource = new BindingSource();

        public Form1()
        {
            InitializeComponent();
            SetupControlsAndGrid();
            InitSampleData();
            BindControls();
            GenerateNextMemberId();
        }

        #region Yêu cầu 1: Cấu hình Grid và Khởi tạo Control bằng Code C#
        private void SetupControlsAndGrid()
        {
            this.Text = "Hệ Thống Quản Lý Dịch Vụ Phòng Gym & Hội Viên";
            this.StartPosition = FormStartPosition.CenterScreen;

            // Khởi tạo ComboBox Gói tập
            cboPackage.Items.Clear();
            cboPackage.Items.AddRange(new object[] { "Theo tháng", "6 tháng", "1 năm" });
            cboPackage.SelectedIndex = 0;

            // Cấu hình DateTimePicker
            dtpStartDate.Format = DateTimePickerFormat.Custom;
            dtpStartDate.CustomFormat = "dd/MM/yyyy";
            dtpStartDate.Value = DateTime.Today;

            // Cấu hình DataGridView bằng Code (AutoGenerateColumns = false bắt buộc bằng code)
            dgvMembers.AutoGenerateColumns = false;
            dgvMembers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMembers.MultiSelect = false;
            dgvMembers.ReadOnly = true;
            dgvMembers.AllowUserToAddRows = false;
            dgvMembers.RowHeadersVisible = false;
            dgvMembers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvMembers.Columns.Clear();
            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(MemberModel.MemberId),
                HeaderText = "Mã HV",
                FillWeight = 60
            });
            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(MemberModel.FullName),
                HeaderText = "Tên hội viên",
                FillWeight = 120
            });
            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(MemberModel.Phone),
                HeaderText = "SĐT",
                FillWeight = 85
            });
            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(MemberModel.Package),
                HeaderText = "Gói tập",
                FillWeight = 85
            });

            var colDate = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(MemberModel.StartDate),
                HeaderText = "Ngày ĐK",
                FillWeight = 85
            };
            colDate.DefaultCellStyle.Format = "dd/MM/yyyy";
            dgvMembers.Columns.Add(colDate);

            // Cột Phí dịch vụ định dạng "N0"
            var colFee = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(MemberModel.Fee),
                HeaderText = "Phí dịch vụ",
                FillWeight = 90
            };
            colFee.DefaultCellStyle.Format = "N0";
            colFee.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvMembers.Columns.Add(colFee);

            // Gán nguồn dữ liệu vào BindingSource
            _bindingSource.DataSource = _memberList;
            dgvMembers.DataSource = _bindingSource;
        }

        private void InitSampleData()
        {
            _memberList.Add(new MemberModel
            {
                MemberId = "HV001",
                FullName = "Nguyễn Văn Hải",
                Phone = "0987111222",
                Package = "Theo tháng",
                StartDate = DateTime.Today.AddDays(-15),
                Fee = 350000
            });

            _memberList.Add(new MemberModel
            {
                MemberId = "HV002",
                FullName = "Trần Thị Thu Hà",
                Phone = "0912333444",
                Package = "6 tháng",
                StartDate = DateTime.Today.AddDays(-30),
                Fee = 1800000
            });

            _memberList.Add(new MemberModel
            {
                MemberId = "HV003",
                FullName = "Lê Hoàng Long",
                Phone = "0905555666",
                Package = "1 năm",
                StartDate = DateTime.Today.AddDays(-60),
                Fee = 3200000
            });
        }
        #endregion

        #region Yêu cầu 3: Đồng bộ Data Binding 2 chiều
        private void BindControls()
        {
            // Thiết lập DataBindings đồng bộ giữa BindingSource và Control
            txtMemberId.DataBindings.Add("Text", _bindingSource, nameof(MemberModel.MemberId), true, DataSourceUpdateMode.Never);
            txtFullName.DataBindings.Add("Text", _bindingSource, nameof(MemberModel.FullName), true, DataSourceUpdateMode.Never);
            txtPhone.DataBindings.Add("Text", _bindingSource, nameof(MemberModel.Phone), true, DataSourceUpdateMode.Never);
            cboPackage.DataBindings.Add("Text", _bindingSource, nameof(MemberModel.Package), true, DataSourceUpdateMode.Never);
            dtpStartDate.DataBindings.Add("Value", _bindingSource, nameof(MemberModel.StartDate), true, DataSourceUpdateMode.Never);
            txtFee.DataBindings.Add("Text", _bindingSource, nameof(MemberModel.Fee), true, DataSourceUpdateMode.Never, "0");

            // Bắt sự kiện chuyển dòng để xóa thông báo lỗi cũ nếu có
            _bindingSource.CurrentChanged += (s, e) => errorProvider1?.Clear();
        }

        private void GenerateNextMemberId()
        {
            int maxId = 0;
            foreach (var item in _memberList)
            {
                if (item.MemberId.StartsWith("HV") && int.TryParse(item.MemberId.Substring(2), out int num))
                {
                    if (num > maxId) maxId = num;
                }
            }
            txtMemberId.Text = $"HV{(maxId + 1):D3}";
        }
        #endregion

        #region Yêu cầu 2: Kiểm tra hợp lệ dữ liệu (ErrorProvider)
        private bool ValidateData(out decimal fee)
        {
            bool isValid = true;
            fee = 0;
            errorProvider1?.Clear();

            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string feeText = txtFee.Text.Trim().Replace(",", "").Replace(".", "");

            // 1. Tên hội viên: Không để trống hoặc chứa ký tự đặc biệt
            if (string.IsNullOrEmpty(fullName))
            {
                errorProvider1?.SetError(txtFullName, "Tên hội viên không được để trống!");
                isValid = false;
            }
            else if (!Regex.IsMatch(fullName, @"^[\p{L}\s]+$"))
            {
                errorProvider1?.SetError(txtFullName, "Tên hội viên chỉ được chứa chữ cái và khoảng trắng, không được chứa ký tự đặc biệt hoặc số!");
                isValid = false;
            }

            // 2. Số điện thoại: Phải đúng 10 chữ số
            if (string.IsNullOrEmpty(phone))
            {
                errorProvider1?.SetError(txtPhone, "Số điện thoại không được để trống!");
                isValid = false;
            }
            else if (!Regex.IsMatch(phone, @"^[0-9]{10}$"))
            {
                errorProvider1?.SetError(txtPhone, "Số điện thoại phải bao gồm đúng 10 chữ số!");
                isValid = false;
            }

            // 3. Phí dịch vụ: Phải là số nguyên dương
            if (string.IsNullOrEmpty(feeText))
            {
                errorProvider1?.SetError(txtFee, "Phí dịch vụ không được để trống!");
                isValid = false;
            }
            else if (!decimal.TryParse(feeText, out fee) || fee <= 0)
            {
                errorProvider1?.SetError(txtFee, "Phí dịch vụ phải là số nguyên dương lớn hơn 0!");
                isValid = false;
            }

            return isValid;
        }
        #endregion

        #region Các nút thao tác chức năng (Thêm, Sửa, Xóa, Làm mới)
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateData(out decimal fee)) return;

            string memberId = txtMemberId.Text.Trim();

            // Kiểm tra trùng mã hội viên
            if (_memberList.Any(m => m.MemberId.Equals(memberId, StringComparison.OrdinalIgnoreCase)))
            {
                GenerateNextMemberId();
                memberId = txtMemberId.Text.Trim();
            }

            var newMember = new MemberModel
            {
                MemberId = memberId,
                FullName = txtFullName.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                Package = cboPackage.SelectedItem.ToString(),
                StartDate = dtpStartDate.Value.Date,
                Fee = fee
            };

            _memberList.Add(newMember);
            _bindingSource.Position = _bindingSource.IndexOf(newMember);

            MessageBox.Show("Thêm mới hội viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_bindingSource.Current is MemberModel current)
            {
                if (!ValidateData(out decimal fee)) return;

                // Cập nhật giá trị vào Model (kích hoạt OnPropertyChanged tự đồng bộ DataGridView)
                current.FullName = txtFullName.Text.Trim();
                current.Phone = txtPhone.Text.Trim();
                current.Package = cboPackage.SelectedItem.ToString();
                current.StartDate = dtpStartDate.Value.Date;
                current.Fee = fee;

                // Làm mới hiển thị dòng hiện tại
                _bindingSource.ResetCurrentItem();

                MessageBox.Show("Cập nhật thông tin hội viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một hội viên từ danh sách để sửa thông tin!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_bindingSource.Current is MemberModel current)
            {
                // Hộp thoại xác nhận Yes/No với biểu tượng Warning theo đúng yêu cầu đề bài
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa hội viên [{current.MemberId}] - {current.FullName}?",
                    "Cảnh báo xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    _memberList.Remove(current);
                    btnClear_Click(sender, e);
                    MessageBox.Show("Đã xóa hội viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một hội viên để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            GenerateNextMemberId();
            txtFullName.Clear();
            txtPhone.Clear();
            cboPackage.SelectedIndex = 0;
            dtpStartDate.Value = DateTime.Today;
            txtFee.Text = "350000";
            errorProvider1?.Clear();
            txtFullName.Focus();
        }
        #endregion

        #region Các sự kiện giao diện phụ
        private void cboPackage_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Tự gợi ý phí tương ứng theo từng gói tập khi thêm mới
            switch (cboPackage.SelectedItem?.ToString())
            {
                case "Theo tháng": txtFee.Text = "350000"; break;
                case "6 tháng": txtFee.Text = "1800000"; break;
                case "1 năm": txtFee.Text = "3200000"; break;
            }
        }

        private void txtMemberId_TextChanged(object sender, EventArgs e) { }
        private void txtFullName_TextChanged(object sender, EventArgs e) { }
        private void txtPhone_TextChanged(object sender, EventArgs e) { }
        private void dtpStartDate_ValueChanged(object sender, EventArgs e) { }
        private void txtFee_TextChanged(object sender, EventArgs e) { }
        private void dgvMembers_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        #endregion
    }

    #region Model Class: MemberModel
    public class MemberModel : INotifyPropertyChanged
    {
        private string _memberId;
        private string _fullName;
        private string _phone;
        private string _package;
        private DateTime _startDate;
        private decimal _fee;

        public string MemberId
        {
            get => _memberId;
            set { _memberId = value; OnPropertyChanged(); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); }
        }

        public string Package
        {
            get => _package;
            set { _package = value; OnPropertyChanged(); }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        public decimal Fee
        {
            get => _fee;
            set { _fee = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion
}