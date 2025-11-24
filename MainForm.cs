using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class MainForm : Form
{
    private Label lblAddr;
    private TextBox txtAddr;

    private Label lblFile;
    private TextBox txtInFile;
    private Button btnBrowse;

    private Label lblUrl;
    private TextBox txtUrl;

    private NumericUpDown numPort;
    private NumericUpDown numTimeout;
    private NumericUpDown numThreads;
    private CheckBox chkVerbose;
    private CheckBox chkIPv6;
    private Button btnRun;

    private Label lblPort;
    private Label lblTimeout;
    private Label lblThreads;
    private GroupBox gbParams;
    private GroupBox gbOptions;
    private TableLayoutPanel tblParams;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;

    private Process? runningProcess;

    public MainForm()
    {
        InitializeComponent();
        this.FormClosing += MainForm_FormClosing;
        _ = CheckAndDownloadGeoDB_Async();
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        lblAddr = new Label();
        txtAddr = new TextBox();
        lblFile = new Label();
        txtInFile = new TextBox();
        btnBrowse = new Button();
        lblUrl = new Label();
        txtUrl = new TextBox();
        gbParams = new GroupBox();
        tblParams = new TableLayoutPanel();
        lblPort = new Label();
        numPort = new NumericUpDown();
        lblThreads = new Label();
        numThreads = new NumericUpDown();
        lblTimeout = new Label();
        numTimeout = new NumericUpDown();
        gbOptions = new GroupBox();
        chkIPv6 = new CheckBox();
        chkVerbose = new CheckBox();
        btnRun = new Button();
        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();
        gbParams.SuspendLayout();
        tblParams.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numPort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numThreads).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numTimeout).BeginInit();
        gbOptions.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        
        lblAddr.AutoSize = true;
        lblAddr.Location = new Point(12, 16);
        lblAddr.Name = "lblAddr";
        lblAddr.Size = new Size(74, 23);
        lblAddr.TabIndex = 0;
        lblAddr.Text = "Address:";
        
        txtAddr.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAddr.BackColor = Color.FromArgb(51, 51, 55);
        txtAddr.BorderStyle = BorderStyle.FixedSingle;
        txtAddr.ForeColor = Color.Gainsboro;
        txtAddr.Location = new Point(16, 42);
        txtAddr.Name = "txtAddr";
        txtAddr.PlaceholderText = "Single target IP/Domain";
        txtAddr.Size = new Size(352, 30);
        txtAddr.TabIndex = 1;
        
        lblFile.AutoSize = true;
        lblFile.ForeColor = Color.Gainsboro;
        lblFile.Location = new Point(12, 80);
        lblFile.Name = "lblFile";
        lblFile.Size = new Size(146, 23);
        lblFile.TabIndex = 2;
        lblFile.Text = "Or Load from File:";
        
        txtInFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtInFile.BackColor = Color.FromArgb(51, 51, 55);
        txtInFile.BorderStyle = BorderStyle.FixedSingle;
        txtInFile.ForeColor = Color.Gainsboro;
        txtInFile.Location = new Point(16, 106);
        txtInFile.Name = "txtInFile";
        txtInFile.PlaceholderText = "path/to/in.txt";
        txtInFile.Size = new Size(255, 30);
        txtInFile.TabIndex = 3;
        
        btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowse.BackColor = Color.FromArgb(63, 63, 70);
        btnBrowse.FlatAppearance.BorderSize = 0;
        btnBrowse.FlatStyle = FlatStyle.Flat;
        btnBrowse.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
        btnBrowse.ForeColor = Color.White;
        btnBrowse.Location = new Point(278, 105);
        btnBrowse.Name = "btnBrowse";
        btnBrowse.Size = new Size(90, 32);
        btnBrowse.TabIndex = 4;
        btnBrowse.Text = "Browse";
        btnBrowse.UseVisualStyleBackColor = false;
        btnBrowse.Click += btnBrowse_Click;
        
        lblUrl.AutoSize = true;
        lblUrl.ForeColor = Color.Gainsboro;
        lblUrl.Location = new Point(12, 144);
        lblUrl.Name = "lblUrl";
        lblUrl.Size = new Size(156, 23);
        lblUrl.TabIndex = 5;
        lblUrl.Text = "Or Crawl from URL:";
        
        txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtUrl.BackColor = Color.FromArgb(51, 51, 55);
        txtUrl.BorderStyle = BorderStyle.FixedSingle;
        txtUrl.ForeColor = Color.Gainsboro;
        txtUrl.Location = new Point(16, 170);
        txtUrl.Name = "txtUrl";
        txtUrl.PlaceholderText = "https://example.com/list";
        txtUrl.Size = new Size(352, 30);
        txtUrl.TabIndex = 6;
        
        gbParams.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        gbParams.Controls.Add(tblParams);
        gbParams.ForeColor = Color.Gainsboro;
        gbParams.Location = new Point(16, 214);
        gbParams.Name = "gbParams";
        gbParams.Size = new Size(352, 144);
        gbParams.TabIndex = 7;
        gbParams.TabStop = false;
        gbParams.Text = "Parameters";
        
        tblParams.ColumnCount = 2;
        tblParams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        tblParams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        tblParams.Controls.Add(lblPort, 0, 0);
        tblParams.Controls.Add(numPort, 1, 0);
        tblParams.Controls.Add(lblThreads, 0, 1);
        tblParams.Controls.Add(numThreads, 1, 1);
        tblParams.Controls.Add(lblTimeout, 0, 2);
        tblParams.Controls.Add(numTimeout, 1, 2);
        tblParams.Dock = DockStyle.Fill;
        tblParams.Location = new Point(3, 26);
        tblParams.Name = "tblParams";
        tblParams.RowCount = 3;
        tblParams.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        tblParams.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        tblParams.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        tblParams.Size = new Size(346, 115);
        tblParams.TabIndex = 0;
        
        lblPort.Anchor = AnchorStyles.Left;
        lblPort.AutoSize = true;
        lblPort.Location = new Point(3, 7);
        lblPort.Name = "lblPort";
        lblPort.Size = new Size(45, 23);
        lblPort.TabIndex = 0;
        lblPort.Text = "Port:";
        
        numPort.Anchor = AnchorStyles.Left;
        numPort.BackColor = Color.FromArgb(51, 51, 55);
        numPort.BorderStyle = BorderStyle.FixedSingle;
        numPort.ForeColor = Color.Gainsboro;
        numPort.Location = new Point(141, 4);
        numPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        numPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numPort.Name = "numPort";
        numPort.Size = new Size(202, 30);
        numPort.TabIndex = 8;
        numPort.Value = new decimal(new int[] { 443, 0, 0, 0 });
        
        lblThreads.Anchor = AnchorStyles.Left;
        lblThreads.AutoSize = true;
        lblThreads.Location = new Point(3, 45);
        lblThreads.Name = "lblThreads";
        lblThreads.Size = new Size(74, 23);
        lblThreads.TabIndex = 4;
        lblThreads.Text = "Threads:";
        
        numThreads.Anchor = AnchorStyles.Left;
        numThreads.BackColor = Color.FromArgb(51, 51, 55);
        numThreads.BorderStyle = BorderStyle.FixedSingle;
        numThreads.ForeColor = Color.Gainsboro;
        numThreads.Location = new Point(141, 42);
        numThreads.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
        numThreads.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numThreads.Name = "numThreads";
        numThreads.Size = new Size(202, 30);
        numThreads.TabIndex = 9;
        numThreads.Value = new decimal(new int[] { 10, 0, 0, 0 });
        
        lblTimeout.Anchor = AnchorStyles.Left;
        lblTimeout.AutoSize = true;
        lblTimeout.Location = new Point(3, 84);
        lblTimeout.Name = "lblTimeout";
        lblTimeout.Size = new Size(99, 23);
        lblTimeout.TabIndex = 8;
        lblTimeout.Text = "Timeout (s):";
        
        numTimeout.Anchor = AnchorStyles.Left;
        numTimeout.BackColor = Color.FromArgb(51, 51, 55);
        numTimeout.BorderStyle = BorderStyle.FixedSingle;
        numTimeout.ForeColor = Color.Gainsboro;
        numTimeout.Location = new Point(141, 80);
        numTimeout.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        numTimeout.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numTimeout.Name = "numTimeout";
        numTimeout.Size = new Size(202, 30);
        numTimeout.TabIndex = 10;
        numTimeout.Value = new decimal(new int[] { 5, 0, 0, 0 });
        
        gbOptions.Controls.Add(chkIPv6);
        gbOptions.Controls.Add(chkVerbose);
        gbOptions.ForeColor = Color.Gainsboro;
        gbOptions.Location = new Point(16, 364);
        gbOptions.Name = "gbOptions";
        gbOptions.Size = new Size(171, 100);
        gbOptions.TabIndex = 11;
        gbOptions.TabStop = false;
        gbOptions.Text = "Options";
        
        chkIPv6.AutoSize = true;
        chkIPv6.Location = new Point(15, 29);
        chkIPv6.Name = "chkIPv6";
        chkIPv6.Size = new Size(120, 27);
        chkIPv6.TabIndex = 12;
        chkIPv6.Text = "Enable IPv6";
        chkIPv6.UseVisualStyleBackColor = true;
        
        chkVerbose.AutoSize = true;
        chkVerbose.Location = new Point(15, 62);
        chkVerbose.Name = "chkVerbose";
        chkVerbose.Size = new Size(93, 27);
        chkVerbose.TabIndex = 13;
        chkVerbose.Text = "Verbose";
        chkVerbose.UseVisualStyleBackColor = true;
        
        btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRun.BackColor = Color.FromArgb(0, 122, 204);
        btnRun.Cursor = Cursors.Hand;
        btnRun.FlatAppearance.BorderSize = 0;
        btnRun.FlatStyle = FlatStyle.Flat;
        btnRun.ForeColor = Color.White;
        btnRun.Location = new Point(193, 376);
        btnRun.Name = "btnRun";
        btnRun.Size = new Size(175, 88);
        btnRun.TabIndex = 14;
        btnRun.Text = "Scan (CLI)";
        btnRun.UseVisualStyleBackColor = false;
        btnRun.Click += btnRun_Click;
        
        statusStrip.BackColor = Color.FromArgb(30, 30, 30);
        statusStrip.ImageScalingSize = new Size(20, 20);
        statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
        statusStrip.Location = new Point(0, 485);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(384, 26);
        statusStrip.SizingGrip = false;
        statusStrip.TabIndex = 15;
        statusStrip.Text = "statusStrip1";
        
        lblStatus.ForeColor = Color.Gray;
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(50, 20);
        lblStatus.Text = "Ready";
        
        AutoScaleDimensions = new SizeF(9F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(30, 30, 30);
        ClientSize = new Size(384, 511);
        Controls.Add(statusStrip);
        Controls.Add(btnRun);
        Controls.Add(gbOptions);
        Controls.Add(gbParams);
        Controls.Add(txtUrl);
        Controls.Add(lblUrl);
        Controls.Add(btnBrowse);
        Controls.Add(txtInFile);
        Controls.Add(lblFile);
        Controls.Add(txtAddr);
        Controls.Add(lblAddr);
        Font = new Font("Segoe UI", 10F);
        ForeColor = Color.Gainsboro;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "RealiTLScanner GUI";
        gbParams.ResumeLayout(false);
        tblParams.ResumeLayout(false);
        tblParams.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numPort).EndInit();
        ((System.ComponentModel.ISupportInitialize)numThreads).EndInit();
        ((System.ComponentModel.ISupportInitialize)numTimeout).EndInit();
        gbOptions.ResumeLayout(false);
        gbOptions.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Title = "Select Target List";
            ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            ofd.CheckFileExists = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInFile.Text = ofd.FileName;
                txtAddr.Text = "";
                txtUrl.Text = "";
            }
        }
    }

    private async void btnRun_Click(object sender, EventArgs e)
    {
        bool hasAddr = !string.IsNullOrWhiteSpace(txtAddr.Text);
        bool hasFile = !string.IsNullOrWhiteSpace(txtInFile.Text);
        bool hasUrl = !string.IsNullOrWhiteSpace(txtUrl.Text);

        if (!hasAddr && !hasFile && !hasUrl)
        {
            MessageBox.Show("Please specify a Target: Address, File, or URL.", "Missing Arguments", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnRun.Enabled = false;
        btnRun.Text = "Scanning...";
        lblStatus.Text = "Процесс сканирования запущен...";

        string exePath = "RealiTLScanner.exe";
        if (!File.Exists(exePath))
        {
            MessageBox.Show("RealiTLScanner.exe не найден рядом с GUI", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnRun.Enabled = true;
            btnRun.Text = "Scan (CLI)";
            lblStatus.Text = "Ошибка: .exe не найден";
            return;
        }

        StringBuilder args = new StringBuilder();

        if (hasUrl)
        {
            args.Append($" -url \"{txtUrl.Text}\"");
        }
        else if (hasFile)
        {
            args.Append($" -in \"{txtInFile.Text}\"");
        }
        else
        {
            args.Append($" -addr {txtAddr.Text}");
        }

        args.Append($" -port {numPort.Value}");
        args.Append($" -thread {numThreads.Value}");
        args.Append($" -timeout {numTimeout.Value}");
        args.Append(" -out result.csv");

        if (chkIPv6.Checked) args.Append(" -46");
        if (chkVerbose.Checked) args.Append(" -v");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args.ToString(),
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            runningProcess = new Process { StartInfo = psi };
            runningProcess.Start();

            await runningProcess.WaitForExitAsync();
            lblStatus.Text = "Сканирование завершено.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при запуске: {ex.Message}");
            lblStatus.Text = "Ошибка при запуске процесса.";
        }
        finally
        {
            runningProcess = null;
            btnRun.Enabled = true;
            btnRun.Text = "Scan (CLI)";

            await Task.Delay(3000);
            if (lblStatus.Text == "Сканирование завершено.")
                lblStatus.Text = "Готово";
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (runningProcess != null && !runningProcess.HasExited)
        {
            try { runningProcess.Kill(true); } catch { }
        }
    }

    private async Task CheckAndDownloadGeoDB_Async()
    {
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Country.mmdb");
        lblStatus.Text = "Проверка базы GeoDB...";

        if (File.Exists(dbPath))
        {
            await Task.Delay(1000);
            lblStatus.Text = "Готово (база GeoDB уже существует)";
            return;
        }

        var result = MessageBox.Show(
            "Файл Country.mmdb не найден. Загрузить его сейчас? (git.io/GeoLite2-Country.mmdb)",
            "Требуется файл",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.No)
        {
            lblStatus.Text = "Ошибка: Country.mmdb отсутствует.";
            return;
        }

        try
        {
            lblStatus.Text = "Загрузка Country.mmdb...";
            using var http = new HttpClient();
            var data = await http.GetByteArrayAsync("https://git.io/GeoLite2-Country.mmdb");
            await File.WriteAllBytesAsync(dbPath, data);

            MessageBox.Show("Файл Country.mmdb был успешно загружен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            lblStatus.Text = "Готово (база загружена)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось загрузить базу GeoLite:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            lblStatus.Text = "Ошибка загрузки GeoDB";
        }
    }
}
