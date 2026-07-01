using System.Text.Json;
using ChatRoleplay.Config;
using Microsoft.Extensions.Logging;

namespace ChatRoleplay.Services;

/// <summary>
/// Manages loading and saving of JSON configuration files from the data directory.
/// </summary>
public class ConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly string _dataDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public CoreConfig CoreConfig { get; private set; } = new();
    public CharacterConfig CharacterConfig { get; private set; } = new();
    public PromptConfig PromptConfig { get; private set; } = new();

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;
        _dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
    }

    public async Task LoadAllAsync()
    {
        Directory.CreateDirectory(_dataDirectory);

        CoreConfig = await LoadOrCreateAsync("core-config.json", new CoreConfig());
        CharacterConfig = await LoadOrCreateAsync("character-config.json", new CharacterConfig());
        PromptConfig = await LoadOrCreateAsync("prompt-config.json", new PromptConfig());

        _logger.LogInformation("All configuration files loaded from {DataDir}", _dataDirectory);
    }

    private async Task<T> LoadOrCreateAsync<T>(string fileName, T defaultValue) where T : class
    {
        var filePath = Path.Combine(_dataDirectory, fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("Config file {File} not found – creating with defaults", fileName);
            await SaveAsync(fileName, defaultValue);
            return defaultValue;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var loaded = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (loaded != null)
            {
                _logger.LogInformation("Loaded config: {File}", fileName);
                // Re-save to pick up any new fields added since last run
                await SaveAsync(fileName, loaded);
                return loaded;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize {File}. Using defaults.", fileName);
        }

        return defaultValue;
    }

    public async Task SaveAsync<T>(string fileName, T value)
    {
        Directory.CreateDirectory(_dataDirectory);
        var filePath = Path.Combine(_dataDirectory, fileName);
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }
}
