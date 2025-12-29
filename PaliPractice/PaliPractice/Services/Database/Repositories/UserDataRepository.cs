using System.Globalization;
using System.Text.Json;
using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Repository for user data access (mastery, difficulty, settings, history).
/// Uses type-specific tables for nouns and verbs.
/// </summary>
public class UserDataRepository
{
    readonly SQLiteConnection _connection;

    // Exponential moving average factor for difficulty updates
    const double DifficultyAlpha = 0.3;

    // Key to track if defaults have been initialized
    const string SettingsInitializedKey = "system.settings_initialized";

    public UserDataRepository(SQLiteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Initializes all default settings if not already done.
    /// Called once on first app launch after database creation.
    /// </summary>
    public void InitializeDefaultsIfNeeded()
    {
        if (GetSetting(SettingsInitializedKey, false))
            return;

        System.Diagnostics.Debug.WriteLine("[UserData] Initializing default settings...");

        // Nouns settings
        SetSetting(SettingsKeys.NounsDailyGoal, SettingsKeys.DefaultDailyGoal);
        SetSetting(SettingsKeys.NounsLemmaMin, SettingsKeys.DefaultLemmaMin);
        SetSetting(SettingsKeys.NounsLemmaMax, SettingsKeys.DefaultLemmaMax);
        SetSetting(SettingsKeys.NounsCases, SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultCases));
        SetSetting(SettingsKeys.NounsNumbers, SettingsHelpers.ToCsv(SettingsKeys.DefaultNumbers));
        SetSetting(SettingsKeys.NounsMascPatterns, SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultMascPatterns));
        SetSetting(SettingsKeys.NounsFemPatterns, SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultFemPatterns));
        SetSetting(SettingsKeys.NounsNeutPatterns, SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultNeutPatterns));

        // Verbs settings
        SetSetting(SettingsKeys.VerbsDailyGoal, SettingsKeys.DefaultDailyGoal);
        SetSetting(SettingsKeys.VerbsLemmaMin, SettingsKeys.DefaultLemmaMin);
        SetSetting(SettingsKeys.VerbsLemmaMax, SettingsKeys.DefaultLemmaMax);
        SetSetting(SettingsKeys.VerbsTenses, SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultTenses));
        SetSetting(SettingsKeys.VerbsPersons, SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultPersons));
        SetSetting(SettingsKeys.VerbsNumbers, SettingsHelpers.ToCsv(SettingsKeys.DefaultNumbers));
        SetSetting(SettingsKeys.VerbsVoices, SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultVoices));
        SetSetting(SettingsKeys.VerbsPatterns, SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultPatterns));

        // Mark as initialized
        SetSetting(SettingsInitializedKey, true);
        System.Diagnostics.Debug.WriteLine("[UserData] Default settings initialized");
    }

    // === Noun Form Mastery ===

    public NounsFormMastery? GetNounFormMastery(long formId)
    {
        return _connection.Table<NounsFormMastery>()
            .FirstOrDefault(f => f.FormId == formId);
    }

    public List<NounsFormMastery> GetDueNounForms(int limit = 100)
    {
        // Filter in memory since IsDue is calculated
        return _connection.Table<NounsFormMastery>()
            .ToList()
            .Where(f => f.IsDue)
            .OrderBy(f => f.NextDueUtc)
            .Take(limit)
            .ToList();
    }

    public HashSet<long> GetPracticedNounFormIds()
    {
        return _connection.Table<NounsFormMastery>()
            .Select(f => f.FormId)
            .ToList()
            .ToHashSet();
    }

    public void RecordNounPracticeResult(long formId, bool wasEasy, string formText)
    {
        var existing = GetNounFormMastery(formId);
        var now = DateTime.UtcNow;

        int oldLevel;
        int newLevel;

        if (existing == null)
        {
            // First practice of this form
            oldLevel = CooldownCalculator.DefaultLevel;
            newLevel = CooldownCalculator.AdjustLevel(oldLevel, wasEasy);

            var record = new NounsFormMastery
            {
                FormId = formId,
                MasteryLevel = newLevel,
                PreviousLevel = oldLevel,
                LastPracticedUtc = now,
                CreatedUtc = now
            };
            _connection.Insert(record);
        }
        else
        {
            // Update existing record
            oldLevel = existing.MasteryLevel;
            newLevel = CooldownCalculator.AdjustLevel(oldLevel, wasEasy);

            existing.PreviousLevel = oldLevel;
            existing.MasteryLevel = newLevel;
            existing.LastPracticedUtc = now;

            _connection.Update(existing);
        }

        // Record to history
        var history = new NounsPracticeHistory
        {
            FormId = formId,
            FormText = formText,
            OldLevel = oldLevel,
            NewLevel = newLevel,
            PracticedUtc = now
        };
        _connection.Insert(history);
    }

    // === Verb Form Mastery ===

    public VerbsFormMastery? GetVerbFormMastery(long formId)
    {
        return _connection.Table<VerbsFormMastery>()
            .FirstOrDefault(f => f.FormId == formId);
    }

    public List<VerbsFormMastery> GetDueVerbForms(int limit = 100)
    {
        // Filter in memory since IsDue is calculated
        return _connection.Table<VerbsFormMastery>()
            .ToList()
            .Where(f => f.IsDue)
            .OrderBy(f => f.NextDueUtc)
            .Take(limit)
            .ToList();
    }

    public HashSet<long> GetPracticedVerbFormIds()
    {
        return _connection.Table<VerbsFormMastery>()
            .Select(f => f.FormId)
            .ToList()
            .ToHashSet();
    }

    public void RecordVerbPracticeResult(long formId, bool wasEasy, string formText)
    {
        var existing = GetVerbFormMastery(formId);
        var now = DateTime.UtcNow;

        int oldLevel;
        int newLevel;

        if (existing == null)
        {
            // First practice of this form
            oldLevel = CooldownCalculator.DefaultLevel;
            newLevel = CooldownCalculator.AdjustLevel(oldLevel, wasEasy);

            var record = new VerbsFormMastery
            {
                FormId = formId,
                MasteryLevel = newLevel,
                PreviousLevel = oldLevel,
                LastPracticedUtc = now,
                CreatedUtc = now
            };
            _connection.Insert(record);
        }
        else
        {
            // Update existing record
            oldLevel = existing.MasteryLevel;
            newLevel = CooldownCalculator.AdjustLevel(oldLevel, wasEasy);

            existing.PreviousLevel = oldLevel;
            existing.MasteryLevel = newLevel;
            existing.LastPracticedUtc = now;

            _connection.Update(existing);
        }

        // Record to history
        var history = new VerbsPracticeHistory
        {
            FormId = formId,
            FormText = formText,
            OldLevel = oldLevel,
            NewLevel = newLevel,
            PracticedUtc = now
        };
        _connection.Insert(history);
    }

    // === Noun Practice History ===

    public List<NounsPracticeHistory> GetRecentNounHistory(int limit = 50)
    {
        return _connection.Table<NounsPracticeHistory>()
            .OrderByDescending(h => h.PracticedUtc)
            .Take(limit)
            .ToList();
    }

    public List<NounsPracticeHistory> GetTodayNounHistory()
    {
        var todayStart = DateTime.UtcNow.Date;
        return _connection.Table<NounsPracticeHistory>()
            .Where(h => h.PracticedUtc >= todayStart)
            .OrderByDescending(h => h.PracticedUtc)
            .ToList();
    }

    // === Verb Practice History ===

    public List<VerbsPracticeHistory> GetRecentVerbHistory(int limit = 50)
    {
        return _connection.Table<VerbsPracticeHistory>()
            .OrderByDescending(h => h.PracticedUtc)
            .Take(limit)
            .ToList();
    }

    public List<VerbsPracticeHistory> GetTodayVerbHistory()
    {
        var todayStart = DateTime.UtcNow.Date;
        return _connection.Table<VerbsPracticeHistory>()
            .Where(h => h.PracticedUtc >= todayStart)
            .OrderByDescending(h => h.PracticedUtc)
            .ToList();
    }

    // === Noun Combination Difficulty ===

    public NounsCombinationDifficulty? GetNounDifficulty(string comboKey)
    {
        return _connection.Table<NounsCombinationDifficulty>()
            .FirstOrDefault(c => c.ComboKey == comboKey);
    }

    public NounsCombinationDifficulty? GetDeclensionDifficulty(Case @case, Gender gender, Number number)
    {
        return GetNounDifficulty(Declension.ComboKey(@case, gender, number));
    }

    public void UpdateDeclensionDifficulty(Case @case, Gender gender, Number number, bool wasHard)
    {
        var key = Declension.ComboKey(@case, gender, number);
        var existing = GetNounDifficulty(key);

        if (existing == null)
        {
            existing = NounsCombinationDifficulty.Create(@case, gender, number);
            UpdateNounDifficultyScore(existing, wasHard);
            _connection.Insert(existing);
        }
        else
        {
            UpdateNounDifficultyScore(existing, wasHard);
            _connection.Update(existing);
        }
    }

    void UpdateNounDifficultyScore(NounsCombinationDifficulty combo, bool wasHard)
    {
        // Exponential moving average: new_score = alpha * observation + (1-alpha) * old_score
        var observation = wasHard ? 1.0 : 0.0;
        combo.DifficultyScore = DifficultyAlpha * observation + (1 - DifficultyAlpha) * combo.DifficultyScore;
        combo.TotalAttempts++;
        combo.LastUpdatedUtc = DateTime.UtcNow;
    }

    public List<NounsCombinationDifficulty> GetHardestNounCombinations(int limit = 10)
    {
        return _connection.Table<NounsCombinationDifficulty>()
            .OrderByDescending(c => c.DifficultyScore)
            .Take(limit)
            .ToList();
    }

    // === Verb Combination Difficulty ===

    public VerbsCombinationDifficulty? GetVerbDifficulty(string comboKey)
    {
        return _connection.Table<VerbsCombinationDifficulty>()
            .FirstOrDefault(c => c.ComboKey == comboKey);
    }

    public VerbsCombinationDifficulty? GetConjugationDifficulty(Tense tense, Person person, Number number, bool reflexive)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        return GetVerbDifficulty(Conjugation.ComboKey(tense, person, number, voice));
    }

    public void UpdateConjugationDifficulty(Tense tense, Person person, Number number, bool reflexive, bool wasHard)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        var key = Conjugation.ComboKey(tense, person, number, voice);
        var existing = GetVerbDifficulty(key);

        if (existing == null)
        {
            existing = VerbsCombinationDifficulty.Create(tense, person, number, reflexive);
            UpdateVerbDifficultyScore(existing, wasHard);
            _connection.Insert(existing);
        }
        else
        {
            UpdateVerbDifficultyScore(existing, wasHard);
            _connection.Update(existing);
        }
    }

    void UpdateVerbDifficultyScore(VerbsCombinationDifficulty combo, bool wasHard)
    {
        // Exponential moving average: new_score = alpha * observation + (1-alpha) * old_score
        var observation = wasHard ? 1.0 : 0.0;
        combo.DifficultyScore = DifficultyAlpha * observation + (1 - DifficultyAlpha) * combo.DifficultyScore;
        combo.TotalAttempts++;
        combo.LastUpdatedUtc = DateTime.UtcNow;
    }

    public List<VerbsCombinationDifficulty> GetHardestVerbCombinations(int limit = 10)
    {
        return _connection.Table<VerbsCombinationDifficulty>()
            .OrderByDescending(c => c.DifficultyScore)
            .Take(limit)
            .ToList();
    }

    // === Type-Dispatching Helper Methods ===
    // These methods dispatch to type-specific implementations for backwards compatibility.

    /// <summary>
    /// Records a practice result, dispatching to type-specific methods.
    /// </summary>
    public void RecordPracticeResult(long formId, PracticeType type, bool wasEasy, string formText)
    {
        if (type == PracticeType.Declension)
            RecordNounPracticeResult(formId, wasEasy, formText);
        else
            RecordVerbPracticeResult(formId, wasEasy, formText);
    }

    /// <summary>
    /// Gets recent practice history as IPracticeHistory for display.
    /// </summary>
    public List<IPracticeHistory> GetRecentHistory(PracticeType type, int limit = 50)
    {
        if (type == PracticeType.Declension)
            return GetRecentNounHistory(limit).Cast<IPracticeHistory>().ToList();
        else
            return GetRecentVerbHistory(limit).Cast<IPracticeHistory>().ToList();
    }

    // === Settings ===

    public T GetSetting<T>(string key, T defaultValue)
    {
        var setting = _connection.Table<UserSetting>()
            .FirstOrDefault(s => s.Key == key);

        if (setting == null)
            return defaultValue;

        try
        {
            // Handle primitive types directly
            if (typeof(T) == typeof(string))
                return (T)(object)setting.Value;
            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(setting.Value, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(setting.Value);

            // For complex types, use JSON
            return JsonSerializer.Deserialize<T>(setting.Value) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public void SetSetting<T>(string key, T value)
    {
        string stringValue;
        if (typeof(T) == typeof(string))
            stringValue = (string)(object)value!;
        else if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal))
            stringValue = value?.ToString() ?? "";
        else
            stringValue = JsonSerializer.Serialize(value);

        var existing = _connection.Table<UserSetting>()
            .FirstOrDefault(s => s.Key == key);

        if (existing == null)
        {
            _connection.Insert(new UserSetting
            {
                Key = key,
                Value = stringValue,
                UpdatedUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Value = stringValue;
            existing.UpdatedUtc = DateTime.UtcNow;
            _connection.Update(existing);
        }
    }

    // === Daily Progress ===

    public DailyProgress GetTodayProgress()
    {
        var todayKey = DailyProgress.TodayKey;
        var existing = _connection.Table<DailyProgress>()
            .FirstOrDefault(p => p.Date == todayKey);

        if (existing != null)
            return existing;

        // Create new record for today
        var progress = DailyProgress.CreateForToday();
        _connection.Insert(progress);
        return progress;
    }

    public void IncrementProgress(PracticeType type)
    {
        var progress = GetTodayProgress();

        if (type == PracticeType.Declension)
            progress.DeclensionsCompleted++;
        else
            progress.ConjugationsCompleted++;

        _connection.Update(progress);
    }

    public bool IsDailyGoalMet(PracticeType type)
    {
        var progress = GetTodayProgress();
        var goal = GetDailyGoal(type);
        var completed = type == PracticeType.Declension
            ? progress.DeclensionsCompleted
            : progress.ConjugationsCompleted;

        return completed >= goal;
    }

    public int GetDailyGoal(PracticeType type)
    {
        var key = type == PracticeType.Declension
            ? SettingsKeys.NounsDailyGoal
            : SettingsKeys.VerbsDailyGoal;

        return GetSetting(key, SettingsKeys.DefaultDailyGoal);
    }

    // === Self-Healing Settings Getters ===
    // These methods read settings and rewrite defaults if the value is invalid/empty.
    // This ensures corrupted settings don't cause empty practice queues.

    /// <summary>
    /// Gets an enum list setting. If empty after parsing, rewrites the default and returns it.
    /// </summary>
    public List<T> GetEnumListOrResetDefault<T>(string key, T[] defaults) where T : struct, Enum
    {
        var csv = GetSetting(key, "");
        var parsed = SettingsHelpers.FromCsv<T>(csv);

        if (parsed.Count > 0)
            return parsed;

        // Empty or invalid - rewrite defaults
        System.Diagnostics.Debug.WriteLine($"[UserData] Setting '{key}' was empty/invalid, resetting to defaults");
        var defaultCsv = SettingsHelpers.ToCsv(defaults);
        SetSetting(key, defaultCsv);
        return defaults.ToList();
    }

    /// <summary>
    /// Gets an enum set setting. If empty after parsing, rewrites the default and returns it.
    /// </summary>
    public HashSet<T> GetEnumSetOrResetDefault<T>(string key, T[] defaults) where T : struct, Enum
        => GetEnumListOrResetDefault(key, defaults).ToHashSet();

    /// <summary>
    /// Gets a validated lemma range. If invalid (min >= max, out of bounds), rewrites defaults.
    /// </summary>
    public (int min, int max) GetLemmaRangeOrResetDefault(PracticeType type, int maxAllowed)
    {
        var (minKey, maxKey) = type == PracticeType.Declension
            ? (SettingsKeys.NounsLemmaMin, SettingsKeys.NounsLemmaMax)
            : (SettingsKeys.VerbsLemmaMin, SettingsKeys.VerbsLemmaMax);

        var min = GetSetting(minKey, SettingsKeys.DefaultLemmaMin);
        var max = GetSetting(maxKey, SettingsKeys.DefaultLemmaMax);

        var (validMin, validMax) = SettingsHelpers.ValidateRange(min, max, 1, maxAllowed);

        // If validation changed the values, rewrite them
        if (validMin != min || validMax != max)
        {
            System.Diagnostics.Debug.WriteLine($"[UserData] Lemma range for {type} was invalid ({min}-{max}), resetting to ({validMin}-{validMax})");
            SetSetting(minKey, validMin);
            SetSetting(maxKey, validMax);
        }

        return (validMin, validMax);
    }
}
