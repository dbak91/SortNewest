namespace SortNewest;

internal static class PickerSettings
{
    private const string SortOrderKey = "sort_order";

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

    public static PickerSortOrder SortOrder
    {
        get
        {
            string saved = Preferences.Default.Get(SortOrderKey, PickerSortOptions.Default.Order.ToString());
            return Enum.TryParse(saved, out PickerSortOrder order) &&
                   Enum.IsDefined(typeof(PickerSortOrder), order)
                ? order
                : PickerSortOptions.Default.Order;
        }
        set => Preferences.Default.Set(SortOrderKey, value.ToString());
    }
}
