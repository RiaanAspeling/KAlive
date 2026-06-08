using System.Runtime.InteropServices;

namespace KAlive;

public enum MonitorState { Paused, Watching, KeepingAwake }

public class IdleMonitor : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Settings _settings;
    private readonly Random _random = new();
    private MonitorState _state = MonitorState.Watching;
    private uint _lastSyntheticTickCount; // 0 = never fired
    private int _nextIntervalSeconds; // current rolled gap between subsequent injections
    private DateTime _lastScheduleCheck; // default = not yet initialized

    public event EventHandler<MonitorState>? StateChanged;
    public MonitorState State => _state;
    public uint IdleSeconds { get; private set; }
    public int SecondsUntilNextFire { get; private set; }

    public IdleMonitor(Settings settings)
    {
        _settings = settings;
        _timer = new System.Windows.Forms.Timer { Interval = 5000 };
        _timer.Tick += OnTick;
        RollNextInterval();
        ApplyEnabledState();
        _timer.Start();
    }

    public void ApplyEnabledState()
    {
        SetState(_settings.Enabled ? MonitorState.Watching : MonitorState.Paused);
    }

    private void RollNextInterval()
    {
        int min = Math.Min(_settings.InjectionIntervalMinSeconds, _settings.InjectionIntervalMaxSeconds);
        int max = Math.Max(_settings.InjectionIntervalMinSeconds, _settings.InjectionIntervalMaxSeconds);
        _nextIntervalSeconds = _random.Next(min, max + 1);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        ApplySchedule();

        if (!_settings.Enabled)
        {
            SetState(MonitorState.Paused);
            return;
        }

        var (idleSeconds, lastInputTick) = GetIdleInfo();
        IdleSeconds = idleSeconds;

        bool inKeepAwakeMode = _lastSyntheticTickCount != 0
            && (int)(lastInputTick - _lastSyntheticTickCount) <= 100;

        if (inKeepAwakeMode)
        {
            int secondsSinceLastFire = (int)((Native.GetTickCount() - _lastSyntheticTickCount) / 1000);
            SecondsUntilNextFire = Math.Max(0, _nextIntervalSeconds - secondsSinceLastFire);

            if (secondsSinceLastFire >= _nextIntervalSeconds)
            {
                SendRandomFKey();
                _lastSyntheticTickCount = Native.GetTickCount();
                RollNextInterval();
                SecondsUntilNextFire = _nextIntervalSeconds;
            }
            SetState(MonitorState.KeepingAwake);
        }
        else
        {
            SecondsUntilNextFire = 0;
            if (idleSeconds >= (uint)_settings.InitialIdleThresholdSeconds)
            {
                SendRandomFKey();
                _lastSyntheticTickCount = Native.GetTickCount();
                RollNextInterval();
                SetState(MonitorState.KeepingAwake);
            }
            else
            {
                SetState(MonitorState.Watching);
            }
        }
    }

    private static (uint idleSeconds, uint lastInputTick) GetIdleInfo()
    {
        var lii = new Native.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<Native.LASTINPUTINFO>() };
        if (!Native.GetLastInputInfo(ref lii)) return (0, 0);
        return ((Native.GetTickCount() - lii.dwTime) / 1000, lii.dwTime);
    }

    private void SendRandomFKey()
    {
        const ushort VK_F15 = 0x7E;
        const ushort VK_F24 = 0x87;
        ushort vk = (ushort)_random.Next(VK_F15, VK_F24 + 1);

        var inputs = new Native.INPUT[]
        {
            new() { type = Native.INPUT_KEYBOARD, U = new() { ki = new() { wVk = vk } } },
            new() { type = Native.INPUT_KEYBOARD, U = new() { ki = new() { wVk = vk, dwFlags = Native.KEYEVENTF_KEYUP } } }
        };
        Native.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Native.INPUT>());
    }

    private void SetState(MonitorState newState)
    {
        if (_state == newState) return;
        _state = newState;
        StateChanged?.Invoke(this, newState);
    }

    private void ApplySchedule()
    {
        if (!_settings.ScheduleEnabled) { _lastScheduleCheck = default; return; }

        var now = DateTime.Now;
        bool isInWindow = IsWithinActiveWindow(now);

        if (_lastScheduleCheck == default)
        {
            // First tick with schedule on — snap Enabled to match the current window.
            if (_settings.Enabled != isInWindow)
            {
                _settings.Enabled = isInWindow;
                _settings.Save();
            }
        }
        else
        {
            // Only flip on boundary crossings, so manual overrides inside the window stick.
            bool wasInWindow = IsWithinActiveWindow(_lastScheduleCheck);
            if (wasInWindow != isInWindow)
            {
                _settings.Enabled = isInWindow;
                _settings.Save();
            }
        }

        _lastScheduleCheck = now;
    }

    private bool IsWithinActiveWindow(DateTime when)
    {
        var current = TimeOnly.FromDateTime(when);
        var wake = ParseTime(_settings.WakeAt, new TimeOnly(6, 0));
        var sleep = ParseTime(_settings.SleepAt, new TimeOnly(16, 0));
        if (wake == sleep) return false; // zero-length window
        return wake < sleep
            ? current >= wake && current < sleep
            : current >= wake || current < sleep; // crosses midnight
    }

    private static TimeOnly ParseTime(string s, TimeOnly fallback) =>
        TimeOnly.TryParse(s, out var t) ? t : fallback;

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
