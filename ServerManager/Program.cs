namespace ServerManager;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}

class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private MainForm? _mainForm;
    private readonly System.Windows.Forms.Timer _timer;

    public TrayApplicationContext()
    {
        _trayIcon = new NotifyIcon
        {
            Text = "hpDevServer",
            Icon = CreateIcon(Color.Gray),
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
        _trayIcon.DoubleClick += (s, e) => ShowMainForm();

        _timer = new System.Windows.Forms.Timer { Interval = 5000 };
        _timer.Tick += async (s, e) => await UpdateTrayIconAsync();
        _timer.Start();

        _ = UpdateTrayIconAsync();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir", null, (s, e) => ShowMainForm());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Start All", null, async (s, e) => await DockerHelper.StartAllAsync());
        menu.Items.Add("Stop All", null, async (s, e) => await DockerHelper.StopAllAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair", null, (s, e) => { _trayIcon.Visible = false; Application.Exit(); });
        return menu;
    }

    public void ShowMainForm()
    {
        if (_mainForm == null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm();
            _mainForm.FormClosed += (s, e) => _mainForm = null;
        }
        _mainForm.Show();
        _mainForm.BringToFront();
        _mainForm.WindowState = FormWindowState.Normal;
    }

    public async Task UpdateTrayIconAsync()
    {
        var statuses = await DockerHelper.GetStatusesAsync();
        int running = statuses.Values.Count(v => v);
        Color color = running == 0 ? Color.Red : running == statuses.Count ? Color.LimeGreen : Color.Orange;
        _trayIcon.Icon = CreateIcon(color);
        _trayIcon.Text = $"hpDevServer — {running}/{statuses.Count} a correr";
        _mainForm?.RefreshStatuses(statuses);
    }

    private static Icon CreateIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 13, 13);
        return Icon.FromHandle(bmp.GetHicon());
    }
}
