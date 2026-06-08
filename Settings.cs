using System.Text.Json;
using Microsoft.Win32;

namespace KAlive;

public class Settings
{
    public bool Enabled { get; set; } = true;
    public int InitialIdleThresholdSeconds { get; set; } = 240;
    public int InjectionIntervalMinSeconds { get; set; } = 60;
    public int InjectionIntervalMaxSeconds { get; set; } = 120;
    public bool ScheduleEnabled { get; set; } = false;
    public string WakeAt { get; set; } = "06:00";
    public string SleepAt { get; set; } = "16:00";

    private static string FilePath
    {
        get
        {
            string dir = Path.GetDirectoryName(Environment.ProcessPath ?? Application.ExecutablePath)
                         ?? AppContext.BaseDirectory;
            return Path.Combine(dir, "settings.json");
        }
    }

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch
        {
            // fall through to defaults
        }
        return new Settings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KAlive";

    public static bool GetStartWithWindows()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) != null;
    }

    public static void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;
        if (enabled)
        {
            var exe = Environment.ProcessPath ?? Application.ExecutablePath;
            key.SetValue(AppName, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
