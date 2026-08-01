using System.Text.Json;
using Launchpad.Core.Localization;
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

    /// <summary>Language-independent key for the recovery notice; null when no
    /// recovery happened. The UI translates it with the current language.</summary>
    public LanguageKey? LastRecoveryNoteKey { get; private set; }

    public IReadOnlyList<LaunchItem> ReadItems()
    {
        LastRecoveryNoteKey = null;
        var path = Path.Combine(_configDir, ItemsFileName);
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            return ParseOrThrow(path, json =>
                JsonSerializer.Deserialize<List<LaunchItem>>(json, LauncherJson.Options) ?? []);
        }
        catch (ConfigParseException corrupt)
        {
            return RecoverFromBackup(path, corrupt);
        }
    }

    /// <summary>
    /// A corrupt config.json is recovered from the backup by a file-level copy
    /// (deliberately NOT via WriteItems, which would overwrite the backup with
    /// the corrupt file). If the backup is also unreadable the original parse
    /// error is rethrown with backup details appended.
    /// </summary>
    private IReadOnlyList<LaunchItem> RecoverFromBackup(string path, ConfigParseException corrupt)
    {
        var backupPath = Path.Combine(_configDir, BackupFileName);
        if (!File.Exists(backupPath))
        {
            throw new ConfigParseException(path, $"{corrupt.Message}; no backup available (config.json.bak missing)", corrupt);
        }

        try
        {
            var recovered = ParseOrThrow(backupPath, json =>
                JsonSerializer.Deserialize<List<LaunchItem>>(json, LauncherJson.Options) ?? []);
            File.WriteAllText(path, File.ReadAllText(backupPath));
            LastRecoveryNoteKey = LanguageKey.StatusRecovered;
            return recovered;
        }
        catch (ConfigParseException backupCorrupt)
        {
            throw new ConfigParseException(path, $"{corrupt.Message}; backup also unreadable: {backupCorrupt.Message}", corrupt);
        }
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

        // ToList so the source-generated context (List metadata) is hit; the
        // IReadOnlyList interface type has no generated metadata.
        File.WriteAllText(path, JsonSerializer.Serialize(items.ToList(), LauncherJson.Options));
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
