using System.Globalization;
using System.Text.Json;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Repository for user data access (mastery, difficulty, settings, history).
/// Uses type-specific tables for nouns and verbs.
/// </summary>
public class UserDataRepository : IUserDataRepository
{
    readonly SQLiteConnection _connection;
    readonly TimeProvider _timeProvider;

    // Key to track if defaults have been initialized
    const string SettingsInitializedKey = "system.settings_initialized";

    public UserDataRepository(SQLiteConnection connection, TimeProvider? timeProvider = null)
    {
        _connection = connection;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Queries due forms with pre-calculated cutoff dates for each mastery level.
    /// Uses centralized WHERE clause from CooldownCalculator.
    /// </summary>
    List<T> QueryDueForms<T>(string tableName, int limit) where T : new()
    {
        var sql = $@"
            SELECT * FROM {tableName}
            WHERE {CooldownCalculator.DueFormsWhereClause}
            ORDER BY last_practiced_utc, form_id
            LIMIT ?";

        var cutoffs = CooldownCalculator.GetDueCutoffParams(_timeProvider.GetUtcNow().UtcDateTime);

        return _connection.Query<T>(sql,
            cutoffs[0], cutoffs[1], cutoffs[2], cutoffs[3], cutoffs[4],
            cutoffs[5], cutoffs[6], cutoffs[7], cutoffs[8], cutoffs[9],
            limit);
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

        // Appearance settings
        SetSetting(SettingsKeys.AppearanceTheme, SettingsKeys.DefaultAppearanceTheme);
        SetSetting(
            SettingsKeys.AppearanceTranslationLanguage,
            (int)TranslationLanguageResolver.GetInitialPreference());

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
        => QueryDueForms<NounsFormMastery>("nouns_form_mastery", limit);

    public HashSet<long> GetPracticedNounFormIds()
    {
        return _connection.Table<NounsFormMastery>()
            .Select(f => f.FormId)
            .ToList()
            .ToHashSet();
    }

    public void RecordNounPracticeResult(long formId, bool wasEasy) =>
        RecordPractice<NounsFormMastery, NounsPracticeHistory>(formId, wasEasy, null);

    // === Verb Form Mastery ===

    public VerbsFormMastery? GetVerbFormMastery(long formId)
    {
        return _connection.Table<VerbsFormMastery>()
            .FirstOrDefault(f => f.FormId == formId);
    }

    public List<VerbsFormMastery> GetDueVerbForms(int limit = 100)
        => QueryDueForms<VerbsFormMastery>("verbs_form_mastery", limit);

    public HashSet<long> GetPracticedVerbFormIds()
    {
        return _connection.Table<VerbsFormMastery>()
            .Select(f => f.FormId)
            .ToList()
            .ToHashSet();
    }

    public void RecordVerbPracticeResult(long formId, bool wasEasy) =>
        RecordPractice<VerbsFormMastery, VerbsPracticeHistory>(formId, wasEasy, null);

    void RecordPractice<TMastery, THistory>(long formId, bool wasEasy, PracticeSnapshot? snapshot)
        where TMastery : FormMasteryBase, new()
        where THistory : PracticeHistoryBase, new()
    {
        _connection.RunInTransaction(() =>
        {
            var existing = _connection.Find<TMastery>(formId);
            var oldLevel = existing?.MasteryLevel ?? CooldownCalculator.DefaultLevel;
            var record = existing ?? new TMastery { FormId = formId };
            record.PreviousLevel = oldLevel;
            record.MasteryLevel = CooldownCalculator.AdjustLevel(oldLevel, wasEasy);
            record.LastPracticedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            if (existing is null)
                _connection.Insert(record);
            else
                _connection.Update(record);

            _connection.Insert(new THistory
            {
                FormId = formId,
                OldLevel = oldLevel,
                NewLevel = record.MasteryLevel,
                PracticedUtc = record.LastPracticedUtc,
                FormText = snapshot?.FormText ?? "",
                LemmaText = snapshot?.LemmaText ?? "",
                GrammarText = snapshot?.GrammarText ?? "",
                SnapshotOrigin = snapshot is null ? HistorySnapshotOrigin.Unknown : HistorySnapshotOrigin.Practiced
            });
        });
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
        var todayStart = _timeProvider.GetUtcNow().UtcDateTime.Date;
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
        var todayStart = _timeProvider.GetUtcNow().UtcDateTime.Date;
        return _connection.Table<VerbsPracticeHistory>()
            .Where(h => h.PracticedUtc >= todayStart)
            .OrderByDescending(h => h.PracticedUtc)
            .ToList();
    }

    // === Type-Dispatching Helper Methods ===
    // These methods dispatch to type-specific implementations for backwards compatibility.

    /// <summary>
    /// Records a practice result, dispatching to type-specific methods.
    /// </summary>
    public void RecordPracticeResult(long formId, PracticeType type, bool wasEasy) =>
        RecordPracticeResult(formId, type, wasEasy, null);

    public void RecordPracticeResult(long formId, PracticeType type, bool wasEasy, PracticeSnapshot? snapshot)
    {
        if (type == PracticeType.Declension)
            RecordPractice<NounsFormMastery, NounsPracticeHistory>(formId, wasEasy, snapshot);
        else if (type == PracticeType.Conjugation)
            RecordPractice<VerbsFormMastery, VerbsPracticeHistory>(formId, wasEasy, snapshot);
        else
            throw new ArgumentOutOfRangeException(nameof(type));
    }

    /// <summary>
    /// Gets recent practice history as IPracticeHistory for display.
    /// </summary>
    public List<IPracticeHistory> GetRecentHistory(PracticeType type, int limit = 50)
    {
        return type == PracticeType.Declension 
            ? GetRecentNounHistory(limit).Cast<IPracticeHistory>().ToList()
            : GetRecentVerbHistory(limit).Cast<IPracticeHistory>().ToList();
    }

    /// <summary>Completed answers, independent of queue builds and the current filters.</summary>
    public long GetPracticeCount(PracticeType type) => type == PracticeType.Declension
        ? _connection.ExecuteScalar<long>("SELECT COUNT(*) FROM nouns_practice_history")
        : _connection.ExecuteScalar<long>("SELECT COUNT(*) FROM verbs_practice_history");

    /// <summary>Stable seed date from the first recorded answer, without new stored state.</summary>
    public DateTime? GetFirstPracticeUtc(PracticeType type) => type == PracticeType.Declension
        ? _connection.Table<NounsPracticeHistory>().OrderBy(h => h.Id).FirstOrDefault()?.PracticedUtc
        : _connection.Table<VerbsPracticeHistory>().OrderBy(h => h.Id).FirstOrDefault()?.PracticedUtc;

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
                UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime
            });
        }
        else
        {
            existing.Value = stringValue;
            existing.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            _connection.Update(existing);
        }
    }

    /// <summary>
    /// Gets all stored settings as key-value pairs.
    /// </summary>
    public Dictionary<string, string> GetAllSettings()
    {
        return _connection.Table<UserSetting>()
            .ToDictionary(s => s.Key, s => s.Value);
    }

    // === Daily Progress ===

    public DailyProgress GetTodayProgress()
    {
        var todayKey = DailyProgress.GetDateKey(_timeProvider.GetLocalNow().DateTime);
        var existing = _connection.Table<DailyProgress>()
            .FirstOrDefault(p => p.Date == todayKey);

        if (existing != null)
            return existing;

        // Create new record for today
        var progress = new DailyProgress { Date = todayKey };
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
    // These methods read settings and rewrite defaults if the value is missing or invalid.
    // This ensures corrupted settings don't cause empty practice queues.

    /// <summary>
    /// Gets an enum list setting. If missing or invalid, rewrites the default and returns it.
    /// A stored empty string is returned as an empty list when <paramref name="allowEmpty"/> is true.
    /// Returns items in canonical order (sorted by enum value) for deterministic iteration.
    /// </summary>
    public List<T> GetEnumListOrResetDefault<T>(
        string key,
        T[] defaults,
        bool allowEmpty = false)
        where T : struct, Enum
    {
        var setting = _connection.Table<UserSetting>()
            .FirstOrDefault(candidate => candidate.Key == key);

        if (allowEmpty && setting?.Value == string.Empty)
            return [];

        var parsed = SettingsHelpers.FromCsv<T>(setting?.Value ?? string.Empty);

        if (parsed.Count > 0)
            return parsed.OrderBy(e => e).ToList();  // Canonical order for determinism

        // Missing, invalid, or disallowed empty value - rewrite defaults
        System.Diagnostics.Debug.WriteLine($"[UserData] Setting '{key}' was missing/invalid, resetting to defaults");
        var defaultCsv = SettingsHelpers.ToCsv(defaults);
        SetSetting(key, defaultCsv);
        return defaults.OrderBy(e => e).ToList();  // Canonical order for determinism
    }

    /// <summary>
    /// Gets an enum set setting. If missing or invalid, rewrites the default and returns it.
    /// A stored empty string is returned as an empty set when <paramref name="allowEmpty"/> is true.
    /// </summary>
    public HashSet<T> GetEnumSetOrResetDefault<T>(
        string key,
        T[] defaults,
        bool allowEmpty = false)
        where T : struct, Enum
        => GetEnumListOrResetDefault(key, defaults, allowEmpty).ToHashSet();

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
