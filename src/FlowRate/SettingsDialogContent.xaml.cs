using FlowRate.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FlowRate;

/// <summary>
/// Content shown inside the Preferences dialog. Binds to the shared
/// <see cref="MainViewModel"/> so edits flow straight back to the main window.
/// </summary>
public sealed partial class SettingsDialogContent : UserControl
{
    public MainViewModel ViewModel { get; }

    public SettingsDialogContent(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
