using FlowRate.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

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
        // AppWindow.SetIcon("Assets/AppIcon.ico"); // TODO: Fix asset deployment

        SizeAndCenter();
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
}
