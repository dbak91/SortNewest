using Android.Content;
using Android.Content.PM;
using Android.Content.Res;

namespace FilesNewest;

internal sealed record PickerSortLabels(string SortMenu, string NewestFirst, string Overflow)
{
    internal static PickerSortLabels English { get; } = new("Sort by...", "Modified (newest first)", "More options");

    internal static PickerSortLabels Load(Context context, string packageName)
    {
        if (packageName is not ("com.google.android.documentsui" or "com.android.documentsui"))
            return English;

        try
        {
            using var resources = context.PackageManager?.GetResourcesForApplication(packageName);
            if (resources is null)
                return English;

            string Read(string name, string fallback)
            {
                int id = resources.GetIdentifier(name, "string", packageName);
                return id == 0 ? fallback : resources.GetString(id);
            }

            return new PickerSortLabels(
                Read("menu_sort", English.SortMenu),
                Read("sort_dimension_date_descending", English.NewestFirst),
                Read("abc_action_menu_overflow_description", English.Overflow));
        }
        catch (PackageManager.NameNotFoundException)
        {
            return English;
        }
        catch (Resources.NotFoundException)
        {
            return English;
        }
    }

    internal static bool Matches(string? actual, string expected) =>
        string.Equals(actual?.Trim().Replace("…", "...", StringComparison.Ordinal),
            expected.Trim().Replace("…", "...", StringComparison.Ordinal), StringComparison.Ordinal);
}
