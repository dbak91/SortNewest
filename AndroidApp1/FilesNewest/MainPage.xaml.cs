using Android.Content;
using Android.Provider;
using Microsoft.Maui.Dispatching;

namespace FilesNewest;

public partial class MainPage : ContentPage
{
    private IDispatcherTimer? _statusTimer;
    private bool _refreshing;

    public MainPage()
    {
        InitializeComponent();
        _refreshing = true;
        MonitoringSwitch.IsToggled = PickerSettings.Enabled;
        _refreshing = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshStatus();
        _statusTimer ??= Dispatcher.CreateTimer();
        _statusTimer.Interval = TimeSpan.FromSeconds(1);
        _statusTimer.Tick -= OnStatusTick;
        _statusTimer.Tick += OnStatusTick;
        _statusTimer.Start();
    }

    protected override void OnDisappearing()
    {
        _statusTimer?.Stop();
        base.OnDisappearing();
    }

    private void OnStatusTick(object? sender, EventArgs e) => RefreshStatus();

    private async void OnMonitoringToggled(object? sender, ToggledEventArgs e)
    {
        if (_refreshing)
            return;

        if (e.Value && !await DisplayAlertAsync("Allow picker automation?",
            "Files Newest uses an accessibility service to inspect the stock Files picker’s visible controls and tap More options → Sort by... → Modified (newest first). It also receives window-change events to stop when you leave Files. It does not inspect other apps’ screen contents, access file contents, or transmit data. You can disable it at any time. Enable monitoring?",
            "Enable", "Cancel"))
        {
            _refreshing = true;
            MonitoringSwitch.IsToggled = false;
            _refreshing = false;
            return;
        }

        PickerSettings.Enabled = e.Value;
        PickerSettings.LastStatus = e.Value ? "Monitoring enabled. Waiting for Files and accessibility permission." : "Monitoring paused.";
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        MonitoringStatusLabel.Text = PickerSettings.Enabled ? "Monitoring enabled" : "Monitoring paused";
        var context = global::Android.App.Application.Context;
        string enabled = Settings.Secure.GetString(context.ContentResolver, Settings.Secure.EnabledAccessibilityServices) ?? string.Empty;
        bool authorized = enabled.Split(':').Any(component =>
            component.Equals("com.example.filesnewest/com.example.filesnewest.PickerSortService", StringComparison.OrdinalIgnoreCase) ||
            component.Equals("com.example.filesnewest/.PickerSortService", StringComparison.OrdinalIgnoreCase));
        AccessibilityStatusLabel.Text = PickerSortService.Connected ? "Accessibility service connected" :
            authorized ? "Accessibility enabled; waiting for Android to connect the service" : "Accessibility service not enabled";
        LastActionLabel.Text = PickerSettings.LastStatus;
    }

    private async void OnAccessibilitySettingsClicked(object? sender, EventArgs e)
    {
        try
        {
            StartActivity(new Intent(Settings.ActionAccessibilitySettings));
        }
        catch (Exception ex) when (ex is ActivityNotFoundException or Java.Lang.SecurityException)
        {
            await DisplayAlertAsync("Open Settings manually", "Open Android Settings → Accessibility → Downloaded apps → Files Newest — automatic sorting, then enable the service.", "OK");
        }
    }

    private async void OnTestPickerClicked(object? sender, EventArgs e)
    {
        try
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            StartActivity(intent);
        }
        catch (ActivityNotFoundException)
        {
            await DisplayAlertAsync("Picker unavailable", "No system file picker is installed or enabled on this device.", "OK");
        }
    }

    private static void StartActivity(Intent intent)
    {
        var activity = Platform.CurrentActivity;
        if (activity is not null)
            activity.StartActivity(intent);
        else
        {
            intent.AddFlags(ActivityFlags.NewTask);
            global::Android.App.Application.Context.StartActivity(intent);
        }
    }
}
