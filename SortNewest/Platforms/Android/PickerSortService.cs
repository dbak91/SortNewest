using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views.Accessibility;

namespace SortNewest;

[Service(Name = "com.companyname.SortNewest.PickerSortService", Label = "@string/picker_service_name",
    Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE", Exported = true)]
[IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
[MetaData("android.accessibilityservice", Resource = "@xml/picker_accessibility_service")]
public sealed class PickerSortService : AccessibilityService
{
    private enum Stage { Idle, Overflow, SortChoices, Confirm }

    private readonly Handler _handler = new(Looper.MainLooper!);
    private Stage _stage;
    private bool _scheduled;
    private bool _sessionCompleted;
    private int _polls;
    private PickerSortLabels _labels = PickerSortLabels.English;

    public static bool Connected { get; private set; }

    private static bool IsFiles(string? packageName) =>
        packageName is "com.google.android.documentsui" or "com.android.documentsui";

    protected override void OnServiceConnected()
    {
        base.OnServiceConnected();
        Connected = true;
        SetStatus("Accessibility connected. Waiting for the stock Files picker.");
        Schedule();
    }

    public override void OnAccessibilityEvent(AccessibilityEvent? e)
    {
        if (e is null)
            return;

        if (!PickerSettings.Enabled)
        {
            Reset();
            return;
        }

        if (!IsFiles(e.PackageName?.ToString()))
        {
            if (e.EventType == EventTypes.WindowStateChanged)
            {
                using var root = RootInActiveWindow;
                if (root is not null && !IsFiles(root.PackageName?.ToString()))
                    Reset();
            }
            return;
        }

        Schedule();
    }

    private void Schedule()
    {
        if (_scheduled || !PickerSettings.Enabled)
            return;

        _scheduled = true;
        _handler.PostDelayed(Tick, 300);
    }

    private void Tick()
    {
        _scheduled = false;
        if (!PickerSettings.Enabled)
        {
            Reset();
            return;
        }

        try
        {
            using var root = RootInActiveWindow;
            if (root is null)
                return;
            if (!IsFiles(root.PackageName?.ToString()))
            {
                Reset();
                return;
            }

            if (_stage == Stage.Idle)
            {
                if (_sessionCompleted)
                    return;

                _labels = PickerSortLabels.Load(this, root.PackageName!.ToString()!);

                // Never select items by matching arbitrary file names in the directory list.
                if (HasChoiceDialog(root))
                {
                    _stage = Stage.SortChoices;
                }
                else if (ClickSortMenu(root))
                {
                    _stage = Stage.SortChoices;
                    SetStatus("Opened Sort by...; waiting for newest-first option.");
                }
                else if (Visit(root, node => IsOverflow(node) && Click(node)))
                {
                    _stage = Stage.Overflow;
                    SetStatus("Opened Files options menu.");
                }
                else
                {
                    Finish("Files detected, but the More options control was not recognized. No action taken.");
                    return;
                }
                _polls = 0;
            }
            else if (_stage == Stage.Overflow)
            {
                if (ClickSortMenu(root))
                {
                    _stage = Stage.SortChoices;
                    _polls = 0;
                    SetStatus("Opened Sort by...; waiting for newest-first option.");
                }
            }
            else if (_stage == Stage.SortChoices)
            {
                bool alreadySelected = false;
                bool found = HasChoiceDialog(root) && Visit(root, node =>
                {
                    if (!IsNewestChoice(node))
                        return false;

                    alreadySelected = IsChecked(node);
                    return Click(node);
                });
                if (found)
                {
                    _stage = Stage.Confirm;
                    _polls = 0;
                    SetStatus(alreadySelected ? "Newest-first option is already selected." : "Tapped Modified (newest first).");
                }
            }
            else if (_stage == Stage.Confirm)
            {
                if (!HasChoiceDialog(root))
                {
                    Finish("Newest-first option selected; sort dialog closed.");
                    return;
                }

                if (Visit(root, node => node.ViewIdResourceName == "android:id/button1" && Click(node)))
                {
                    Finish("Newest-first option selected and confirmed.");
                    return;
                }

                if (_polls >= 3)
                {
                    Finish("Newest-first option selected. Close the sort dialog manually if it remains open.");
                    return;
                }
            }

            if (++_polls >= 12)
            {
                Finish("Stopped: expected sort controls were not found. Check the labels and close any open menu manually.");
                return;
            }
            Schedule();
        }
        catch (Java.Lang.IllegalStateException)
        {
            Finish("Files changed while sorting. Reopen the picker to try again.");
        }
        catch (Java.Lang.SecurityException)
        {
            Finish("Android denied access to the picker. Check the accessibility service permission.");
        }
    }

    private bool ClickSortMenu(AccessibilityNodeInfo root) => Visit(root, node =>
        node.ViewIdResourceName is "android:id/title" or "com.google.android.documentsui:id/title" or "com.android.documentsui:id/title" &&
        Exact(node.Text?.ToString(), _labels.SortMenu) &&
        IsInMenu(node) && Click(node));

    private static bool IsInMenu(AccessibilityNodeInfo node, int depth = 0)
    {
        if (node.ClassName?.ToString() == "android.widget.ListView")
            return true;
        if (depth >= 4)
            return false;
        using var parent = node.Parent;
        return parent is not null && IsInMenu(parent, depth + 1);
    }

    private bool IsOverflow(AccessibilityNodeInfo node) =>
        node.VisibleToUser && node.Enabled &&
        (node.ClassName?.ToString() is "android.widget.ImageButton" or "android.widget.ImageView") &&
        (Exact(node.ContentDescription?.ToString(), _labels.Overflow) ||
         (_labels == PickerSortLabels.English && Exact(node.ContentDescription?.ToString(), "More")));

    private bool HasChoiceDialog(AccessibilityNodeInfo root) => Visit(root, IsNewestChoice);

    private bool IsNewestChoice(AccessibilityNodeInfo node) =>
        Exact(node.Text?.ToString(), _labels.NewestFirst) &&
        (node.Checkable || node.ClassName?.ToString() is "android.widget.CheckedTextView" or "android.widget.RadioButton" ||
         node.ViewIdResourceName is "android:id/text1");

    private static bool Exact(string? actual, string expected) =>
        PickerSortLabels.Matches(actual, expected);

    private static bool IsChecked(AccessibilityNodeInfo node)
    {
        if (node.Checked || node.Selected)
            return true;
        using var parent = node.Parent;
        return parent is not null && (parent.Checked || parent.Selected);
    }

    private static bool Click(AccessibilityNodeInfo node, int depth = 0)
    {
        if (!node.VisibleToUser || !node.Enabled)
            return false;
        if (node.Clickable && node.PerformAction(global::Android.Views.Accessibility.Action.Click))
            return true;
        if (depth >= 3)
            return false;
        using var parent = node.Parent;
        return parent is not null && Click(parent, depth + 1);
    }

    private static bool Visit(AccessibilityNodeInfo root, Func<AccessibilityNodeInfo, bool> predicate, int depth = 0)
    {
        if (depth > 40)
            return false;
        if (root.VisibleToUser && predicate(root))
            return true;
        for (int i = 0; i < root.ChildCount; i++)
        {
            using var child = root.GetChild(i);
            if (child is not null && Visit(child, predicate, depth + 1))
                return true;
        }
        return false;
    }

    private void Finish(string message)
    {
        _sessionCompleted = true;
        _stage = Stage.Idle;
        _polls = 0;
        SetStatus(message);
    }

    private static void SetStatus(string message) => PickerSettings.LastStatus = message;

    private void Reset()
    {
        _handler.RemoveCallbacksAndMessages(null);
        _scheduled = false;
        _stage = Stage.Idle;
        _polls = 0;
        _sessionCompleted = false;
    }

    public override void OnInterrupt()
    {
        Reset();
        SetStatus("Accessibility interrupted; waiting for Files.");
    }

    public override void OnDestroy()
    {
        Connected = false;
        Reset();
        _handler.Dispose();
        base.OnDestroy();
    }
}
