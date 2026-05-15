using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Gr8scale;

public sealed class AppProfile
{
    public string ExeName { get; set; } = "";       // case-insensitive match key, e.g. "notepad.exe"
    public string ExePath { get; set; } = "";       // best-known full path (used for icon resolution; matching is by ExeName)
    public string DisplayName { get; set; } = "";   // shown in UI
    public double Intensity { get; set; }           // 0..100

    [JsonIgnore] public ImageSource? Icon { get; set; }
}

public sealed class Settings
{
    public double Intensity { get; set; } = 0.0;                              // default / fallback
    public List<AppProfile> AppProfiles { get; set; } = new();
}

internal static class SettingsService
{
    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "gr8scale");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<Settings>(json, Options);
                if (s != null)
                {
                    s.AppProfiles ??= new List<AppProfile>();
                    return s;
                }
            }
        }
        catch { }
        return new Settings();
    }

    public static void Save(Settings s)
    {
        try
        {
            var json = JsonSerializer.Serialize(s, Options);
            File.WriteAllText(FilePath, json);
        }
        catch { }
    }
}
