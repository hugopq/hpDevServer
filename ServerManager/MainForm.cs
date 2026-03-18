namespace ServerManager;

using System.Diagnostics;

public class MainForm : Form
{
    private readonly Dictionary<string, (Label Status, Button Start, Button Stop)> _rows = [];
    private readonly System.Windows.Forms.Timer _timer;
    private RichTextBox _log = null!;

    public MainForm()
    {
        Text            = "hpDevServer Manager";
        Size            = new Size(580, 530);
        MinimumSize     = new Size(580, 530);
        MaximumSize     = new Size(580, 530);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = Color.FromArgb(240, 240, 245);
        ForeColor       = Color.FromArgb(30, 30, 30);

        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("hpdev.ico");
        if (stream != null)
            Icon = new Icon(stream);

        BuildUI();

        _timer = new System.Windows.Forms.Timer { Interval = 3000 };
        _timer.Tick += async (s, e) => await RefreshAsync();
        _timer.Start();

        _ = RefreshAsync();
    }

    private void BuildUI()
    {
        var outer = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            Padding     = new Padding(16, 12, 16, 12),
            RowCount    = DockerHelper.Services.Length + 3, // header + services + log + bar
            ColumnCount = 4,
            BackColor   = Color.Transparent,
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        // Header row
        outer.Controls.Add(MakeLabel("Serviço", Color.FromArgb(100, 100, 110), bold: true), 0, 0);
        outer.Controls.Add(MakeLabel("Estado",  Color.FromArgb(100, 100, 110), bold: true), 1, 0);
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        // Service rows
        for (int i = 0; i < DockerHelper.Services.Length; i++)
        {
            var (svcDisplay, _) = DockerHelper.Services[i];

            var statusLbl = MakeLabel("...", Color.Gray);
            var startBtn  = MakeButton("Start", Color.FromArgb(45, 140, 45));
            var stopBtn   = MakeButton("Stop",  Color.FromArgb(160, 45, 45));

            var svc = svcDisplay;
            startBtn.Click += async (s, e) =>
            {
                SetRow(svc, enabled: false);
                SetStatus(svc, "A iniciar...", Color.Goldenrod);
                AppendLog($"[{svc}] A iniciar...");
                await DockerHelper.StartServiceAsync(svc, line => AppendLog(line));
                await RefreshAsync();
                SetRow(svc, enabled: true);
            };
            stopBtn.Click += async (s, e) =>
            {
                SetRow(svc, enabled: false);
                SetStatus(svc, "A parar...", Color.Goldenrod);
                AppendLog($"[{svc}] A parar...");
                await DockerHelper.StopServiceAsync(svc, line => AppendLog(line));
                await RefreshAsync();
                SetRow(svc, enabled: true);
            };

            _rows[svc] = (statusLbl, startBtn, stopBtn);

            outer.Controls.Add(MakeLabel(svcDisplay, Color.FromArgb(30, 30, 30)), 0, i + 1);
            outer.Controls.Add(statusLbl, 1, i + 1);
            outer.Controls.Add(startBtn,  2, i + 1);
            outer.Controls.Add(stopBtn,   3, i + 1);
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        }

        // Log panel
        _log = new RichTextBox
        {
            Dock          = DockStyle.Fill,
            BackColor     = Color.FromArgb(30, 30, 35),
            ForeColor     = Color.FromArgb(200, 200, 200),
            Font          = new Font("Consolas", 8f),
            ReadOnly      = true,
            BorderStyle   = BorderStyle.None,
            ScrollBars    = RichTextBoxScrollBars.Vertical,
            Margin        = new Padding(0, 6, 0, 4),
        };
        int logRow = DockerHelper.Services.Length + 1;
        outer.Controls.Add(_log, 0, logRow);
        outer.SetColumnSpan(_log, 4);
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));

        // Bottom action bar
        var bar = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0, 6, 0, 0),
        };

        var startAll = MakeButton("Start All", Color.FromArgb(45, 140, 45), width: 90);
        var stopAll  = MakeButton("Stop All",  Color.FromArgb(160, 45, 45), width: 90);
        var backup   = MakeButton("Backup DB", Color.FromArgb(30, 100, 180), width: 90);
        var phpLink  = new LinkLabel
        {
            Text      = "Abrir phpMyAdmin",
            ForeColor = Color.CornflowerBlue,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Padding   = new Padding(6, 6, 0, 0),
        };

        startAll.Click += async (s, e) =>
        {
            startAll.Enabled = stopAll.Enabled = false;
            SetAllStatuses("A iniciar...", Color.Goldenrod);
            AppendLog("--- Start All ---");
            await DockerHelper.StartAllAsync(line => AppendLog(line));
            await RefreshAsync();
            startAll.Enabled = stopAll.Enabled = true;
        };
        stopAll.Click += async (s, e) =>
        {
            startAll.Enabled = stopAll.Enabled = false;
            SetAllStatuses("A parar...", Color.Goldenrod);
            AppendLog("--- Stop All ---");
            await DockerHelper.StopAllAsync(line => AppendLog(line));
            await RefreshAsync();
            startAll.Enabled = stopAll.Enabled = true;
        };
        backup.Click += async (s, e) =>
        {
            backup.Enabled = false;
            backup.Text = "A fazer...";
            AppendLog("--- Backup iniciado ---");
            try
            {
                var filePath = await DockerHelper.RunBackupAsync();
                AppendLog($"Backup guardado: {filePath}");
                MessageBox.Show($"Backup guardado em:\n{filePath}", "Backup concluído",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"Erro no backup: {ex.Message}");
                MessageBox.Show($"Erro ao fazer backup:\n{ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                backup.Enabled = true;
                backup.Text = "Backup DB";
            }
        };
        phpLink.Click += (s, e) =>
            Process.Start(new ProcessStartInfo("http://localhost:8080") { UseShellExecute = true });

        bar.Controls.AddRange([startAll, stopAll, backup, phpLink]);

        int lastRow = DockerHelper.Services.Length + 2;
        outer.Controls.Add(bar, 0, lastRow);
        outer.SetColumnSpan(bar, 4);
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(outer);
    }

    public async Task RefreshAsync()
    {
        var statuses = await DockerHelper.GetStatusesAsync();
        RefreshStatuses(statuses);
    }

    public void RefreshStatuses(Dictionary<string, bool> statuses)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { Invoke(() => RefreshStatuses(statuses)); return; }

        foreach (var (svc, running) in statuses)
        {
            if (!_rows.TryGetValue(svc, out var row)) continue;
            row.Status.Text      = running ? "A correr" : "Parado";
            row.Status.ForeColor = running ? Color.LimeGreen : Color.IndianRed;
        }
    }

    private void AppendLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (_log.IsDisposed) return;
        if (_log.InvokeRequired) { _log.Invoke(() => AppendLog(message)); return; }

        _log.AppendText(message + "\n");
        _log.ScrollToCaret();
    }

    private void SetStatus(string svc, string text, Color color)
    {
        if (!_rows.TryGetValue(svc, out var row)) return;
        if (InvokeRequired) { Invoke(() => SetStatus(svc, text, color)); return; }
        row.Status.Text      = text;
        row.Status.ForeColor = color;
    }

    private void SetAllStatuses(string text, Color color)
    {
        foreach (var svc in _rows.Keys)
            SetStatus(svc, text, color);
    }

    private void SetRow(string svc, bool enabled)
    {
        if (!_rows.TryGetValue(svc, out var row)) return;
        if (InvokeRequired) { Invoke(() => SetRow(svc, enabled)); return; }
        row.Start.Enabled = row.Stop.Enabled = enabled;
    }

    private static Label MakeLabel(string text, Color color, bool bold = false) => new()
    {
        Text      = text,
        ForeColor = color,
        Font      = new Font("Segoe UI", bold ? 11f : 9.5f, bold ? FontStyle.Bold : FontStyle.Regular),
        Dock      = DockStyle.Fill,
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static Button MakeButton(string text, Color back, int width = 80) => new()
    {
        Text      = text,
        Width     = width,
        Height    = 42,
        BackColor = back,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Margin    = new Padding(2, 4, 2, 4),
    };
}
