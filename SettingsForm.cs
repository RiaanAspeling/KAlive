namespace KAlive;

internal class SettingsForm : Form
{
    private readonly Settings _settings;
    private readonly NumericUpDown _initialIdle;
    private readonly NumericUpDown _intervalMin;
    private readonly NumericUpDown _intervalMax;
    private readonly CheckBox _scheduleEnabled;
    private readonly DateTimePicker _wakeAt;
    private readonly DateTimePicker _sleepAt;

    public SettingsForm(Settings settings)
    {
        _settings = settings;
        Text = "KAlive Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(380, 380);

        // Idle behavior group
        var grpIdle = new GroupBox
        {
            Text = "Idle behavior",
            Left = 12, Top = 8, Width = 356, Height = 170
        };

        var lblInitial = new Label { Text = "Initial idle threshold (seconds):", Left = 12, Top = 24, AutoSize = true };
        _initialIdle = new NumericUpDown { Left = 210, Top = 20, Width = 130, Minimum = 10, Maximum = 7200, Value = settings.InitialIdleThresholdSeconds };

        var noteInitial = new Label
        {
            Text = "How long real inactivity must persist before keep-awake starts.",
            Left = 12, Top = 48, Width = 328, AutoSize = false, Height = 16,
            ForeColor = SystemColors.GrayText
        };

        var lblMin = new Label { Text = "Injection interval min (seconds):", Left = 12, Top = 76, AutoSize = true };
        _intervalMin = new NumericUpDown { Left = 210, Top = 72, Width = 130, Minimum = 5, Maximum = 3600, Value = settings.InjectionIntervalMinSeconds };

        var lblMax = new Label { Text = "Injection interval max (seconds):", Left = 12, Top = 108, AutoSize = true };
        _intervalMax = new NumericUpDown { Left = 210, Top = 104, Width = 130, Minimum = 5, Maximum = 3600, Value = settings.InjectionIntervalMaxSeconds };

        var noteInterval = new Label
        {
            Text = "Gap between subsequent F-keys (random in [Min, Max]).",
            Left = 12, Top = 132, Width = 328, AutoSize = false, Height = 16,
            ForeColor = SystemColors.GrayText
        };

        grpIdle.Controls.AddRange(new Control[] { lblInitial, _initialIdle, noteInitial, lblMin, _intervalMin, lblMax, _intervalMax, noteInterval });

        // Schedule group
        var grpSchedule = new GroupBox
        {
            Text = "Schedule",
            Left = 12, Top = 188, Width = 356, Height = 140
        };

        _scheduleEnabled = new CheckBox
        {
            Text = "Enable schedule",
            Left = 12, Top = 24, AutoSize = true,
            Checked = settings.ScheduleEnabled
        };

        var lblWake = new Label { Text = "Resume at:", Left = 32, Top = 56, AutoSize = true };
        _wakeAt = new DateTimePicker
        {
            Left = 130, Top = 52, Width = 100,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = TimeToDateTime(settings.WakeAt, 6, 0)
        };

        var lblSleep = new Label { Text = "Pause at:", Left = 32, Top = 88, AutoSize = true };
        _sleepAt = new DateTimePicker
        {
            Left = 130, Top = 84, Width = 100,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = TimeToDateTime(settings.SleepAt, 16, 0)
        };

        var noteSchedule = new Label
        {
            Text = "Boundary-triggered: a manual pause/resume inside the window will stick.",
            Left = 12, Top = 112, Width = 328, AutoSize = false, Height = 16,
            ForeColor = SystemColors.GrayText
        };

        _scheduleEnabled.CheckedChanged += (_, _) =>
        {
            _wakeAt.Enabled = _scheduleEnabled.Checked;
            _sleepAt.Enabled = _scheduleEnabled.Checked;
        };
        _wakeAt.Enabled = _scheduleEnabled.Checked;
        _sleepAt.Enabled = _scheduleEnabled.Checked;

        grpSchedule.Controls.AddRange(new Control[] { _scheduleEnabled, lblWake, _wakeAt, lblSleep, _sleepAt, noteSchedule });

        // Buttons
        var ok = new Button { Text = "OK", Left = 192, Top = 340, Width = 80, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 280, Top = 340, Width = 80, DialogResult = DialogResult.Cancel };

        ok.Click += (_, _) =>
        {
            int initial = (int)_initialIdle.Value;
            int min = (int)_intervalMin.Value;
            int max = (int)_intervalMax.Value;
            if (max < min)
            {
                MessageBox.Show(this, "Injection interval Max must be ≥ Min.", "KAlive", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            _settings.InitialIdleThresholdSeconds = initial;
            _settings.InjectionIntervalMinSeconds = min;
            _settings.InjectionIntervalMaxSeconds = max;
            _settings.ScheduleEnabled = _scheduleEnabled.Checked;
            _settings.WakeAt = _wakeAt.Value.ToString("HH:mm");
            _settings.SleepAt = _sleepAt.Value.ToString("HH:mm");
        };

        Controls.AddRange(new Control[] { grpIdle, grpSchedule, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static DateTime TimeToDateTime(string s, int defaultHour, int defaultMinute)
    {
        var today = DateTime.Today;
        return TimeOnly.TryParse(s, out var t)
            ? today.Add(t.ToTimeSpan())
            : today.AddHours(defaultHour).AddMinutes(defaultMinute);
    }
}
