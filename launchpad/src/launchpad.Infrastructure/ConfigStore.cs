using System.Text.Json;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.Core.Serialization;

namespace Launchpad.Infrastructure;

/// <summary>
/// File-backed config store. Items are backed up to config.json.bak before every
/// write (matching legacy behavior); parse failures raise <see cref="ConfigParseException"/>.
/// </summary>
public sealed class ConfigStore(string configDir) : IConfigStore
{
    private const string ItemsFileName = "config.json";
    private const string SettingsFileName = "settings.json";
    private const string BackupFileName = "config.json.bak";

    public IReadOnlyList<LaunchItem> ReadItems()
    {
        var path = Path.Combine(configDir, ItemsFileName);
        if (!File.Exists(path))
        {
            return [];
        }

        return ParseOrThrow(path, json =>
            JsonSerializer.Deserialize<List<LaunchItem>>(json, LauncherJson.Options) ?? []);
    }

    public AppSettings ReadSettings()
    {
        var path = Path.Combine(configDir, SettingsFileName);
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        return ParseOrThrow(path, json =>
            JsonSerializer.Deserialize<AppSettings>(json, LauncherJson.Options) ?? new AppSettings());
    }

    public void WriteItems(IReadOnlyList<LaunchItem> items)
    {
        var path = Path.Combine(configDir, ItemsFileName);
        if (File.Exists(path))
        {
            File.Copy(path, Path.Combine(configDir, BackupFileName), overwrite: true);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(items, LauncherJson.Options));
    }

    public void WriteSettings(AppSettings settings)
    {
        var path = Path.Combine(configDir, SettingsFileName);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, LauncherJson.Options));
    }

    private static T ParseOrThrow<T>(string path, Func<string, T> parse)
    {
        try
        {
            return parse(File.ReadAllText(path));
        }
        catch (JsonException e)
        {
            throw new ConfigParseException(path, e.Message, e);
        }
    }
}
