using FlowRate.ViewModels;
using Microsoft.UI.Xaml;

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
        AppWindow.SetIcon("Assets/AppIcon.ico");
    }
}
