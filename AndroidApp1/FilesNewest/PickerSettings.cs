namespace FilesNewest;

internal static class PickerSettings
{
    public static bool Enabled
    {
        get => Preferences.Default.Get("monitoring_enabled", false);
        set => Preferences.Default.Set("monitoring_enabled", value);
    }

    public static string LastStatus
    {
        get => Preferences.Default.Get("last_status", "Not started. Enable monitoring and the accessibility service.");
        set => Preferences.Default.Set("last_status", value);
    }
}
