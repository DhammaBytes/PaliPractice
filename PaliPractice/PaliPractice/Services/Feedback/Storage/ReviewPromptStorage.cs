using Windows.Storage;

namespace PaliPractice.Services.Feedback.Storage;

/// <summary>
/// Persists review prompt state using ApplicationData.LocalSettings.
/// </summary>
public sealed class ReviewPromptStorage
{
    const string LastPromptTimestampKey = "ReviewPrompt_LastTimestamp";
    const string PromptCountKey = "ReviewPrompt_Count";
    const string UserOpenedStoreKey = "ReviewPrompt_UserOpenedStore";

    readonly ApplicationDataContainer _settings;

    public ReviewPromptStorage()
    {
        _settings = ApplicationData.Current.LocalSettings;
    }

    /// <summary>
    /// Gets the Unix timestamp of the last review prompt, or 0 if never prompted.
    /// </summary>
    public long LastPromptTimestamp
    {
        get => GetLong(LastPromptTimestampKey, 0);
        set => _settings.Values[LastPromptTimestampKey] = value;
    }

    /// <summary>
    /// Gets the total number of review prompts shown.
    /// </summary>
    public int PromptCount
    {
        get => GetInt(PromptCountKey, 0);
        set => _settings.Values[PromptCountKey] = value;
    }

    /// <summary>
    /// Gets or sets whether the user has manually opened the store page.
    /// </summary>
    public bool HasUserOpenedStore
    {
        get => GetBool(UserOpenedStoreKey, false);
        set => _settings.Values[UserOpenedStoreKey] = value;
    }

    /// <summary>
    /// Records a review prompt, updating timestamp and incrementing count.
    /// </summary>
    public void RecordPrompt()
    {
        LastPromptTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PromptCount++;
    }

    /// <summary>
    /// Gets the number of days since the last review prompt, or int.MaxValue if never prompted.
    /// </summary>
    public int DaysSinceLastPrompt
    {
        get
        {
            var lastTimestamp = LastPromptTimestamp;
            if (lastTimestamp == 0)
                return int.MaxValue;

            var lastPrompt = DateTimeOffset.FromUnixTimeSeconds(lastTimestamp);
            return (int)(DateTimeOffset.UtcNow - lastPrompt).TotalDays;
        }
    }

    long GetLong(string key, long defaultValue)
    {
        if (_settings.Values.TryGetValue(key, out var value))
        {
            return value switch
            {
                long l => l,
                int i => i,
                _ => defaultValue
            };
        }
        return defaultValue;
    }

    int GetInt(string key, int defaultValue)
    {
        if (_settings.Values.TryGetValue(key, out var value))
        {
            return value switch
            {
                int i => i,
                long l => (int)l,
                _ => defaultValue
            };
        }
        return defaultValue;
    }

    bool GetBool(string key, bool defaultValue)
    {
        if (_settings.Values.TryGetValue(key, out var value))
        {
            return value switch
            {
                bool b => b,
                _ => defaultValue
            };
        }
        return defaultValue;
    }
}
