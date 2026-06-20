using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;

namespace _1121538_徐霈綺_final
{
    // ====== Data Structures ======
    public enum TaskPriority
    {
        High,
        Medium,
        Low
    }

    public enum RecurrenceType
    {
        None,
        Daily,
        Weekly,
        Monthly
    }

    public class SubTask
    {
        public bool IsCompleted { get; set; }
        public string Content { get; set; }
        public override string ToString() => Content;
    }

    public class GridRowDisplayData
    {
        public TaskItem Parent { get; set; }
        public SubTask SubTask { get; set; }
        public bool IsSubTask => SubTask != null;
    }

    public class TaskItem
    {
        public bool IsCompleted { get; set; }
        public string Content { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime? DueDate { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public Color ItemColor { get; set; } = Color.White;
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
        public DateTime CreatedDate { get; set; } = DateTime.Today;
        public DateTime? CompletedDate { get; set; }
        public List<SubTask> SubTasks { get; set; } = new List<SubTask>();
        public bool IsExpanded { get; set; } = false;

        public string ProgressText => SubTasks.Count > 0 ? $"{SubTasks.Count(s => s.IsCompleted)}/{SubTasks.Count}" : "";

        public IEnumerable<string> ToSaveLines()
        {
            string status = IsCompleted ? "[x]" : "[ ]";
            string tagsStr = Tags.Count > 0 ? string.Join(" ", Tags.Select(t => "#" + t)) : string.Empty;
            string dateStr = DueDate.HasValue ? $"@{DueDate.Value:yyyy-MM-dd}" : string.Empty;
            string priStr = $"!{Priority.ToString().ToLower()}";
            string colorStr = ItemColor.ToArgb() != Color.White.ToArgb() ? $"&{ColorTranslator.ToHtml(ItemColor)}" : string.Empty;
            string recStr = Recurrence != RecurrenceType.None ? $"~{Recurrence.ToString().ToLower()}" : string.Empty;
            string createdStr = $"+{CreatedDate:yyyy-MM-dd}";
            string completedStr = CompletedDate.HasValue ? $"*{CompletedDate.Value:yyyy-MM-dd}" : string.Empty;

            var parts = new List<string> { status, Content };
            if (!string.IsNullOrEmpty(tagsStr)) parts.Add(tagsStr);
            if (!string.IsNullOrEmpty(dateStr)) parts.Add(dateStr);
            parts.Add(priStr);
            if (!string.IsNullOrEmpty(colorStr)) parts.Add(colorStr);
            if (!string.IsNullOrEmpty(recStr)) parts.Add(recStr);
            parts.Add(createdStr);
            if (!string.IsNullOrEmpty(completedStr)) parts.Add(completedStr);

            yield return string.Join(" ", parts);

            foreach (var st in SubTasks)
            {
                yield return $"    {(st.IsCompleted ? "[x]" : "[ ]")} {st.Content}";
            }
        }

        public string ToSaveString()
        {
            return string.Join(Environment.NewLine, ToSaveLines());
        }

        public override string ToString()
        {
            return ToSaveLines().First();
        }
    }

    // ====== Task Parser ======
    public class TaskParser
    {
        public static SubTask ParseSubTask(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            line = line.TrimStart();
            var matchStatus = Regex.Match(line, @"^\[(?<status>.*?)\]");
            if (!matchStatus.Success) return null;

            string status = matchStatus.Groups["status"].Value.Trim().ToLower();
            bool isCompleted = (status == "x");
            string content = line.Substring(matchStatus.Length).Trim();
            
            if (string.IsNullOrEmpty(content)) return null;

            return new SubTask { IsCompleted = isCompleted, Content = content };
        }

        public static TaskItem Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            var matchStatus = Regex.Match(line, @"^\[(?<status>.*?)\]");
            if (!matchStatus.Success) return null;

            string status = matchStatus.Groups["status"].Value.Trim().ToLower();
            bool isCompleted = (status == "x");

            string remaining = line.Substring(matchStatus.Length);

            var matchDate = Regex.Match(remaining, @"@(\d{4}-\d{2}-\d{2})");
            DateTime? dueDate = null;
            if (matchDate.Success)
            {
                if (DateTime.TryParse(matchDate.Groups[1].Value, out DateTime d))
                    dueDate = d;
                remaining = remaining.Replace(matchDate.Value, "");
            }

            var matchPri = Regex.Match(remaining, @"!(high|medium|low)", RegexOptions.IgnoreCase);
            TaskPriority priority = TaskPriority.Medium;
            if (matchPri.Success)
            {
                string p = matchPri.Groups[1].Value.ToLower();
                if (p == "high") priority = TaskPriority.High;
                else if (p == "low") priority = TaskPriority.Low;
                remaining = remaining.Replace(matchPri.Value, "");
            }

            var matchColor = Regex.Match(remaining, @"&([a-zA-Z]+|#[0-9a-fA-F]{3,8})");
            Color itemColor = Color.White;
            if (matchColor.Success)
            {
                try { itemColor = ColorTranslator.FromHtml(matchColor.Groups[1].Value); } catch { }
                remaining = remaining.Replace(matchColor.Value, "");
            }

            var matchRec = Regex.Match(remaining, @"~(daily|weekly|monthly)", RegexOptions.IgnoreCase);
            RecurrenceType recurrence = RecurrenceType.None;
            if (matchRec.Success)
            {
                string r = matchRec.Groups[1].Value.ToLower();
                if (r == "daily") recurrence = RecurrenceType.Daily;
                else if (r == "weekly") recurrence = RecurrenceType.Weekly;
                else if (r == "monthly") recurrence = RecurrenceType.Monthly;
                remaining = remaining.Replace(matchRec.Value, "");
            }

            var matchCreated = Regex.Match(remaining, @"\+(\d{4}-\d{2}-\d{2})");
            DateTime createdDate = DateTime.Today;
            if (matchCreated.Success)
            {
                if (DateTime.TryParse(matchCreated.Groups[1].Value, out DateTime d))
                    createdDate = d;
                remaining = remaining.Replace(matchCreated.Value, "");
            }

            var matchCompleted = Regex.Match(remaining, @"\*(\d{4}-\d{2}-\d{2})");
            DateTime? completedDate = null;
            if (matchCompleted.Success)
            {
                if (DateTime.TryParse(matchCompleted.Groups[1].Value, out DateTime d))
                    completedDate = d;
                remaining = remaining.Replace(matchCompleted.Value, "");
            }

            var tags = new List<string>();
            var matchTags = Regex.Matches(remaining, @"#(\S+)");
            foreach (Match m in matchTags)
            {
                tags.Add(m.Groups[1].Value);
                remaining = remaining.Replace(m.Value, "");
            }

            string content = remaining.Trim();
            if (string.IsNullOrEmpty(content)) return null; 

            return new TaskItem
            {
                IsCompleted = isCompleted,
                Content = content,
                DueDate = dueDate,
                Priority = priority,
                Tags = tags,
                ItemColor = itemColor,
                Recurrence = recurrence,
                CreatedDate = createdDate,
                CompletedDate = completedDate
            };
        }
    }

    // ====== File Service ======
    public static class FileService
    {
        public static List<TaskItem> LoadTasks(string filePath)
        {
            var tasks = new List<TaskItem>();
            try
            {
                var lines = File.ReadAllLines(filePath);
                TaskItem currentTask = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("\t") || line.StartsWith("    "))
                    {
                        var subTask = TaskParser.ParseSubTask(line);
                        if (subTask != null && currentTask != null)
                        {
                            currentTask.SubTasks.Add(subTask);
                        }
                    }
                    else
                    {
                        currentTask = TaskParser.Parse(line);
                        if (currentTask != null) tasks.Add(currentTask);
                    }
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"讀取檔案時發生錯誤：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"權限不足：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return tasks;
        }

        public static void SaveTasks(string filePath, List<TaskItem> tasks)
        {
            try
            {
                var lines = tasks.SelectMany(t => t.ToSaveLines());
                using (var writer = new StreamWriter(filePath, false))
                {
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"儲存檔案時發生錯誤：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"權限不足：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ====== Task Dialog Form ======
    public class TaskDialogForm : Form
    {
        public TaskItem CurrentTask { get; private set; }
        
        private TextBox txtTaskName, txtTags;
        private DateTimePicker dtpDueDate;
        private CheckBox chkHasDueDate, chkIsCompleted;
        private ComboBox cmbPriority;
        private Button btnOk, btnCancel, btnPickColor;
        private Panel pnlColorPreview;
        private ComboBox cmbRecurrence;
        private TextBox txtSubTask;
        private CheckedListBox clbSubTasks;

        public TaskDialogForm(TaskItem task = null)
        {
            CurrentTask = task ?? new TaskItem();
            SetupUI();
            PopulateData();
        }

        private void SetupUI()
        {
            this.Text = string.IsNullOrEmpty(CurrentTask.Content) ? "新增任務" : "編輯任務";
            this.Size = new Size(350, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblTaskName = new Label { Text = "任務名稱:", Location = new Point(20, 20), AutoSize = true };
            txtTaskName = new TextBox { Location = new Point(100, 18), Width = 200 };

            var lblTags = new Label { Text = "標籤(以空隔或逗號隔開):", Location = new Point(20, 60), AutoSize = true };
            txtTags = new TextBox { Location = new Point(160, 58), Width = 140 };

            chkHasDueDate = new CheckBox { Text = "設定截止日期", Location = new Point(20, 100), Width = 100 };
            dtpDueDate = new DateTimePicker { Location = new Point(130, 98), Width = 170, Format = DateTimePickerFormat.Short, Enabled = false };
            chkHasDueDate.CheckedChanged += (s, e) => { dtpDueDate.Enabled = chkHasDueDate.Checked; };

            var lblPriority = new Label { Text = "優先級:", Location = new Point(20, 140), AutoSize = true };
            cmbPriority = new ComboBox { Location = new Point(100, 138), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPriority.Items.AddRange(new object[] { "High", "Medium", "Low" });

            chkIsCompleted = new CheckBox { Text = "已完成", Location = new Point(20, 180), Width = 100 };

            var lblColor = new Label { Text = "顏色標示:", Location = new Point(140, 180), AutoSize = true };
            pnlColorPreview = new Panel { Location = new Point(210, 178), Size = new Size(20, 20), BorderStyle = BorderStyle.FixedSingle };
            btnPickColor = new Button { Text = "選色", Location = new Point(240, 175), Width = 60 };
            btnPickColor.Click += BtnPickColor_Click;

            var lblRecurrence = new Label { Text = "重複頻率:", Location = new Point(20, 220), AutoSize = true };
            cmbRecurrence = new ComboBox { Location = new Point(100, 218), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRecurrence.Items.AddRange(new object[] { "無", "每天 (Daily)", "每週 (Weekly)", "每月 (Monthly)" });

            var lblSubTasks = new Label { Text = "子任務:", Location = new Point(20, 260), AutoSize = true };
            txtSubTask = new TextBox { Location = new Point(100, 258), Width = 110 };
            var btnAddSubTask = new Button { Text = "新增", Location = new Point(215, 256), Width = 45 };
            var btnDeleteSubTask = new Button { Text = "刪除", Location = new Point(265, 256), Width = 45 };
            clbSubTasks = new CheckedListBox { Location = new Point(20, 285), Size = new Size(295, 80) };
            
            btnAddSubTask.Click += (s, e) => {
                if(!string.IsNullOrWhiteSpace(txtSubTask.Text)) {
                    var st = new SubTask { Content = txtSubTask.Text.Trim() };
                    clbSubTasks.Items.Add(st, false);
                    txtSubTask.Clear();
                }
            };
            btnDeleteSubTask.Click += (s, e) => {
                if(clbSubTasks.SelectedIndex >= 0) {
                    clbSubTasks.Items.RemoveAt(clbSubTasks.SelectedIndex);
                }
            };

            btnOk = new Button { Text = "確認 (OK)", Location = new Point(60, 390), Width = 90 };
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button { Text = "取消 (Cancel)", Location = new Point(170, 390), Width = 90 };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblTaskName);
            this.Controls.Add(txtTaskName);
            this.Controls.Add(lblTags);
            this.Controls.Add(txtTags);
            this.Controls.Add(chkHasDueDate);
            this.Controls.Add(dtpDueDate);
            this.Controls.Add(lblPriority);
            this.Controls.Add(cmbPriority);
            this.Controls.Add(chkIsCompleted);
            this.Controls.Add(lblColor);
            this.Controls.Add(pnlColorPreview);
            this.Controls.Add(btnPickColor);
            this.Controls.Add(lblRecurrence);
            this.Controls.Add(cmbRecurrence);
            this.Controls.Add(lblSubTasks);
            this.Controls.Add(txtSubTask);
            this.Controls.Add(btnAddSubTask);
            this.Controls.Add(btnDeleteSubTask);
            this.Controls.Add(clbSubTasks);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
        }

        private void BtnPickColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = pnlColorPreview.BackColor;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    pnlColorPreview.BackColor = cd.Color;
                }
            }
        }

        private void PopulateData()
        {
            txtTaskName.Text = CurrentTask.Content ?? "";
            txtTags.Text = string.Join(" ", CurrentTask.Tags);
            
            if (CurrentTask.DueDate.HasValue)
            {
                chkHasDueDate.Checked = true;
                dtpDueDate.Value = CurrentTask.DueDate.Value;
            }
            else
            {
                chkHasDueDate.Checked = false;
                dtpDueDate.Value = DateTime.Now;
            }

            if (CurrentTask.Priority == TaskPriority.High) cmbPriority.SelectedIndex = 0;
            else if (CurrentTask.Priority == TaskPriority.Medium) cmbPriority.SelectedIndex = 1;
            else cmbPriority.SelectedIndex = 2;

            if (CurrentTask.Recurrence == RecurrenceType.None) cmbRecurrence.SelectedIndex = 0;
            else if (CurrentTask.Recurrence == RecurrenceType.Daily) cmbRecurrence.SelectedIndex = 1;
            else if (CurrentTask.Recurrence == RecurrenceType.Weekly) cmbRecurrence.SelectedIndex = 2;
            else cmbRecurrence.SelectedIndex = 3;

            chkIsCompleted.Checked = CurrentTask.IsCompleted;
            pnlColorPreview.BackColor = CurrentTask.ItemColor;

            clbSubTasks.Items.Clear();
            foreach (var st in CurrentTask.SubTasks)
            {
                clbSubTasks.Items.Add(new SubTask { Content = st.Content, IsCompleted = st.IsCompleted }, st.IsCompleted);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string name = txtTaskName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("請輸入任務名稱", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CurrentTask.Content = name;

            var tagsRaw = txtTags.Text.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            CurrentTask.Tags = new List<string>(tagsRaw);

            CurrentTask.DueDate = chkHasDueDate.Checked ? (DateTime?)dtpDueDate.Value.Date : null;

            if (cmbPriority.SelectedIndex == 0) CurrentTask.Priority = TaskPriority.High;
            else if (cmbPriority.SelectedIndex == 1) CurrentTask.Priority = TaskPriority.Medium;
            else CurrentTask.Priority = TaskPriority.Low;

            if (cmbRecurrence.SelectedIndex == 0) CurrentTask.Recurrence = RecurrenceType.None;
            else if (cmbRecurrence.SelectedIndex == 1) CurrentTask.Recurrence = RecurrenceType.Daily;
            else if (cmbRecurrence.SelectedIndex == 2) CurrentTask.Recurrence = RecurrenceType.Weekly;
            else CurrentTask.Recurrence = RecurrenceType.Monthly;

            CurrentTask.IsCompleted = chkIsCompleted.Checked;
            CurrentTask.ItemColor = pnlColorPreview.BackColor;

            CurrentTask.SubTasks.Clear();
            for (int i = 0; i < clbSubTasks.Items.Count; i++)
            {
                if (clbSubTasks.Items[i] is SubTask st)
                {
                    st.IsCompleted = clbSubTasks.GetItemChecked(i);
                    CurrentTask.SubTasks.Add(st);
                }
            }

            if (CurrentTask.SubTasks.Count > 0 && CurrentTask.SubTasks.All(s => s.IsCompleted) && !CurrentTask.IsCompleted)
            {
                if (MessageBox.Show("所有子任務都已完成，是否將父任務也設為完成？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    CurrentTask.IsCompleted = true;
                }
            }

            this.DialogResult = DialogResult.OK;
        }
    }

    // ====== Form1 UI Logic ======
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, string lParam);

        private DataGridView dgvTasks;
        private Label lblPath, lblSort, lblFilter, lblFilterColor, lblFilterTime;
        private ComboBox cmbSortBy, cmbFilterTag, cmbFilterColor, cmbFilterTime;
        private Button btnOpen, btnDelete, btnSave, btnAdd, btnDashboard, btnFocus;
        private TextBox txtSearch;

        private string currentFilePath = string.Empty;
        private List<TaskItem> _allTasks = new List<TaskItem>();
        private bool _isUpdatingUI = false;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string lastPath = Properties.Settings.Default.LastFilePath;
                if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
                {
                    currentFilePath = lastPath;
                    lblPath.Text = $"目前檔案: {currentFilePath}";
                    _allTasks = FileService.LoadTasks(currentFilePath);
                    UpdateFilterTagsComboBox();
                    ApplyFilters();
                }
            }
            catch (Exception)
            {
                try
                {
                    Properties.Settings.Default.LastFilePath = "";
                    Properties.Settings.Default.Save();
                }
                catch { } // Ignore saving errors cleanly in case of corrupted configurations
            }
        }

        private void SetupUI()
        {
            this.Text = "代辦事項管理視窗程式";
            this.Size = new Size(860, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            lblPath = new Label { Text = "目前檔案: 未儲存 (請開啟或儲存檔案)", Location = new Point(20, 10), AutoSize = true, ForeColor = Color.Blue };

            lblSort = new Label { Text = "排序方式:", Location = new Point(20, 45), AutoSize = true };
            cmbSortBy = new ComboBox { Location = new Point(80, 42), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSortBy.Items.AddRange(new object[] { "預設排序 (Default)", "截止日期：由近到遠 (Asc)", "截止日期：由遠到近 (Desc)", "依優先級 (By Priority)" });
            cmbSortBy.SelectedIndex = 0;
            cmbSortBy.SelectedIndexChanged += (s, e) => ApplyFilters();

            lblFilter = new Label { Text = "標籤篩選:", Location = new Point(270, 45), AutoSize = true };
            cmbFilterTag = new ComboBox { Location = new Point(330, 42), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterTag.Items.Add("[所有標籤]");
            cmbFilterTag.SelectedIndex = 0;
            cmbFilterTag.SelectedIndexChanged += (s, e) => ApplyFilters();

            lblFilterColor = new Label { Text = "顏色篩選:", Location = new Point(440, 45), AutoSize = true };
            cmbFilterColor = new ComboBox { Location = new Point(500, 42), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterColor.Items.Add("[所有顏色]");
            cmbFilterColor.SelectedIndex = 0;
            cmbFilterColor.SelectedIndexChanged += (s, e) => ApplyFilters();

            lblFilterTime = new Label { Text = "時間篩選:", Location = new Point(610, 45), AutoSize = true };
            cmbFilterTime = new ComboBox { Location = new Point(670, 42), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterTime.Items.AddRange(new object[] { "[所有時間]", "已逾期 (Overdue)", "今天到期 (Due Today)", "七天內到期 (Next 7 Days)", "無期限 (No Due Date)" });
            cmbFilterTime.SelectedIndex = 0;
            cmbFilterTime.SelectedIndexChanged += (s, e) => ApplyFilters();

            btnAdd = new Button { Text = "新增任務", Location = new Point(20, 80), Width = 100 };
            btnAdd.Click += BtnAdd_Click;

            btnDashboard = new Button { Text = "查看儀表板", Location = new Point(130, 80), Width = 100 };
            btnDashboard.Click += (s, e) => { new DashboardForm(_allTasks).Show(); };

            btnFocus = new Button { Text = "進入專注模式", Location = new Point(240, 80), Width = 120 };
            btnFocus.Click += BtnFocus_Click;

            var btnHelp = new Button { Text = "❓ 教學指南", Location = new Point(370, 80), Width = 100 };
            btnHelp.Click += (s, e) => { new HelpForm().ShowDialog(); };

            var lblSearch = new Label { Text = "搜尋:", Location = new Point(480, 85), AutoSize = true };
            txtSearch = new TextBox { Location = new Point(530, 82), Width = 150 };
            SendMessage(txtSearch.Handle, 0x1501, 1, "搜尋任務...");
            txtSearch.TextChanged += (s, e) => ApplyFilters();

            // DataGridView
            dgvTasks = new DataGridView 
            { 
                Location = new Point(20, 120), 
                Size = new Size(800, 430), 
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowDrop = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                MultiSelect = false,
                ReadOnly = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(224, 224, 224),
                EnableHeadersVisualStyles = false
            };
            dgvTasks.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvTasks.ColumnHeadersDefaultCellStyle.Font = new Font(this.Font, FontStyle.Bold);
            dgvTasks.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgvTasks.ColumnHeadersHeight = 35;
            dgvTasks.RowTemplate.Height = 30;
            dgvTasks.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);

            var colExpand = new DataGridViewTextBoxColumn { Name = "Expand", HeaderText = "", Width = 30, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } };
            var colStatus = new DataGridViewCheckBoxColumn { Name = "Status", HeaderText = "完成", Width = 50, ReadOnly = false };
            var colContent = new DataGridViewTextBoxColumn { Name = "Content", HeaderText = "任務內容", Width = 350, ReadOnly = true };
            var colTags = new DataGridViewTextBoxColumn { Name = "Tags", HeaderText = "標籤", Width = 120, ReadOnly = true };
            var colDueDate = new DataGridViewTextBoxColumn { Name = "DueDate", HeaderText = "截止日期", Width = 100, ReadOnly = true };
            var colPriority = new DataGridViewTextBoxColumn { Name = "Priority", HeaderText = "優先級", Width = 80, ReadOnly = true };
            var colProgress = new DataGridViewTextBoxColumn { Name = "Progress", HeaderText = "進度", Width = 60, ReadOnly = true };
            
            dgvTasks.Columns.AddRange(new DataGridViewColumn[] { colExpand, colStatus, colContent, colTags, colDueDate, colPriority, colProgress });
            
            dgvTasks.CellClick += DgvTasks_CellClick;
            dgvTasks.CellContentClick += DgvTasks_CellContentClick;
            dgvTasks.CellDoubleClick += DgvTasks_CellDoubleClick;

            dgvTasks.MouseDown += DgvTasks_MouseDown;
            dgvTasks.MouseMove += DgvTasks_MouseMove;
            dgvTasks.DragEnter += DgvTasks_DragEnter;
            dgvTasks.DragOver += DgvTasks_DragOver;
            dgvTasks.DragDrop += DgvTasks_DragDrop;

            var btnNew = new Button { Text = "New File (建立新檔)", Location = new Point(20, 570), Width = 140, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnNew.Click += BtnNew_Click;

            btnOpen = new Button { Text = "Open File", Location = new Point(170, 570), Width = 100, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnOpen.Click += BtnOpen_Click;

            btnSave = new Button { Text = "Save", Location = new Point(280, 570), Width = 100, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnSave.Click += BtnSave_Click;

            btnDelete = new Button { Text = "Delete", Location = new Point(390, 570), Width = 100, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnDelete.Click += BtnDelete_Click;

            this.Controls.Add(lblPath);
            this.Controls.Add(lblSort);
            this.Controls.Add(cmbSortBy);
            this.Controls.Add(lblFilter);
            this.Controls.Add(cmbFilterTag);
            this.Controls.Add(lblFilterColor);
            this.Controls.Add(cmbFilterColor);
            this.Controls.Add(lblFilterTime);
            this.Controls.Add(cmbFilterTime);
            this.Controls.Add(btnAdd);
            this.Controls.Add(btnDashboard);
            this.Controls.Add(btnFocus);
            this.Controls.Add(btnHelp);
            this.Controls.Add(lblSearch);
            this.Controls.Add(txtSearch);

            this.Controls.Add(dgvTasks);
            this.Controls.Add(btnNew);
            this.Controls.Add(btnOpen);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnDelete);
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (_allTasks.Count > 0)
            {
                var result = MessageBox.Show("建立新檔將會清空目前的畫面，確定要繼續嗎？（若尚未儲存，請先取消並點擊 Save）", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                sfd.DefaultExt = "txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    _allTasks.Clear();
                    string newFilePath = sfd.FileName;
                    File.WriteAllText(newFilePath, "");
                    currentFilePath = newFilePath;
                    lblPath.Text = $"目前檔案: {currentFilePath}";

                    Properties.Settings.Default.LastFilePath = currentFilePath;
                    Properties.Settings.Default.Save();

                    UpdateFilterTagsComboBox();
                    ApplyFilters();
                }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new TaskDialogForm())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _allTasks.Add(dialog.CurrentTask);
                    UpdateFilterTagsComboBox();
                    ApplyFilters();
                }
            }
        }

        private void BtnFocus_Click(object sender, EventArgs e)
        {
            if (dgvTasks.SelectedRows.Count == 1)
            {
                var data = dgvTasks.SelectedRows[0].Tag as GridRowDisplayData;
                if (data != null && !data.IsSubTask)
                {
                    var task = data.Parent;
                    if (task.IsCompleted)
                    {
                        MessageBox.Show("該任務已完成，請選擇未完成的任務進入專注模式。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    using (var focusForm = new FocusForm(task))
                    {
                        if (focusForm.ShowDialog() == DialogResult.OK)
                        {
                            task.IsCompleted = true;
                            task.CompletedDate = DateTime.Today;

                            if (task.Recurrence != RecurrenceType.None)
                            {
                                GenerateNextRecurringTask(task);
                            }
                            else
                            {
                                bool wasUpdating = _isUpdatingUI;
                                _isUpdatingUI = true;
                                try
                                {
                                    if (!string.IsNullOrEmpty(currentFilePath))
                                    {
                                        FileService.SaveTasks(currentFilePath, _allTasks);
                                    }
                                    UpdateFilterTagsComboBox();
                                    ApplyFilters();
                                }
                                finally
                                {
                                    _isUpdatingUI = wasUpdating;
                                }
                            }
                        }
                    }
                }
                else if (data != null && data.IsSubTask)
                {
                    MessageBox.Show("專注模式目前只能針對父任務進行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("請選擇一個未完成的任務進入專注模式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DgvTasks_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dgvTasks.Columns["Expand"].Index)
            {
                var data = dgvTasks.Rows[e.RowIndex].Tag as GridRowDisplayData;
                if (data != null && !data.IsSubTask && data.Parent.SubTasks != null && data.Parent.SubTasks.Count > 0)
                {
                    data.Parent.IsExpanded = !data.Parent.IsExpanded;
                    ApplyFilters();
                }
            }
        }

        private void DgvTasks_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingUI || e.RowIndex < 0) return;

            if (e.ColumnIndex == dgvTasks.Columns["Status"].Index)
            {
                dgvTasks.CommitEdit(DataGridViewDataErrorContexts.Commit);
                var row = dgvTasks.Rows[e.RowIndex];
                var data = row.Tag as GridRowDisplayData;

                bool wasUpdating = _isUpdatingUI;
                _isUpdatingUI = true;
                bool shouldGenerateRecurrence = false;

                try
                {
                    if (row.Cells["Status"].Value is bool isChecked)
                    {
                        if (data.IsSubTask)
                        {
                            data.SubTask.IsCompleted = isChecked;

                            if (data.Parent.SubTasks.Count > 0)
                            {
                                bool allCompleted = data.Parent.SubTasks.All(s => s.IsCompleted);
                                if (allCompleted && !data.Parent.IsCompleted)
                                {
                                    data.Parent.IsCompleted = true;
                                    data.Parent.CompletedDate = DateTime.Today;
                                    if (data.Parent.Recurrence != RecurrenceType.None)
                                    {
                                        shouldGenerateRecurrence = true;
                                    }
                                }
                                else if (!allCompleted && data.Parent.IsCompleted)
                                {
                                    data.Parent.IsCompleted = false;
                                    data.Parent.CompletedDate = null;
                                }
                            }
                        }
                        else
                        {
                            bool newlyCompleted = isChecked && !data.Parent.IsCompleted;
                            data.Parent.IsCompleted = isChecked;

                            if (isChecked) data.Parent.CompletedDate = DateTime.Today;
                            else data.Parent.CompletedDate = null;

                            if (data.Parent.SubTasks != null)
                            {
                                foreach (var st in data.Parent.SubTasks)
                                {
                                    st.IsCompleted = isChecked;
                                }
                            }

                            if (newlyCompleted && data.Parent.Recurrence != RecurrenceType.None)
                            {
                                shouldGenerateRecurrence = true;
                            }
                        }

                        if (!string.IsNullOrEmpty(currentFilePath))
                        {
                            FileService.SaveTasks(currentFilePath, _allTasks);
                        }

                        UpdateFilterTagsComboBox();
                        ApplyFilters();
                    }
                }
                finally
                {
                    _isUpdatingUI = wasUpdating;
                }

                if (shouldGenerateRecurrence)
                {
                    GenerateNextRecurringTask(data.Parent);
                }
            }
        }

        private void DgvTasks_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex != dgvTasks.Columns["Status"].Index && e.ColumnIndex != dgvTasks.Columns["Expand"].Index)
            {
                var data = dgvTasks.Rows[e.RowIndex].Tag as GridRowDisplayData;
                if (data != null && !data.IsSubTask)
                {
                    using (var dialog = new TaskDialogForm(data.Parent))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            UpdateFilterTagsComboBox();
                            ApplyFilters();
                        }
                    }
                }
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "Text Files|*.txt" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = ofd.FileName;
                    lblPath.Text = $"目前檔案: {currentFilePath}";
                    _allTasks = FileService.LoadTasks(currentFilePath);
                    UpdateFilterTagsComboBox();
                    ApplyFilters();

                    Properties.Settings.Default.LastFilePath = currentFilePath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_allTasks.Count == 0 && string.IsNullOrEmpty(currentFilePath)) return;

            if (string.IsNullOrEmpty(currentFilePath))
            {
                using (var sfd = new SaveFileDialog { Filter = "Text Files|*.txt" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        currentFilePath = sfd.FileName;
                        lblPath.Text = $"現在檔案: {currentFilePath}";
                    }
                    else return;
                }
            }
            FileService.SaveTasks(currentFilePath, _allTasks);

            Properties.Settings.Default.LastFilePath = currentFilePath;
            Properties.Settings.Default.Save();

            MessageBox.Show("儲存成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvTasks.SelectedRows.Count > 0)
            {
                var data = dgvTasks.SelectedRows[0].Tag as GridRowDisplayData;
                if (data != null && !data.IsSubTask)
                {
                    _allTasks.Remove(data.Parent);
                    UpdateFilterTagsComboBox();
                    ApplyFilters();
                }
                else if (data != null && data.IsSubTask)
                {
                    MessageBox.Show("請透過雙擊父任務，在編輯視窗中刪除子任務。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("請先選擇要刪除的任務", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void GenerateNextRecurringTask(TaskItem completedTask)
        {
            if (completedTask == null || completedTask.Recurrence == RecurrenceType.None) return;

            var newTask = new TaskItem
            {
                Content = completedTask.Content,
                Tags = new List<string>(completedTask.Tags),
                Priority = completedTask.Priority,
                ItemColor = completedTask.ItemColor,
                Recurrence = completedTask.Recurrence,
                IsCompleted = false,
                CreatedDate = DateTime.Today,
                CompletedDate = null
            };

            foreach(var st in completedTask.SubTasks)
            {
                newTask.SubTasks.Add(new SubTask { Content = st.Content, IsCompleted = false });
            }

            DateTime baseDate = completedTask.DueDate.HasValue ? completedTask.DueDate.Value : DateTime.Today;

            if (completedTask.Recurrence == RecurrenceType.Daily) newTask.DueDate = baseDate.AddDays(1);
            else if (completedTask.Recurrence == RecurrenceType.Weekly) newTask.DueDate = baseDate.AddDays(7);
            else if (completedTask.Recurrence == RecurrenceType.Monthly) newTask.DueDate = baseDate.AddMonths(1);

            bool wasUpdating = _isUpdatingUI;
            _isUpdatingUI = true;
            try
            {
                _allTasks.Add(newTask);

                if (!string.IsNullOrEmpty(currentFilePath))
                {
                     FileService.SaveTasks(currentFilePath, _allTasks);
                }

                UpdateFilterTagsComboBox();
                ApplyFilters();
            }
            finally
            {
                _isUpdatingUI = wasUpdating;
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && Path.GetExtension(files[0]).ToLower() == ".txt")
            {
                currentFilePath = files[0];
                lblPath.Text = $"目前檔案: {currentFilePath}";
                _allTasks = FileService.LoadTasks(currentFilePath);
                UpdateFilterTagsComboBox();
                ApplyFilters();

                Properties.Settings.Default.LastFilePath = currentFilePath;
                Properties.Settings.Default.Save();
            }
        }

        private int _dragSourceIndex = -1;

        private void DgvTasks_MouseDown(object sender, MouseEventArgs e)
        {
            if (cmbSortBy.SelectedIndex == 0) // Default sort only
            {
                var hitTest = dgvTasks.HitTest(e.X, e.Y);
                if (hitTest.RowIndex != -1 && hitTest.ColumnIndex != dgvTasks.Columns["Status"].Index && hitTest.ColumnIndex != dgvTasks.Columns["Expand"].Index)
                {
                    _dragSourceIndex = hitTest.RowIndex;
                }
                else
                {
                    _dragSourceIndex = -1;
                }
            }
        }

        private void DgvTasks_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left && _dragSourceIndex != -1)
            {
                var row = dgvTasks.Rows[_dragSourceIndex];
                dgvTasks.DoDragDrop(row, DragDropEffects.Move);
            }
        }

        private void DgvTasks_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(DataGridViewRow))) e.Effect = DragDropEffects.Move;
        }

        private void DgvTasks_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(DataGridViewRow))) e.Effect = DragDropEffects.Move;
        }

        private void DgvTasks_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(DataGridViewRow))) return;

            Point clientPoint = dgvTasks.PointToClient(new Point(e.X, e.Y));
            var hitTest = dgvTasks.HitTest(clientPoint.X, clientPoint.Y);
            int dropIndex = hitTest.RowIndex;

            if (dropIndex >= 0 && _dragSourceIndex >= 0 && dropIndex != _dragSourceIndex)
            {
                var sourceData = dgvTasks.Rows[_dragSourceIndex].Tag as GridRowDisplayData;
                var targetData = dgvTasks.Rows[dropIndex].Tag as GridRowDisplayData;

                if (sourceData != null && targetData != null && !sourceData.IsSubTask && !targetData.IsSubTask)
                {
                    var sourceTask = sourceData.Parent;
                    var targetTask = targetData.Parent;

                    int sourceIdx = _allTasks.IndexOf(sourceTask);
                    int targetIdx = _allTasks.IndexOf(targetTask);

                    if (sourceIdx >= 0 && targetIdx >= 0)
                    {
                        _allTasks.RemoveAt(sourceIdx);
                        _allTasks.Insert(targetIdx, sourceTask);
                        
                        if (!string.IsNullOrEmpty(currentFilePath))
                        {
                            FileService.SaveTasks(currentFilePath, _allTasks);
                        }
                        ApplyFilters();
                    }
                }
            }
            _dragSourceIndex = -1;
        }

        private void UpdateFilterTagsComboBox()
        {
            _isUpdatingUI = true;
            try
            {
                string currentTagSelection = cmbFilterTag.SelectedItem?.ToString();

                cmbFilterTag.Items.Clear();
                cmbFilterTag.Items.Add("[所有標籤]");

                var uniqueTags = _allTasks.SelectMany(t => t.Tags).Distinct().OrderBy(t => t).ToList();
                foreach (var tag in uniqueTags)
                {
                    cmbFilterTag.Items.Add(tag); // Not adding '#' in the UI
                }

                if (currentTagSelection != null && cmbFilterTag.Items.Contains(currentTagSelection))
                    cmbFilterTag.SelectedItem = currentTagSelection;
                else
                    cmbFilterTag.SelectedIndex = 0;

                string currentColorSelection = cmbFilterColor.SelectedItem?.ToString();

                cmbFilterColor.Items.Clear();
                cmbFilterColor.Items.Add("[所有顏色]");

                var uniqueColors = _allTasks.Select(t => ColorTranslator.ToHtml(t.ItemColor)).Distinct().ToList();
                foreach (var col in uniqueColors)
                {
                    if (col != ColorTranslator.ToHtml(Color.White))
                    {
                        cmbFilterColor.Items.Add(col);
                    }
                }
                if (currentColorSelection != null && cmbFilterColor.Items.Contains(currentColorSelection))
                    cmbFilterColor.SelectedItem = currentColorSelection;
                else
                    cmbFilterColor.SelectedIndex = 0;
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void ApplyFilters()
        {
            if (cmbFilterTag.SelectedIndex == -1 || cmbSortBy.SelectedIndex == -1 || cmbFilterColor.SelectedIndex == -1 || cmbFilterTime.SelectedIndex == -1) return;

            bool wasUpdating = _isUpdatingUI;
            _isUpdatingUI = true;
            try
            {
                dgvTasks.Rows.Clear();

                var filtered = _allTasks.AsEnumerable();

                // 0. Search Text
                string searchText = txtSearch.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(searchText))
                {
                    filtered = filtered.Where(t => t.Content.ToLower().Contains(searchText) || t.Tags.Any(tag => tag.ToLower().Contains(searchText)));
                }

                // 1. Tag Filtering
                string filterTag = cmbFilterTag.SelectedItem.ToString();
                if (filterTag != "[所有標籤]")
                {
                    filtered = filtered.Where(t => t.Tags.Contains(filterTag));
                }

                // 2. Color Filtering
                string filterColor = cmbFilterColor.SelectedItem.ToString();
                if (filterColor != "[所有顏色]")
                {
                    filtered = filtered.Where(t => ColorTranslator.ToHtml(t.ItemColor) == filterColor);
                }

                // 3. Time Filtering
                string filterTime = cmbFilterTime.SelectedItem.ToString();
                if (filterTime == "已逾期 (Overdue)")
                {
                    filtered = filtered.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.Today && !t.IsCompleted);
                }
                else if (filterTime == "今天到期 (Due Today)")
                {
                    filtered = filtered.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today);
                }
                else if (filterTime == "七天內到期 (Next 7 Days)")
                {
                    filtered = filtered.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= DateTime.Today && t.DueDate.Value.Date <= DateTime.Today.AddDays(7));
                }
                else if (filterTime == "無期限 (No Due Date)")
                {
                    filtered = filtered.Where(t => !t.DueDate.HasValue);
                }

                // 4. Sorting
                int sortMode = cmbSortBy.SelectedIndex;
                if (sortMode == 1) // Due Date Ascending
                {
                    filtered = filtered
                        .OrderBy(t => t.DueDate.HasValue ? 0 : 1) // Tasks with dates come first
                        .ThenBy(t => t.DueDate);
                }
                else if (sortMode == 2) // Due Date Descending
                {
                    filtered = filtered
                        .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
                        .ThenByDescending(t => t.DueDate);
                }
                else if (sortMode == 3) // Priority
                {
                    filtered = filtered.OrderBy(t => (int)t.Priority); // High=0, Medium=1, Low=2
                }

                // Render Items
                var displayList = new List<GridRowDisplayData>();
                foreach (var t in filtered)
                {
                    displayList.Add(new GridRowDisplayData { Parent = t });
                    if (t.IsExpanded && t.SubTasks != null && t.SubTasks.Count > 0)
                    {
                        foreach (var st in t.SubTasks)
                        {
                            displayList.Add(new GridRowDisplayData { Parent = t, SubTask = st });
                        }
                    }
                }

                foreach (var data in displayList)
                {
                    int rowIndex = dgvTasks.Rows.Add();
                    var row = dgvTasks.Rows[rowIndex];
                    row.Tag = data; // store data

                    if (!data.IsSubTask)
                    {
                        var t = data.Parent;
                        string expandIcon = "";
                        if (t.SubTasks != null && t.SubTasks.Count > 0)
                        {
                            expandIcon = t.IsExpanded ? "▼" : "▶";
                        }
                        row.Cells["Expand"].Value = expandIcon;
                        row.Cells["Status"].Value = t.IsCompleted;
                        row.Cells["Content"].Value = t.Content;
                        row.Cells["Tags"].Value = string.Join(", ", t.Tags);
                        row.Cells["DueDate"].Value = t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "";
                        row.Cells["Priority"].Value = t.Priority.ToString();
                        row.Cells["Progress"].Value = t.ProgressText;

                        if (!t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.Today)
                        {
                            row.DefaultCellStyle.ForeColor = Color.Red;
                            row.DefaultCellStyle.Font = new Font(dgvTasks.Font, FontStyle.Bold);
                        }

                        if (t.ItemColor.ToArgb() != Color.White.ToArgb() && t.ItemColor.ToArgb() != Color.Transparent.ToArgb())
                        {
                            row.DefaultCellStyle.BackColor = t.ItemColor;

                            // Contrast logic
                            if (t.ItemColor.R * 0.299 + t.ItemColor.G * 0.587 + t.ItemColor.B * 0.114 < 128)
                                row.DefaultCellStyle.ForeColor = Color.White; // Only override text if background is too dark
                        }

                        if (t.IsCompleted)
                        {
                            row.DefaultCellStyle.Font = new Font(dgvTasks.Font, FontStyle.Strikeout);
                        }
                    }
                    else
                    {
                        var st = data.SubTask;
                        row.Cells["Expand"].Value = "";
                        row.Cells["Status"].Value = st.IsCompleted;
                        row.Cells["Content"].Value = "    " + st.Content;
                        row.Cells["Tags"].Value = "";
                        row.Cells["DueDate"].Value = "";
                        row.Cells["Priority"].Value = "";
                        row.Cells["Progress"].Value = "";

                        row.DefaultCellStyle.BackColor = Color.WhiteSmoke;
                        row.DefaultCellStyle.ForeColor = Color.Black;

                        if (st.IsCompleted)
                        {
                            row.DefaultCellStyle.Font = new Font(dgvTasks.Font, FontStyle.Strikeout);
                        }
                    }
                }
            }
            finally
            {
                _isUpdatingUI = wasUpdating;
            }
        }
    }

    // ====== Dashboard Form ======
    public class DashboardForm : Form
    {
        private List<TaskItem> _tasks;
        private PictureBox pbHeatmap;
        private Chart chartBurnDown;
        private Label lblStatusWarning;

        public DashboardForm(List<TaskItem> tasks)
        {
            _tasks = tasks;
            SetupUI();
            DrawChart();
        }

        private void SetupUI()
        {
            this.Text = "數據視覺化儀表板 - Dashboard";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            var lblHeatmapTitle = new Label { Text = "每日完成熱力圖 (過去 12 週)", Location = new Point(20, 20), AutoSize = true, Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold) };
            pbHeatmap = new PictureBox { Location = new Point(20, 50), Size = new Size(740, 150) };
            pbHeatmap.Paint += PbHeatmap_Paint;

            lblStatusWarning = new Label { Location = new Point(20, 220), AutoSize = true, Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold) };

            var lblChartTitle = new Label { Text = "任務積壓警告圖 (過去 14 天)", Location = new Point(20, 260), AutoSize = true, Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold) };
            chartBurnDown = new Chart { Location = new Point(20, 290), Size = new Size(740, 250) };

            this.Controls.Add(lblHeatmapTitle);
            this.Controls.Add(pbHeatmap);
            this.Controls.Add(lblStatusWarning);
            this.Controls.Add(lblChartTitle);
            this.Controls.Add(chartBurnDown);
        }

        private void PbHeatmap_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-(12 * 7)).AddDays(-(int)today.DayOfWeek + 1); // Start on Monday 12 weeks ago

            var completedCounts = _tasks
                .Where(t => t.IsCompleted && t.CompletedDate.HasValue && t.CompletedDate.Value >= startDate)
                .GroupBy(t => t.CompletedDate.Value.Date)
                .ToDictionary(k => k.Key, v => v.Count());

            int cellSize = 15;
            int cellPadding = 3;
            
            for (int col = 0; col <= 12; col++)
            {
                for (int row = 0; row < 7; row++)
                {
                    DateTime cellDate = startDate.AddDays(col * 7 + row);
                    if (cellDate > today) break;

                    int count = 0;
                    if (completedCounts.ContainsKey(cellDate))
                        count = completedCounts[cellDate];

                    Color cellColor = Color.FromArgb(235, 237, 240); // Grey
                    if (count >= 3) cellColor = Color.FromArgb(33, 110, 57); // Dark Green
                    else if (count >= 1) cellColor = Color.FromArgb(64, 196, 99); // Light Green

                    using (Brush brush = new SolidBrush(cellColor))
                    {
                        g.FillRectangle(brush, col * (cellSize + cellPadding), row * (cellSize + cellPadding), cellSize, cellSize);
                    }
                }
            }
        }

        private void DrawChart()
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-13);

            var area = new ChartArea();
            area.AxisX.LabelStyle.Format = "MM/dd";
            area.AxisX.Interval = 2;
            area.AxisY.Title = "任務數";
            chartBurnDown.ChartAreas.Add(area);

            var seriesA = new Series("累積未完成") { ChartType = SeriesChartType.Line, Color = Color.Red, BorderWidth = 2 };
            var seriesB = new Series("新完成") { ChartType = SeriesChartType.Line, Color = Color.Green, BorderWidth = 2 };

            int totalNewLast7Days = 0;
            int totalCompletedLast7Days = 0;

            var createdCounts = _tasks.GroupBy(t => t.CreatedDate.Date).ToDictionary(g => g.Key, g => g.Count());
            var completedCounts = _tasks.Where(t => t.IsCompleted && t.CompletedDate.HasValue)
                                        .GroupBy(t => t.CompletedDate.Value.Date)
                                        .ToDictionary(g => g.Key, g => g.Count());

            int runningCreated = _tasks.Count(t => t.CreatedDate.Date < startDate);
            int runningCompleted = _tasks.Count(t => t.IsCompleted && t.CompletedDate.HasValue && t.CompletedDate.Value.Date < startDate);

            for (int i = 0; i <= 13; i++)
            {
                DateTime day = startDate.AddDays(i);

                int createdToday = createdCounts.ContainsKey(day) ? createdCounts[day] : 0;
                int completedToday = completedCounts.ContainsKey(day) ? completedCounts[day] : 0;

                runningCreated += createdToday;
                runningCompleted += completedToday;

                int backlogCount = runningCreated - runningCompleted;

                seriesA.Points.AddXY(day, backlogCount);
                seriesB.Points.AddXY(day, completedToday);

                if (day >= today.AddDays(-6))
                {
                    totalNewLast7Days += createdToday;
                    totalCompletedLast7Days += completedToday;
                }
            }

            chartBurnDown.Series.Add(seriesA);
            chartBurnDown.Series.Add(seriesB);
            
            var legend = new Legend();
            chartBurnDown.Legends.Add(legend);

            double avgNew = totalNewLast7Days / 7.0;
            double avgCompleted = totalCompletedLast7Days / 7.0;

            if (avgNew > avgCompleted)
            {
                lblStatusWarning.Text = "⚠️ 警告：任務累積速度快於完成速度，請留意行程安排避免過載！";
                lblStatusWarning.ForeColor = Color.Red;
            }
            else
            {
                lblStatusWarning.Text = "✅ 任務處理順暢，繼續保持！";
                lblStatusWarning.ForeColor = Color.Green;
            }
        }
    }

    // ====== Focus Mode Form ======
    public class FocusForm : Form
    {
        private TaskItem _focusTask;
        private Timer _timer;
        private int _timeLeftSeconds = 25 * 60; // 25 minutes
        private Label lblTaskName;
        private Label lblTimer;
        private Button btnToggle;
        private Button btnExit;
        private Button btnFinish;
        private bool _isRunning = false;

        public FocusForm(TaskItem task)
        {
            _focusTask = task;
            SetupUI();
            
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
        }

        private void SetupUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            lblTaskName = new Label
            {
                Text = $"正在專注：{_focusTask.Content}",
                Font = new Font(this.Font.FontFamily, 24, FontStyle.Regular),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblTimer = new Label
            {
                Text = FormatTime(_timeLeftSeconds),
                Font = new Font(this.Font.FontFamily, 72, FontStyle.Bold),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnToggle = new Button
            {
                Text = "開始 (Play)",
                Font = new Font(this.Font.FontFamily, 16),
                Size = new Size(150, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            btnToggle.Click += BtnToggle_Click;

            btnExit = new Button
            {
                Text = "放棄並退出 (Exit)",
                Font = new Font(this.Font.FontFamily, 16),
                Size = new Size(220, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            btnExit.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            btnFinish = new Button
            {
                Text = "完成任務並退出 (Done)",
                Font = new Font(this.Font.FontFamily, 16),
                Size = new Size(260, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            btnFinish.Click += (s, e) => {
                _timer.Stop();
                this.DialogResult = DialogResult.OK;
            };

            this.Controls.Add(lblTaskName);
            this.Controls.Add(lblTimer);
            this.Controls.Add(btnToggle);
            this.Controls.Add(btnExit);
            this.Controls.Add(btnFinish);

            this.Load += FocusForm_Load;
            this.Resize += FocusForm_Resize;
        }

        private void FocusForm_Load(object sender, EventArgs e)
        {
            CenterControls();
        }

        private void FocusForm_Resize(object sender, EventArgs e)
        {
            CenterControls();
        }

        private void CenterControls()
        {
            lblTaskName.Location = new Point((this.ClientSize.Width - lblTaskName.Width) / 2, this.ClientSize.Height / 2 - 150);
            lblTimer.Location = new Point((this.ClientSize.Width - lblTimer.Width) / 2, this.ClientSize.Height / 2 - 80);
            
            int btnY = this.ClientSize.Height / 2 + 50;
            int gap = 20;
            int totalBtnWidth = btnToggle.Width + btnExit.Width + btnFinish.Width + 2 * gap;

            int startX = (this.ClientSize.Width - totalBtnWidth) / 2;
            btnToggle.Location = new Point(startX, btnY);
            btnExit.Location = new Point(btnToggle.Right + gap, btnY);
            btnFinish.Location = new Point(btnExit.Right + gap, btnY);
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                _timer.Stop();
                btnToggle.Text = "開始 (Play)";
            }
            else
            {
                _timer.Start();
                btnToggle.Text = "暫停 (Pause)";
            }
            _isRunning = !_isRunning;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timeLeftSeconds--;
            lblTimer.Text = FormatTime(_timeLeftSeconds);

            if (_timeLeftSeconds <= 0)
            {
                _timer.Stop();
                MessageBox.Show("25 分鐘專注完成！請休息 5 分鐘。", "專注完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
        }

        private string FormatTime(int seconds)
        {
            int m = seconds / 60;
            int s = seconds % 60;
            return $"{m:D2}:{s:D2}";
        }
    }

    // ====== Help Dialog Form ======
    public class HelpForm : Form
    {
        private TabControl tabControl;
        private TabPage tabBasic;
        private TabPage tabMagic;
        private TabPage tabFocus;
        private Button btnOk;

        public HelpForm()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "新手教學指南";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            tabControl = new TabControl
            {
                Location = new Point(15, 15),
                Size = new Size(550, 340),
                Font = new Font("微軟正黑體", 11)
            };

            tabBasic = new TabPage("📝 基礎操作");
            tabMagic = new TabPage("✨ 魔法符號");
            tabFocus = new TabPage("🧘 專注與圖表");

            tabControl.TabPages.Add(tabBasic);
            tabControl.TabPages.Add(tabMagic);
            tabControl.TabPages.Add(tabFocus);

            tabBasic.Controls.Add(CreateRichTextBox(@"
歡迎使用代辦事項管理程式！
這裡可以幫您輕鬆管理每天的大小事。

【新增任務】
點擊左上角的「新增任務」按鈕，可以輸入您想做的事情。

【完成任務】
只要在表格中點擊任務左邊的方塊打勾，任務就會標示為「已完成」。

【子任務】
如果您在建立任務時有加入「子任務」，主畫面上任務的最前方會出現一個小箭頭 (▼ 或 ▶)。
點一下箭頭，就可以展開底下所有的小項目囉！"));

            tabMagic.Controls.Add(CreateRichTextBox(@"
當您在「新增任務」的視窗時，可以為任務加上許多好用的屬性，讓畫面更清楚！

#標籤：可以幫任務加上分類，例如「工作」、「家庭」。
在畫面上方的「標籤篩選」中，就能一次找出同分類的任務。

@日期：設定任務的截止日期。如果您快要遲到了，任務文字會變成紅色的提醒您！

!優先級：設定「High(高)」、「Medium(中)」、「Low(低)」來決定任務的重要程度。

&顏色：在新增視窗裡可以直接點擊「選色」按鈕，幫重要的任務塗上背景顏色，一眼就能看到！

~重複：設定每天或每週要做的固定工作，完成一次後，系統會自動幫您產生下一次的任務！"));

            tabFocus.Controls.Add(CreateRichTextBox(@"
【進入專注模式】
覺得容易分心嗎？選擇一個任務，點擊上方的「進入專注模式」。
畫面會變成全螢幕的黑色計時器，倒數 25 分鐘，幫助您不受干擾地完成工作。
時間到會跳出提醒，並自動將該任務設為完成！

【查看儀表板】
想知道自己最近有多努力嗎？點擊「查看儀表板」。
這裡有一張「每日完成熱力圖」，綠色越多代表您那幾天完成的任務越多。
還有「任務積壓警告圖」，如果紅線比綠線高，代表任務堆積太快囉，該休息一下重新安排行程了！"));

            btnOk = new Button
            {
                Text = "我知道了 (OK)",
                Font = new Font("微軟正黑體", 12),
                Size = new Size(150, 40),
                Location = new Point(225, 365)
            };
            btnOk.Click += (s, e) => this.Close();

            this.Controls.Add(tabControl);
            this.Controls.Add(btnOk);
        }

        private RichTextBox CreateRichTextBox(string text)
        {
            var rtb = new RichTextBox
            {
                Text = text.Trim(),
                Dock = DockStyle.Fill,
                Font = new Font("微軟正黑體", 12),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                BackColor = SystemColors.Window
            };
            return rtb;
        }
    }
}
