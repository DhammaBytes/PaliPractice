using System.Text.Json;
using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Repository for user data access (mastery, difficulty, settings, history).
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
        SetSetting(SettingsKeys.NounsLemmaPreset, SettingsKeys.DefaultLemmaPreset);
        SetSetting(SettingsKeys.NounsCases, SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultCases));
        SetSetting(SettingsKeys.NounsNumbers, SettingsHelpers.ToCsv(SettingsKeys.DefaultNumbers));
        SetSetting(SettingsKeys.NounsMascPatterns, SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultMascPatterns));
        SetSetting(SettingsKeys.NounsFemPatterns, SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultFemPatterns));
        SetSetting(SettingsKeys.NounsNeutPatterns, SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultNeutPatterns));

        // Verbs settings
        SetSetting(SettingsKeys.VerbsDailyGoal, SettingsKeys.DefaultDailyGoal);
        SetSetting(SettingsKeys.VerbsLemmaMin, SettingsKeys.DefaultLemmaMin);
        SetSetting(SettingsKeys.VerbsLemmaMax, SettingsKeys.DefaultLemmaMax);
        SetSetting(SettingsKeys.VerbsLemmaPreset, SettingsKeys.DefaultLemmaPreset);
        SetSetting(SettingsKeys.VerbsTenses, SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultTenses));
        SetSetting(SettingsKeys.VerbsPersons, SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultPersons));
        SetSetting(SettingsKeys.VerbsNumbers, SettingsHelpers.ToCsv(SettingsKeys.DefaultNumbers));
        SetSetting(SettingsKeys.VerbsVoices, SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultVoices));
        SetSetting(SettingsKeys.VerbsPatterns, SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultPatterns));

        // Mark as initialized
        SetSetting(SettingsInitializedKey, true);
        System.Diagnostics.Debug.WriteLine("[UserData] Default settings initialized");
    }

    // === Form Mastery ===

    public FormMastery? GetFormMastery(long formId, PracticeType type)
    {
        return _connection.Table<FormMastery>()
            .FirstOrDefault(f => f.FormId == formId && f.PracticeType == type);
    }

    public List<FormMastery> GetDueForms(PracticeType type, int limit = 100)
    {
        // Filter in memory since IsDue is calculated
        return _connection.Table<FormMastery>()
            .Where(f => f.PracticeType == type)
            .ToList()
            .Where(f => f.IsDue)
            .OrderBy(f => f.NextDueUtc)
            .Take(limit)
            .ToList();
    }

    public HashSet<long> GetPracticedFormIds(PracticeType type)
    {
        return _connection.Table<FormMastery>()
            .Where(f => f.PracticeType == type)
            .Select(f => f.FormId)
            .ToList()
            .ToHashSet();
    }

    public void RecordPracticeResult(long formId, PracticeType type, bool wasEasy, string formText)
    {
        var existing = GetFormMastery(formId, type);
        var now = DateTime.UtcNow;

        int oldLevel;
        int newLevel;

        if (existing == null)
        {
            // First practice of this form
            oldLevel = CooldownCalculator.DefaultLevel;
            newLevel = CooldownCalculator.AdjustLevel(oldLevel, wasEasy);

            var record = new FormMastery
            {
                FormId = formId,
                PracticeType = type,
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
        var history = new PracticeHistory
        {
            FormId = formId,
            PracticeType = type,
            FormText = formText,
            OldLevel = oldLevel,
            NewLevel = newLevel,
            PracticedUtc = now
        };
        _connection.Insert(history);
    }

    // === Practice History ===

    public List<PracticeHistory> GetRecentHistory(PracticeType type, int limit = 50)
    {
        return _connection.Table<PracticeHistory>()
            .Where(h => h.PracticeType == type)
            .OrderByDescending(h => h.PracticedUtc)
            .Take(limit)
            .ToList();
    }

    public List<PracticeHistory> GetTodayHistory(PracticeType type)
    {
        var todayStart = DateTime.UtcNow.Date;
        return _connection.Table<PracticeHistory>()
            .Where(h => h.PracticeType == type && h.PracticedUtc >= todayStart)
            .OrderByDescending(h => h.PracticedUtc)
            .ToList();
    }

    // === Combination Difficulty ===

    public CombinationDifficulty? GetDifficulty(string comboKey)
    {
        return _connection.Table<CombinationDifficulty>()
            .FirstOrDefault(c => c.ComboKey == comboKey);
    }

    public CombinationDifficulty? GetDeclensionDifficulty(Case @case, Gender gender, Number number)
    {
        return GetDifficulty(Declension.ComboKey(@case, gender, number));
    }

    public CombinationDifficulty? GetConjugationDifficulty(Tense tense, Person person, Number number, bool reflexive)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Normal;
        return GetDifficulty(Conjugation.ComboKey(tense, person, number, voice));
    }

    public void UpdateDeclensionDifficulty(Case @case, Gender gender, Number number, bool wasHard)
    {
        var key = Declension.ComboKey(@case, gender, number);
        var existing = GetDifficulty(key);

        if (existing == null)
        {
            existing = CombinationDifficulty.ForDeclension(@case, gender, number);
            UpdateDifficultyScore(existing, wasHard);
            _connection.Insert(existing);
        }
        else
        {
            UpdateDifficultyScore(existing, wasHard);
            _connection.Update(existing);
        }
    }

    public void UpdateConjugationDifficulty(Tense tense, Person person, Number number, bool reflexive, bool wasHard)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Normal;
        var key = Conjugation.ComboKey(tense, person, number, voice);
        var existing = GetDifficulty(key);

        if (existing == null)
        {
            existing = CombinationDifficulty.ForConjugation(tense, person, number, reflexive);
            UpdateDifficultyScore(existing, wasHard);
            _connection.Insert(existing);
        }
        else
        {
            UpdateDifficultyScore(existing, wasHard);
            _connection.Update(existing);
        }
    }

    void UpdateDifficultyScore(CombinationDifficulty combo, bool wasHard)
    {
        // Exponential moving average: new_score = alpha * observation + (1-alpha) * old_score
        var observation = wasHard ? 1.0 : 0.0;
        combo.DifficultyScore = DifficultyAlpha * observation + (1 - DifficultyAlpha) * combo.DifficultyScore;
        combo.TotalAttempts++;
        combo.LastUpdatedUtc = DateTime.UtcNow;
    }

    public List<CombinationDifficulty> GetHardestCombinations(PracticeType type, int limit = 10)
    {
        return _connection.Table<CombinationDifficulty>()
            .Where(c => c.PracticeType == type)
            .OrderByDescending(c => c.DifficultyScore)
            .Take(limit)
            .ToList();
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
                return (T)(object)int.Parse(setting.Value);
            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(setting.Value);
            if (typeof(T) == typeof(double))
                return (T)(object)double.Parse(setting.Value);

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
}
