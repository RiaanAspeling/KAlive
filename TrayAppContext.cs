namespace KAlive;

internal class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Settings _settings;
    private readonly IdleMonitor _monitor;
    private readonly System.Windows.Forms.Timer _tooltipTimer;
    private Icon? _currentIcon;

    private readonly ToolStripMenuItem _headerItem;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _startupItem;

    public TrayAppContext()
    {
        _settings = Settings.Load();
        _monitor = new IdleMonitor(_settings);

        _headerItem = new ToolStripMenuItem("KAlive") { Enabled = false };
        _pauseItem = new ToolStripMenuItem("Pause", null, OnPauseToggle);
        _startupItem = new ToolStripMenuItem("Start with Windows", null, OnStartupToggle)
        {
            Checked = Settings.GetStartWithWindows(),
            CheckOnClick = false
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_headerItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Settings...", null, OnSettings));
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, OnExit));

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Visible = true,
            Text = "KAlive"
        };

        _monitor.StateChanged += (_, state) =>
        {
            UpdateIcon(state);
            UpdatePauseLabel();
        };
        UpdateIcon(_monitor.State);
        UpdatePauseLabel();

        _tooltipTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _tooltipTimer.Tick += (_, _) => UpdateTooltip();
        _tooltipTimer.Start();
        UpdateTooltip();
    }

    private void UpdateIcon(MonitorState state)
    {
        var newIcon = IconFactory.Create(state);
        _notifyIcon.Icon = newIcon;
        _currentIcon?.Dispose();
        _currentIcon = newIcon;
        _headerItem.Text = state switch
        {
            MonitorState.Paused => "KAlive — Paused",
            MonitorState.Watching => "KAlive — Watching",
            MonitorState.KeepingAwake => "KAlive — Keeping awake",
            _ => "KAlive"
        };
    }

    private void UpdateTooltip()
    {
        string suffix = _monitor.State switch
        {
            MonitorState.Paused => "paused",
            MonitorState.KeepingAwake => $"keeping awake — next in {_monitor.SecondsUntilNextFire}s",
            _ => $"idle {FormatDuration(_monitor.IdleSeconds)}"
        };
        _notifyIcon.Text = $"KAlive — {suffix}";
    }

    private static string FormatDuration(uint seconds)
    {
        if (seconds < 60) return $"{seconds}s";
        return $"{seconds / 60}m {seconds % 60}s";
    }

    private void OnPauseToggle(object? sender, EventArgs e)
    {
        _settings.Enabled = !_settings.Enabled;
        _settings.Save();
        _monitor.ApplyEnabledState();
        UpdatePauseLabel();
        UpdateTooltip();
    }

    private void UpdatePauseLabel()
    {
        _pauseItem.Text = _settings.Enabled ? "Pause" : "Resume";
    }

    private void OnStartupToggle(object? sender, EventArgs e)
    {
        bool newValue = !_startupItem.Checked;
        Settings.SetStartWithWindows(newValue);
        _startupItem.Checked = newValue;
    }

    private void OnSettings(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings.Save();
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Dispose();
            _monitor.Dispose();
            _tooltipTimer.Dispose();
            _currentIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
