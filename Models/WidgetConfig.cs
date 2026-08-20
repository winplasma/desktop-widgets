// WinPlasma.Widgets — Models/WidgetConfig.cs
// Data model for a single widget instance stored in config.json

using System.Text.Json.Serialization;

namespace WinPlasma.Widgets.Models;

/// <summary>Identifies which type of widget to render.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WidgetType
{
    Clock,
    Weather,
    SystemStats,
    Calendar,
    StickyNote
}

/// <summary>
/// Persisted configuration for a single widget instance.
/// Multiple instances of the same WidgetType can coexist.
/// </summary>
public sealed class WidgetConfig
{
    /// <summary>Unique ID for this widget instance. Generated as a short GUID.</summary>
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    public WidgetType Type { get; set; } = WidgetType.Clock;

    /// <summary>Monitor device name this widget belongs to. E.g. "\\.\DISPLAY1"</summary>
    public string MonitorId { get; set; } = string.Empty;

    /// <summary>X position within the monitor's work area.</summary>
    public int X { get; set; } = 100;

    /// <summary>Y position within the monitor's work area.</summary>
    public int Y { get; set; } = 100;

    /// <summary>Width in pixels. 0 = use default for this widget type.</summary>
    public int Width { get; set; } = 0;

    /// <summary>Height in pixels. 0 = use default.</summary>
    public int Height { get; set; } = 0;

    // ── Widget-type specific settings ──────────────────────────────────────

    /// <summary>For Weather: city name or "auto" to detect from IP.</summary>
    public string WeatherLocation { get; set; } = "auto";

    /// <summary>For Weather: "celsius" or "fahrenheit".</summary>
    public string WeatherUnit { get; set; } = "celsius";

    /// <summary>For StickyNote: the note text.</summary>
    public string NoteText { get; set; } = string.Empty;

    /// <summary>For StickyNote: hex background color.</summary>
    public string NoteColor { get; set; } = "#FFF176";

    /// <summary>For Clock: show seconds hand.</summary>
    public bool ClockShowSeconds { get; set; } = true;

    /// <summary>For Clock: "analog" or "digital".</summary>
    public string ClockStyle { get; set; } = "digital";
}
