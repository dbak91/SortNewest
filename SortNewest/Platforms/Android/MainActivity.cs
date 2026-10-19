using Android.App;
using Android.Content.PM;
using Android.Views;

namespace SortNewest;

[Activity(Name = "com.companyname.SortNewest.MainActivity", Theme = "@style/Maui.MainTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, Exported = true,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
