# Files Newest

An Android-only .NET 10 MAUI app based on VolumeRestore's Android 36 build properties, MAUI 10.0.20 setup, and dark settings-page design. VolumeRestore and AndroidApp1 are not modified; FilesNewest is a separate project in AndroidApp1.slnx with its own package, com.example.filesnewest.

## Build and install

In Visual Studio 2026, select FilesNewest as the startup project and deploy to a Pixel / stock Android device. Requires the .NET 10 MAUI Android workload and Android 36 SDK; minimum Android version is 7.0 (API 24), matching the template. Debug and Release builds produce an APK. No VolumeRestore signing keys, volume services, or volume permissions are reused.

Alternatively build FilesNewest.csproj with `dotnet build -c Debug`. Find APKs under bin/Debug/net10.0-android36.0.

## Publish a signed ARM64 release

Uses the same folder-publish workflow as VolumeRestore, with a separate Files Newest release key. In Visual Studio, select **Release**, right-click **FilesNewest** → **Publish**, then select **FolderProfile**.

From the directory containing FilesNewest.csproj, run `dotnet publish FilesNewest.csproj -c Release -p:PublishProfile=FolderProfile`.

The installable APK is `bin/Release/net10.0-android36.0/publish/android-arm64/com.example.filesnewest-Signed.apk`. This ARM64 build is intended for physical ARM64 devices such as the Pixel 8a, not the x64 emulator.

Release signing imports the local `release-signing/FilesNewest.signing.props`. The `release-signing/` directory contains the keystore and password files, is excluded from Git, and must never be shared publicly or committed. Back up the entire directory securely; future app updates require the same signing key. Publishing with FolderProfile fails if signing is missing rather than using a debug key, and verifies the APK signature before publishing.

A release signed with this key cannot update an existing debug-signed installation. If Android reports a signature conflict, export anything needed before manually uninstalling the debug build; uninstalling clears preferences and requires accessibility consent again. Publishing does not uninstall or install the app automatically.

## Enable

1. Open **Files Newest**, turn on **Picker monitoring**, and read/accept the accessibility explanation.
2. Tap **Open Accessibility Settings**. Enable **Files Newest — automatic sorting** (usually under downloaded apps).
3. If Android blocks the setting for a sideloaded APK, open Settings → Apps → Files Newest → three-dot menu → **Allow restricted settings**, then return to Accessibility. Only do this for a build you trust.
4. Return to the app and tap **Open Test File Picker**. No file needs to be selected; Back closes the test.
5. The service taps **More options → Sort by... → Modified (newest first)**, and **OK/Apply** if the recognized dialog requires it.
6. To use another sort order manually, pause monitoring first. To revoke access completely, turn the service off in Android Accessibility settings.

Android manages the enabled accessibility service; no foreground service, notification, boot receiver, or battery-exemption request is needed. Manufacturer restrictions or force-stop may prevent reconnection; check the connection status after reboot.

## Scope and privacy

- Supports stock DocumentsUI packages `com.google.android.documentsui` and `com.android.documentsui`, not Files by Google’s standalone browser, third-party managers, or custom app pickers.
- Uses the installed picker's own translated string resources for the device language, including either three dots or an ellipsis in **Sort by...**. Shared menu-label IDs are combined with an exact localized label and menu structure; recognized sort dialogs use the positive-button ID for confirmation. Does not use screen coordinates or select arbitrary files whose names match sort labels.
- Receives window-change events to cancel when another app takes focus. Only Files roots are traversed. Android may display a broader standard accessibility warning because the platform grants window-content access.
- No Internet or storage permissions, analytics, file-content access, file modification, or file selection. Preferences contain only monitoring on/off and a generic last-action message. Picker-session state remains in memory only; folder paths are not tracked.
- This is visible UI automation, not a modification of Android’s default sorting. Menus may briefly open. Layout changes, list loading, locked/profile-isolated pickers, or missing controls can prevent automation.
- Checks once per picker opening, not per folder or storage-provider change. Completed or stopped attempts are not repeated while navigating within the picker. Leaving Files resets the session so the next picker opening can sort again. Temporary missing accessibility roots do not reset the session.
- Attempts are bounded. Unknown dialogs are not dismissed automatically. If a menu is left open, close it manually and reopen the picker. Turning monitoring off stops queued actions; turning off the accessibility service revokes access.

## Picker languages

The service reads `menu_sort`, `sort_dimension_date_descending`, and `abc_action_menu_overflow_description` from the supported DocumentsUI package. Android selects the translations for the current device configuration, so there is no fixed language list or online translation dependency. All translations supplied by that installed picker can be used; languages it does not translate follow Android's normal resource fallback. The Files Newest settings page and status messages remain English.

Labels are resolved again when starting a new sorting attempt. Missing or inaccessible resources fall back to the original English labels. Matching remains exact, apart from surrounding whitespace and equivalent ellipsis punctuation. The service does not guess menu positions or sorting direction. Customized pickers that rename resources or use a different per-app language from the device may not match; unrecognized controls are left untouched.

Android package visibility is limited to the two supported DocumentsUI packages through manifest queries. Reading their installed string resources adds no permission request and does not read documents or transmit data.

## Device acceptance checks

A successful build cannot verify another app’s accessibility tree. Before relying on the app, perform these checks on the target Pixel:

- Set the picker to **File name (A to Z)**. Open it through the test button; confirm **Modified (newest first)** becomes selected and the resulting order is correct.
- Test a picker opened from another app and navigate between folders and storage providers; the sort menu must not reopen during navigation.
- Close the picker, then open a new picker request; sorting should run once again.
- Start with newest-first already selected: no repeated menu opening should occur.
- Repeat with another device language supported by the installed picker; confirm the localized newest-first option is selected. Include a right-to-left language where available. Restore your preferred language afterward. A build or resource-table inspection alone does not validate every translated UI.
- Leave the picker while automation starts: no clicks should occur in the next app.
- Turn monitoring off: sort controls must remain untouched. Turn the accessibility service off: status should show it is not connected.
- Try a custom picker: it must remain untouched.
- Leave Files open for a minute: it must not repeatedly open the menu.
- Reboot and verify accessibility reconnects; Android controls this lifecycle.

If a control is not recognized, provide the Android version and a screenshot of the overflow menu and sort dialog. For local diagnostics, `adb shell uiautomator dump /sdcard/window.xml` followed by `adb pull /sdcard/window.xml` captures UI labels/resource IDs. The dump can include private filenames; redact them before sharing. Never grant accessibility access or other elevated permissions via ADB just to bypass user consent.
