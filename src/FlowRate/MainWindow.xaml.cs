using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowRate.Core.Diagnostics;
using FlowRate.Core.Export;
using FlowRate.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.Storage.Pickers;

namespace FlowRate;

/// <summary>
/// The main application window for FlowRate.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Title = "FlowRate";

        ViewModel.SaveFilePickerAsync = PickExportPathAsync;
        ViewModel.CopyToClipboard = CopyTextToClipboard;

        ApplyWindowIcon();
        SizeAndCenter();
    }

    /// <summary>Copies text to the system clipboard.</summary>
    private static void CopyTextToClipboard(string text)
    {
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(text ?? string.Empty);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
    }

    /// <summary>
    /// Shows a native Save As dialog for exporting a benchmark result, returning the chosen
    /// path or <c>null</c> if the user cancelled. The picker is associated with this window's
    /// HWND, which is required for file pickers in a WinUI desktop app.
    /// </summary>
    private async Task<string?> PickExportPathAsync(string suggestedName, ExportFormat format)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedName),
        };

        if (format == ExportFormat.Json)
            picker.FileTypeChoices.Add("JSON file", new List<string> { ".json" });
        else
            picker.FileTypeChoices.Add("CSV file", new List<string> { ".csv" });

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }


    /// <summary>
    /// Applies the bundled application icon to the window (title bar and taskbar).
    /// Uses an absolute path resolved from the app base directory so it works
    /// under packaged launch; a relative path previously caused a startup crash.
    /// </summary>
    private void ApplyWindowIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }
            else
            {
                Logger.Warn($"App icon not found at {iconPath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to set window icon", ex);
        }
    }

    /// <summary>
    /// Gives the window a sensible default size and centers it on the primary
    /// work area so it never launches cramped or off to one side.
    /// </summary>
    private void SizeAndCenter()
    {
        const int width = 1040;
        const int height = 920;

        AppWindow.Resize(new SizeInt32(width, height));

        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        if (area is not null)
        {
            var x = area.WorkArea.X + (area.WorkArea.Width - width) / 2;
            var y = area.WorkArea.Y + (area.WorkArea.Height - height) / 2;
            AppWindow.Move(new PointInt32(x, y));
        }
    }

    /// <summary>
    /// Opens the Information dialog: iperf3 detection details (path + version),
    /// FlowRate version, and on-demand update checks for both against GitHub.
    /// </summary>
    private async void OnInfoClick(object sender, RoutedEventArgs e)
    {
        var iperf3Path = FlowRate.Core.Services.Iperf3Locator.FindExecutable();
        var iperf3Version = await FlowRate.Core.Services.Iperf3Locator.GetVersionAsync(iperf3Path);
        var appVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(0, 0, 0);

        var panel = new StackPanel { Spacing = 8, MinWidth = 420 };
        panel.Children.Add(new TextBlock { Text = "iperf3 Detection", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = $"Executable: {iperf3Path ?? "Not found"}", TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true });
        panel.Children.Add(new TextBlock { Text = $"Version: {iperf3Version ?? "Unknown"}", IsTextSelectionEnabled = true });
        panel.Children.Add(new TextBlock { Text = "FlowRate", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Version: {appVersion.ToString(3)}", IsTextSelectionEnabled = true });

        var updateStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
        var checkButton = new Button { Content = "Check for updates", Margin = new Thickness(0, 8, 0, 0) };
        checkButton.Click += async (_, _) =>
        {
            checkButton.IsEnabled = false;
            updateStatus.Text = "Checking GitHub for updates...";
            var updates = new FlowRate.Core.Services.UpdateService();
            var flowRate = await updates.CheckFlowRateAsync(appVersion);
            var iperf3 = await updates.CheckIperf3Async(iperf3Version);
            updateStatus.Text = $"FlowRate: {flowRate.Message}\niperf3: {iperf3.Message}";
            if (flowRate.IsUpdateAvailable && flowRate.ReleaseUrl is not null)
                _ = await Windows.System.Launcher.LaunchUriAsync(new Uri(flowRate.ReleaseUrl));
            if (iperf3.IsUpdateAvailable && iperf3.ReleaseUrl is not null)
                _ = await Windows.System.Launcher.LaunchUriAsync(new Uri(iperf3.ReleaseUrl));
            checkButton.IsEnabled = true;
        };
        panel.Children.Add(checkButton);
        panel.Children.Add(updateStatus);

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "Information",
            Content = panel,
            CloseButtonText = "Close",
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Opens the preferences dialog. Saving persists the current configuration as
    /// the defaults used for future sessions.
    /// </summary>
    private async void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "Preferences",
            Content = new SettingsDialogContent(ViewModel),
            PrimaryButtonText = "Save as defaults",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.SaveSettingsCommand.Execute(null);
        }
    }
}
