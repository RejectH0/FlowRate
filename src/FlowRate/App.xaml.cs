using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using FlowRate.Core.Diagnostics;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FlowRate;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        // Capture otherwise-invisible crashes so we have a real diagnostic trail.
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Definite process-lifetime markers so a normal user close is never mistaken
        // for a crash. ProcessExit fires when the runtime shuts down cleanly.
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        Logger.Info($"===== FlowRate process starting (PID {Environment.ProcessId}). Log directory: {Logger.LogDirectory} =====");
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Closed += OnMainWindowClosed;
            _window.Activate();
            Logger.Info("FlowRate launched successfully; main window activated and visible.");

            // First-run gate: verify iperf3 is available; if not, inform the user
            // where to get it and exit. Runs after activation so the dialog has a XamlRoot.
            _ = VerifyIperf3InstalledAsync();
        }
        catch (Exception ex)
        {
            Logger.Error("Startup failed while creating or activating the main window.", ex);
            throw;
        }
    }

    /// <summary>
    /// Checks that iperf3 is installed (app directory or PATH). If missing, shows a
    /// blocking dialog directing the user to the official Windows builds and exits.
    /// </summary>
    private async System.Threading.Tasks.Task VerifyIperf3InstalledAsync()
    {
        var exePath = FlowRate.Core.Services.Iperf3Locator.FindExecutable();
        if (exePath is not null)
        {
            Logger.Info($"iperf3 detected at: {exePath}");
            return;
        }

        Logger.Error("iperf3 was not found in the application directory or on PATH.");

        var dialog = new ContentDialog
        {
            XamlRoot = _window!.Content.XamlRoot,
            Title = "iperf3 Not Found",
            Content = "FlowRate requires iperf3, which was not found in the application folder or on your system PATH.\n\n" +
                      "Please download and install the iperf3 Windows package, then restart FlowRate:\n" +
                      FlowRate.Core.Services.Iperf3Locator.WindowsBuildsUrl,
            PrimaryButtonText = "Open Download Page",
            CloseButtonText = "Exit",
            DefaultButton = ContentDialogButton.Primary,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _ = await Windows.System.Launcher.LaunchUriAsync(new Uri(FlowRate.Core.Services.Iperf3Locator.WindowsBuildsUrl));
        }

        Logger.Info("Exiting: iperf3 is not installed.");
        _window.Close();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        Logger.Info("Main window closed by user. Beginning normal shutdown.");
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        Logger.Info($"===== FlowRate process terminated normally (PID {Environment.ProcessId}) =====");
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Logger.Error($"Unhandled UI exception: {e.Message}", e.Exception);
    }

    private void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        Logger.Error("Unhandled AppDomain exception", e.ExceptionObject as Exception);
    }

    private void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error("Unobserved task exception", e.Exception);
        e.SetObserved();
    }
}
