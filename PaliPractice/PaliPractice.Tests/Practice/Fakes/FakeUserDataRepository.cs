using PaliPractice.Models.Inflection;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Tests.Practice.Fakes;

/// <summary>
/// Fake implementation of IUserDataRepository for testing.
/// Stores settings and mastery data in memory.
/// </summary>
public class FakeUserDataRepository : IUserDataRepository
{
    readonly Dictionary<string, string> _settings = [];
    readonly Dictionary<long, NounsFormMastery> _nounMastery = [];
    readonly Dictionary<long, VerbsFormMastery> _verbMastery = [];
    readonly List<NounsPracticeHistory> _nounHistory = [];
    readonly List<VerbsPracticeHistory> _verbHistory = [];
    DailyProgress _todayProgress = DailyProgress.CreateForToday();

    // Counter for creating distinct timestamps to avoid tie problems in tests
    int _dueFormCounter;

    /// <summary>
    /// Adds a noun form with specified mastery level and due date.
    /// </summary>
    public void AddNounFormMastery(long formId, int masteryLevel, DateTime lastPracticedUtc)
    {
        _nounMastery[formId] = new NounsFormMastery
        {
            FormId = formId,
            MasteryLevel = masteryLevel,
            PreviousLevel = masteryLevel,
            LastPracticedUtc = lastPracticedUtc
        };
    }

    /// <summary>
    /// Adds a verb form with specified mastery level and due date.
    /// </summary>
    public void AddVerbFormMastery(long formId, int masteryLevel, DateTime lastPracticedUtc)
    {
        _verbMastery[formId] = new VerbsFormMastery
        {
            FormId = formId,
            MasteryLevel = masteryLevel,
            PreviousLevel = masteryLevel,
            LastPracticedUtc = lastPracticedUtc
        };
    }

    /// <summary>
    /// Creates a due noun form (practiced in the past such that it's now due).
    /// Each call creates a distinct timestamp to avoid tie problems in ordering.
    /// </summary>
    public void AddDueNounForm(long formId, int masteryLevel)
    {
        // Calculate a date that makes this form due based on mastery level.
        // Add a distinct offset per form to create predictable ordering.
        var cooldownHours = CooldownCalculator.GetCooldownHours(masteryLevel);
        var offset = TimeSpan.FromMinutes(_dueFormCounter++);
        var lastPracticed = DateTime.UtcNow.AddHours(-(cooldownHours + 1)) - offset;
        AddNounFormMastery(formId, masteryLevel, lastPracticed);
    }

    /// <summary>
    /// Creates a due verb form.
    /// Each call creates a distinct timestamp to avoid tie problems in ordering.
    /// </summary>
    public void AddDueVerbForm(long formId, int masteryLevel)
    {
        var cooldownHours = CooldownCalculator.GetCooldownHours(masteryLevel);
        var offset = TimeSpan.FromMinutes(_dueFormCounter++);
        var lastPracticed = DateTime.UtcNow.AddHours(-(cooldownHours + 1)) - offset;
        AddVerbFormMastery(formId, masteryLevel, lastPracticed);
    }

    // IUserDataRepository implementation

    public void InitializeDefaultsIfNeeded()
    {
        // No-op for tests - settings are configured explicitly
    }

    public NounsFormMastery? GetNounFormMastery(long formId) =>
        _nounMastery.GetValueOrDefault(formId);

    public List<NounsFormMastery> GetDueNounForms(int limit = 100) =>
        _nounMastery.Values
            .Where(f => f.IsDue)
            .OrderBy(f => f.LastPracticedUtc)  // Match production: ORDER BY last_practiced_utc, form_id
            .ThenBy(f => f.FormId)
            .Take(limit)
            .ToList();

    public HashSet<long> GetPracticedNounFormIds() =>
        _nounMastery.Keys.ToHashSet();

    public void RecordNounPracticeResult(long formId, bool wasEasy)
    {
        if (_nounMastery.TryGetValue(formId, out var existing))
        {
            existing.PreviousLevel = existing.MasteryLevel;
            existing.MasteryLevel = CooldownCalculator.AdjustLevel(existing.MasteryLevel, wasEasy);
            existing.LastPracticedUtc = DateTime.UtcNow;
        }
        else
        {
            var newLevel = CooldownCalculator.AdjustLevel(CooldownCalculator.DefaultLevel, wasEasy);
            _nounMastery[formId] = new NounsFormMastery
            {
                FormId = formId,
                MasteryLevel = newLevel,
                PreviousLevel = CooldownCalculator.DefaultLevel,
                LastPracticedUtc = DateTime.UtcNow
            };
        }
    }

    public VerbsFormMastery? GetVerbFormMastery(long formId) =>
        _verbMastery.GetValueOrDefault(formId);

    public List<VerbsFormMastery> GetDueVerbForms(int limit = 100) =>
        _verbMastery.Values
            .Where(f => f.IsDue)
            .OrderBy(f => f.LastPracticedUtc)  // Match production: ORDER BY last_practiced_utc, form_id
            .ThenBy(f => f.FormId)
            .Take(limit)
            .ToList();

    public HashSet<long> GetPracticedVerbFormIds() =>
        _verbMastery.Keys.ToHashSet();

    public void RecordVerbPracticeResult(long formId, bool wasEasy)
    {
        if (_verbMastery.TryGetValue(formId, out var existing))
        {
            existing.PreviousLevel = existing.MasteryLevel;
            existing.MasteryLevel = CooldownCalculator.AdjustLevel(existing.MasteryLevel, wasEasy);
            existing.LastPracticedUtc = DateTime.UtcNow;
        }
        else
        {
            var newLevel = CooldownCalculator.AdjustLevel(CooldownCalculator.DefaultLevel, wasEasy);
            _verbMastery[formId] = new VerbsFormMastery
            {
                FormId = formId,
                MasteryLevel = newLevel,
                PreviousLevel = CooldownCalculator.DefaultLevel,
                LastPracticedUtc = DateTime.UtcNow
            };
        }
    }

    public List<NounsPracticeHistory> GetRecentNounHistory(int limit = 50) =>
        _nounHistory.OrderByDescending(h => h.PracticedUtc).Take(limit).ToList();

    public List<NounsPracticeHistory> GetTodayNounHistory() =>
        _nounHistory.Where(h => h.PracticedUtc >= DateTime.UtcNow.Date).ToList();

    public List<VerbsPracticeHistory> GetRecentVerbHistory(int limit = 50) =>
        _verbHistory.OrderByDescending(h => h.PracticedUtc).Take(limit).ToList();

    public List<VerbsPracticeHistory> GetTodayVerbHistory() =>
        _verbHistory.Where(h => h.PracticedUtc >= DateTime.UtcNow.Date).ToList();

    public void RecordPracticeResult(long formId, PracticeType type, bool wasEasy)
    {
        if (type == PracticeType.Declension)
            RecordNounPracticeResult(formId, wasEasy);
        else
            RecordVerbPracticeResult(formId, wasEasy);
    }

    public List<IPracticeHistory> GetRecentHistory(PracticeType type, int limit = 50) =>
        type == PracticeType.Declension
            ? GetRecentNounHistory(limit).Cast<IPracticeHistory>().ToList()
            : GetRecentVerbHistory(limit).Cast<IPracticeHistory>().ToList();

    public T GetSetting<T>(string key, T defaultValue)
    {
        if (!_settings.TryGetValue(key, out var value))
            return defaultValue;

        if (typeof(T) == typeof(string))
            return (T)(object)value;
        if (typeof(T) == typeof(int))
            return (T)(object)int.Parse(value);
        if (typeof(T) == typeof(bool))
            return (T)(object)bool.Parse(value);

        return defaultValue;
    }

    public void SetSetting<T>(string key, T value)
    {
        _settings[key] = value?.ToString() ?? "";
    }

    public Dictionary<string, string> GetAllSettings() => new(_settings);

    public DailyProgress GetTodayProgress() => _todayProgress;

    public void IncrementProgress(PracticeType type)
    {
        if (type == PracticeType.Declension)
            _todayProgress.DeclensionsCompleted++;
        else
            _todayProgress.ConjugationsCompleted++;
    }

    public bool IsDailyGoalMet(PracticeType type)
    {
        var goal = GetDailyGoal(type);
        var completed = type == PracticeType.Declension
            ? _todayProgress.DeclensionsCompleted
            : _todayProgress.ConjugationsCompleted;
        return completed >= goal;
    }

    public int GetDailyGoal(PracticeType type)
    {
        var key = type == PracticeType.Declension
            ? SettingsKeys.NounsDailyGoal
            : SettingsKeys.VerbsDailyGoal;
        return GetSetting(key, SettingsKeys.DefaultDailyGoal);
    }

    public List<T> GetEnumListOrResetDefault<T>(string key, T[] defaults) where T : struct, Enum
    {
        var csv = GetSetting(key, "");
        var parsed = SettingsHelpers.FromCsv<T>(csv);
        if (parsed.Count > 0)
            return parsed.OrderBy(e => e).ToList();  // Canonical order for determinism

        // Reset to defaults
        SetSetting(key, SettingsHelpers.ToCsv(defaults));
        return defaults.OrderBy(e => e).ToList();  // Canonical order for determinism
    }

    public HashSet<T> GetEnumSetOrResetDefault<T>(string key, T[] defaults) where T : struct, Enum =>
        GetEnumListOrResetDefault(key, defaults).ToHashSet();

    public (int min, int max) GetLemmaRangeOrResetDefault(PracticeType type, int maxAllowed)
    {
        var (minKey, maxKey) = type == PracticeType.Declension
            ? (SettingsKeys.NounsLemmaMin, SettingsKeys.NounsLemmaMax)
            : (SettingsKeys.VerbsLemmaMin, SettingsKeys.VerbsLemmaMax);

        var min = GetSetting(minKey, SettingsKeys.DefaultLemmaMin);
        var max = GetSetting(maxKey, SettingsKeys.DefaultLemmaMax);

        var (validMin, validMax) = SettingsHelpers.ValidateRange(min, max, 1, maxAllowed);

        if (validMin != min || validMax != max)
        {
            SetSetting(minKey, validMin);
            SetSetting(maxKey, validMax);
        }

        return (validMin, validMax);
    }
}
