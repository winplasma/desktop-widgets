// WinPlasma.Widgets — Views/WeatherWidget.cs
// Shows current weather from Open-Meteo API. Refreshes every 15 minutes.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinPlasma.Widgets.Models;
using WinPlasma.Widgets.Services;

namespace WinPlasma.Widgets.Views;

/// <summary>
/// A draggable desktop weather widget.
/// Fetches data from Open-Meteo via WeatherService.
/// </summary>
public sealed class WeatherWidget : BaseWidgetWindow
{
    private readonly WeatherService _weatherService;
    private readonly DispatcherTimer _timer;

    private TextBlock? _tempText;
    private TextBlock? _cityText;
    private TextBlock? _conditionText;
    private TextBlock? _iconText;

    public WeatherWidget(WidgetConfig config, WeatherService weatherService, Action<WidgetConfig> onPositionChanged)
        : base(config, onPositionChanged)
    {
        _weatherService = weatherService;

        Content = BuildUI();
        Configure(defaultWidth: 160, defaultHeight: 180);

        // Update weather every 15 minutes (Open-Meteo cache duration)
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(15) };
        _timer.Tick += (_, _) => _ = UpdateWeatherAsync();
        _timer.Start();

        _ = UpdateWeatherAsync();
    }

    private Grid BuildUI()
    {
        var root = new Grid();

        // Dark glassmorphism card
        var card = new Border
        {
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Background = new Microsoft.UI.Xaml.Media.AcrylicBrush
            {
                TintColor = Windows.UI.Color.FromArgb(200, 10, 20, 40),
                TintOpacity = 0.85
            },
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 255, 255, 255)),
            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel { Spacing = 8, HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center, VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center };

        _iconText = new TextBlock
        {
            FontSize = 48,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            Text = "⏳"
        };

        _tempText = new TextBlock
        {
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable"),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            Text = "--°"
        };

        _conditionText = new TextBlock
        {
            FontSize = 14,
            FontFamily = new FontFamily("Segoe UI Variable"),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 255, 255, 255)),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            Text = "Loading..."
        };

        _cityText = new TextBlock
        {
            FontSize = 12,
            FontFamily = new FontFamily("Segoe UI Variable"),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 255, 255, 255)),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            Text = Config.WeatherLocation == "auto" ? "Detecting location..." : Config.WeatherLocation,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };

        stack.Children.Add(_iconText);
        stack.Children.Add(_tempText);
        stack.Children.Add(_conditionText);
        stack.Children.Add(_cityText);

        card.Child = stack;
        root.Children.Add(card);

        // Drag support
        card.PointerPressed += (s, e) => OnDragStart(e);
        card.PointerMoved  += (s, e) => OnDragMove(e);
        card.PointerReleased += (s, e) => OnDragEnd(e);

        return root;
    }

    private async Task UpdateWeatherAsync()
    {
        var data = await _weatherService.GetWeatherAsync(Config.WeatherLocation, Config.WeatherUnit);

        DispatcherQueue.TryEnqueue(() =>
        {
            if (data is null)
            {
                if (_conditionText != null) _conditionText.Text = "Error";
                return;
            }

            if (_tempText != null) _tempText.Text = $"{data.Temperature:F0}{data.Unit}";
            if (_conditionText != null) _conditionText.Text = data.Condition;
            if (_cityText != null) _cityText.Text = data.City;
            if (_iconText != null) _iconText.Text = data.Icon;
        });
    }

    public new void Close()
    {
        _timer.Stop();
        base.Close();
    }
}
