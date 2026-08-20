// WinPlasma.Widgets — Services/WeatherService.cs
// Fetches weather data from Open-Meteo (free, no API key required).
// Caches results for 15 minutes to stay within performance budget.

using System.Net.Http;
using System.Text.Json.Nodes;

namespace WinPlasma.Widgets.Services;

/// <summary>
/// Fetches current weather from Open-Meteo API.
/// No API key needed. Data is cached for 15 minutes.
/// Location: city name → geocoding → lat/lon → weather.
/// </summary>
public sealed class WeatherService : IDisposable
{
    private readonly HttpClient _http;
    private WeatherData? _cache;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private string _lastLocation = string.Empty;

    // Open-Meteo endpoints (completely free, no auth)
    private const string GeoUrl   = "https://geocoding-api.open-meteo.com/v1/search?name={0}&count=1&language=en&format=json";
    private const string WeatherUrl = "https://api.open-meteo.com/v1/forecast?" +
        "latitude={0}&longitude={1}" +
        "&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m,is_day" +
        "&wind_speed_unit=kmh&temperature_unit={2}&timezone=auto";
    private const string IpGeoUrl = "https://ipapi.co/json/";

    public WeatherService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.Add("User-Agent", "WinPlasma/1.0");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Get current weather. Returns cached data if fresh.
    /// </summary>
    public async Task<WeatherData?> GetWeatherAsync(string location, string unit = "celsius")
    {
        // Return cache if still valid and same location
        if (_cache is not null && DateTime.Now < _cacheExpiry && _lastLocation == location)
            return _cache;

        try
        {
            double lat, lon;
            string cityName = location;

            if (location == "auto")
            {
                (lat, lon, cityName) = await GetLocationFromIpAsync();
            }
            else
            {
                (lat, lon) = await GeoCodeAsync(location);
                cityName = location;
            }

            var data = await FetchWeatherAsync(lat, lon, unit, cityName);
            _cache = data;
            _cacheExpiry = DateTime.Now.AddMinutes(15);
            _lastLocation = location;
            return data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeatherService] Error: {ex.Message}");
            return _cache; // Return stale cache on error
        }
    }

    // ── Geocoding ─────────────────────────────────────────────────────────────

    private async Task<(double lat, double lon)> GeoCodeAsync(string city)
    {
        var url = string.Format(GeoUrl, Uri.EscapeDataString(city));
        var json = JsonNode.Parse(await _http.GetStringAsync(url));
        var first = json?["results"]?[0] ?? throw new Exception($"City not found: {city}");
        return (first["latitude"]!.GetValue<double>(), first["longitude"]!.GetValue<double>());
    }

    private async Task<(double lat, double lon, string city)> GetLocationFromIpAsync()
    {
        var json = JsonNode.Parse(await _http.GetStringAsync(IpGeoUrl));
        var lat  = json?["latitude"]?.GetValue<double>() ?? 0;
        var lon  = json?["longitude"]?.GetValue<double>() ?? 0;
        var city = json?["city"]?.ToString() ?? "Unknown";
        return (lat, lon, city);
    }

    // ── Weather fetch ─────────────────────────────────────────────────────────

    private async Task<WeatherData> FetchWeatherAsync(double lat, double lon,
        string unit, string cityName)
    {
        var url = string.Format(WeatherUrl, lat, lon, unit);
        var json = JsonNode.Parse(await _http.GetStringAsync(url));
        var current = json?["current"] ?? throw new Exception("Invalid weather response");

        var code = current["weather_code"]?.GetValue<int>() ?? 0;
        var isDay = current["is_day"]?.GetValue<int>() ?? 1;

        return new WeatherData
        {
            City = cityName,
            Temperature = current["temperature_2m"]?.GetValue<double>() ?? 0,
            Unit = unit == "celsius" ? "°C" : "°F",
            Humidity = current["relative_humidity_2m"]?.GetValue<int>() ?? 0,
            WindSpeed = current["wind_speed_10m"]?.GetValue<double>() ?? 0,
            Condition = WmoCodeToCondition(code),
            Icon = WmoCodeToIcon(code, isDay == 1),
            IsDay = isDay == 1
        };
    }

    // ── WMO weather code lookup ───────────────────────────────────────────────
    // Reference: https://open-meteo.com/en/docs#weathervariables

    private static string WmoCodeToCondition(int code) => code switch
    {
        0          => "Clear sky",
        1 or 2     => "Mainly clear",
        3          => "Overcast",
        45 or 48   => "Foggy",
        51 or 53   => "Drizzle",
        61 or 63   => "Rain",
        65         => "Heavy rain",
        71 or 73   => "Snow",
        80 or 81   => "Rain showers",
        95         => "Thunderstorm",
        _          => "Unknown"
    };

    private static string WmoCodeToIcon(int code, bool isDay) => code switch
    {
        0          => isDay ? "☀️" : "🌙",
        1 or 2     => isDay ? "⛅" : "🌤️",
        3          => "☁️",
        45 or 48   => "🌫️",
        51 or 53 or 61 or 63 => "🌧️",
        65         => "🌧️",
        71 or 73   => "🌨️",
        80 or 81   => "🌦️",
        95         => "⛈️",
        _          => "🌡️"
    };

    public void Dispose() => _http.Dispose();
}

/// <summary>Current weather data from Open-Meteo.</summary>
public sealed class WeatherData
{
    public string City { get; init; } = string.Empty;
    public double Temperature { get; init; }
    public string Unit { get; init; } = "°C";
    public int Humidity { get; init; }
    public double WindSpeed { get; init; }
    public string Condition { get; init; } = string.Empty;
    public string Icon { get; init; } = "🌡️";
    public bool IsDay { get; init; } = true;
}
