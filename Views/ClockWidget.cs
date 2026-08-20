// WinPlasma.Widgets — Views/ClockWidget.cs
// Digital/analog clock widget. Updates every second using DispatcherTimer.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinPlasma.Widgets.Models;

namespace WinPlasma.Widgets.Views;

/// <summary>
/// A draggable desktop clock widget.
/// Shows time and date with a glassmorphism dark card style.
/// </summary>
public sealed class ClockWidget : BaseWidgetWindow
{
    private readonly DispatcherTimer _timer;
    private TextBlock? _timeText;
    private TextBlock? _dateText;
    private TextBlock? _secondsText;

    public ClockWidget(WidgetConfig config, Action<WidgetConfig> onPositionChanged)
        : base(config, onPositionChanged)
    {
        Content = BuildUI();
        Configure(defaultWidth: 200, defaultHeight: 100);

        // Update clock every second
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateTime();
        _timer.Start();
        UpdateTime();
    }

    private Grid BuildUI()
    {
        var root = new Grid();

        // Dark glassmorphism card
        var card = new Border
        {
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(20, 14, 20, 14),
            Background = new Microsoft.UI.Xaml.Media.AcrylicBrush
            {
                TintColor = Windows.UI.Color.FromArgb(200, 8, 8, 20),
                TintOpacity = 0.85
            },
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 255, 255, 255)),
            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel { Spacing = 2, HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center };

        // Time row
        var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center };

        _timeText = new TextBlock
        {
            FontSize = 32,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontFamily = new FontFamily("Segoe UI Variable"),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
        };

        _secondsText = new TextBlock
        {
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 6),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 255, 255, 255)),
            FontFamily = new FontFamily("Segoe UI Variable"),
            Visibility = Config.ClockShowSeconds ? Visibility.Visible : Visibility.Collapsed
        };

        timeRow.Children.Add(_timeText);
        timeRow.Children.Add(_secondsText);

        _dateText = new TextBlock
        {
            FontSize = 12,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            FontFamily = new FontFamily("Segoe UI Variable"),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 255, 255, 255))
        };

        stack.Children.Add(timeRow);
        stack.Children.Add(_dateText);

        card.Child = stack;
        root.Children.Add(card);

        // Drag support
        card.PointerPressed += (s, e) => OnDragStart(e);
        card.PointerMoved  += (s, e) => OnDragMove(e);
        card.PointerReleased += (s, e) => OnDragEnd(e);

        return root;
    }

    private void UpdateTime()
    {
        var now = DateTime.Now;
        if (_timeText is not null)
            _timeText.Text = now.ToString("h:mm");
        if (_secondsText is not null)
            _secondsText.Text = now.ToString(":ss");
        if (_dateText is not null)
            _dateText.Text = now.ToString("dddd, MMMM d");
    }

    public new void Close()
    {
        _timer.Stop();
        base.Close();
    }
}
