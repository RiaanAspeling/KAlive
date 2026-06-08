namespace KAlive;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "Global\\KAlive.SingleInstance", out bool createdNew);
        if (!createdNew) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }
}
