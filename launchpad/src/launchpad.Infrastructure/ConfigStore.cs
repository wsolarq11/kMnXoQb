using System.Text.Json;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.Core.Serialization;

namespace Launchpad.Infrastructure;

/// <summary>
/// File-backed config store. Items are backed up to config.json.bak before every
/// write (matching legacy behavior); parse failures raise <see cref="ConfigParseException"/>.
/// The config directory is created on construction so first-run writes never fail
/// when the directory does not exist (e.g. published layouts without a config/ ancestor).
/// </summary>
public sealed class ConfigStore : IConfigStore
{
    private readonly string _configDir;

    public ConfigStore(string configDir)
    {
        Directory.CreateDirectory(configDir);
        _configDir = configDir;
    }

    private const string ItemsFileName = "config.json";
    private const string SettingsFileName = "settings.json";
    private const string BackupFileName = "config.json.bak";

    public IReadOnlyList<LaunchItem> ReadItems()
    {
        var path = Path.Combine(_configDir, ItemsFileName);
        if (!File.Exists(path))
        {
            return [];
        }

        return ParseOrThrow(path, json =>
            JsonSerializer.Deserialize<List<LaunchItem>>(json, LauncherJson.Options) ?? []);
    }

    public AppSettings ReadSettings()
    {
        var path = Path.Combine(_configDir, SettingsFileName);
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        return ParseOrThrow(path, json =>
            JsonSerializer.Deserialize<AppSettings>(json, LauncherJson.Options) ?? new AppSettings());
    }

    public void WriteItems(IReadOnlyList<LaunchItem> items)
    {
        var path = Path.Combine(_configDir, ItemsFileName);
        if (File.Exists(path))
        {
            File.Copy(path, Path.Combine(_configDir, BackupFileName), overwrite: true);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(items, LauncherJson.Options));
    }

    public void WriteSettings(AppSettings settings)
    {
        var path = Path.Combine(_configDir, SettingsFileName);
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
