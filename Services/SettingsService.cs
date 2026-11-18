using PUBGFlagChanger.Models;
using System.IO;
using System.Text.Json;

namespace PUBGFlagChanger.Services;

public class SettingsService
{
    private readonly string _settingsFile;

    public SettingsService()
    {
        string packageRoot = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Packages");

        Directory.CreateDirectory(packageRoot);

        _settingsFile = Path.Combine(
            packageRoot,
            "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsFile))
            return new AppSettings();

        string json =
            File.ReadAllText(_settingsFile);

        return JsonSerializer.Deserialize<AppSettings>(json)
               ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        string json =
            JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(_settingsFile, json);
    }
}