using Android.Content;
using Android.Content.PM;
using Android.Content.Res;

namespace SortNewest;

internal sealed record PickerSortLabels(string SortMenu, string SortChoice, string Overflow)
{
    internal const string EnglishOverflow = "More options";

    internal static PickerSortLabels English(PickerSortOption option) =>
        new("Sort by...", option.EnglishPickerLabel, EnglishOverflow);

    internal static PickerSortLabels Load(Context context, string packageName, PickerSortOption option)
    {
        PickerSortLabels english = English(option);

        if (packageName is not ("com.google.android.documentsui" or "com.android.documentsui"))
            return english;

        try
        {
            using var resources = context.PackageManager?.GetResourcesForApplication(packageName);
            if (resources is null)
                return english;

            string Read(string name, string fallback)
            {
                int id = resources.GetIdentifier(name, "string", packageName);
                return id == 0 ? fallback : resources.GetString(id);
            }

            return new PickerSortLabels(
                Read("menu_sort", english.SortMenu),
                Read(option.PickerResourceName, english.SortChoice),
                Read("abc_action_menu_overflow_description", english.Overflow));
        }
        catch (PackageManager.NameNotFoundException)
        {
            return english;
        }
        catch (Resources.NotFoundException)
        {
            return english;
        }
    }

    internal static bool Matches(string? actual, string expected) =>
        string.Equals(actual?.Trim().Replace("…", "...", StringComparison.Ordinal),
            expected.Trim().Replace("…", "...", StringComparison.Ordinal), StringComparison.Ordinal);
}
