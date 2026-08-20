// WinPlasma.Widgets — WidgetsPlugin.cs
// IPlugin entry point. Spawns and manages all configured widget windows.

using System.Text.Json;
using System.Text.Json.Nodes;
using WinPlasma.SDK;
using WinPlasma.SDK.Models;
using WinPlasma.Widgets.Models;
using WinPlasma.Widgets.Services;
using WinPlasma.Widgets.Views;

namespace WinPlasma.Widgets;

/// <summary>
/// Win Plasma Widgets plugin.
/// Reads widget configurations and spawns transparent draggable windows.
/// </summary>
public sealed class WidgetsPlugin : IPlugin
{
    public string Id          => "com.winplasma.widgets";
    public string Name        => "Desktop Widgets";
    public string Version     => "1.0.0";
    public string Author      => "WinPlasma";
    public string Description => "Draggable desktop widgets: Clock, Weather, etc.";

    private WinPlasmaContext? _context;
    private readonly List<BaseWidgetWindow> _windows = [];
    private WeatherService? _weatherService;

    // By default, just one clock if none specified
    private List<WidgetConfig> _widgetConfigs = [new WidgetConfig { Type = WidgetType.Clock }];

    public Task InitializeAsync(WinPlasmaContext context)
    {
        _context = context;
        _context.Logger.LogInfo("Widgets: Initialized.");
        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        _context?.Logger.LogInfo("Widgets: Starting...");

        _weatherService = new WeatherService();

        var settings = await _context!.ConfigService.GetSettingsAsync();
        ApplySettingsInternal(settings);

        SpawnWidgets();

        _context.Logger.LogInfo($"Widgets: Started {_windows.Count} widgets.");
    }

    public Task StopAsync()
    {
        _context?.Logger.LogInfo("Widgets: Stopping...");

        foreach (var window in _windows)
        {
            try { window.Close(); } catch { }
        }
        _windows.Clear();

        _weatherService?.Dispose();

        _context?.Logger.LogInfo("Widgets: Stopped.");
        return Task.CompletedTask;
    }

    public Task<PluginSettingsSchema> GetSettingsSchemaAsync()
    {
        // For Phase 2, we just return a basic schema.
        // Full widget management UI would involve a custom control or JSON array editing.
        var schema = new PluginSettingsSchema
        {
            Fields =
            [
                new() { Key = "resetPositions", Label = "Reset all widget positions", FieldType = SettingsFieldType.Bool, DefaultValue = JsonValue.Create(false) }
            ]
        };
        return Task.FromResult(schema);
    }

    public Task ApplySettingsAsync(JsonObject settings)
    {
        ApplySettingsInternal(settings);
        
        // If they requested a reset, reset coordinates and save
        if (settings["resetPositions"]?.GetValue<bool>() == true)
        {
            foreach (var cfg in _widgetConfigs)
            {
                cfg.X = 100;
                cfg.Y = 100;
            }
            settings["resetPositions"] = false;
            SaveConfigsAsync(); // Fire and forget
            
            // Respawn
            SpawnWidgets();
        }

        return Task.CompletedTask;
    }

    private void ApplySettingsInternal(JsonObject settings)
    {
        if (settings.TryGetPropertyValue("widgets", out var widgetsNode) && widgetsNode is JsonArray arr)
        {
            try
            {
                _widgetConfigs = JsonSerializer.Deserialize<List<WidgetConfig>>(arr.ToJsonString()) 
                                 ?? [new WidgetConfig { Type = WidgetType.Clock }];
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError("Failed to parse widgets array.", ex);
            }
        }
    }

    private void SpawnWidgets()
    {
        foreach (var window in _windows)
        {
            try { window.Close(); } catch { }
        }
        _windows.Clear();

        foreach (var config in _widgetConfigs)
        {
            BaseWidgetWindow? window = config.Type switch
            {
                WidgetType.Clock => new ClockWidget(config, OnWidgetPositionChanged),
                WidgetType.Weather => new WeatherWidget(config, _weatherService!, OnWidgetPositionChanged),
                _ => null
            };

            if (window is not null)
            {
                window.Activate();
                _windows.Add(window);
            }
        }
    }

    private void OnWidgetPositionChanged(WidgetConfig updatedConfig)
    {
        // Update the item in our list and save to config
        var index = _widgetConfigs.FindIndex(c => c.InstanceId == updatedConfig.InstanceId);
        if (index >= 0)
        {
            _widgetConfigs[index] = updatedConfig;
            _ = SaveConfigsAsync(); // Fire and forget
        }
    }

    private async Task SaveConfigsAsync()
    {
        if (_context is null) return;
        var settings = await _context.ConfigService.GetSettingsAsync();
        settings["widgets"] = JsonNode.Parse(JsonSerializer.Serialize(_widgetConfigs));
        await _context.ConfigService.SaveSettingsAsync(settings);
    }
}
