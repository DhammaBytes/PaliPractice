using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Interface for user data repository operations.
/// Enables testing with fake implementations.
/// </summary>
public interface IUserDataRepository
{
    // === Initialization ===

    /// <summary>
    /// Initializes all default settings if not already done.
    /// </summary>
    void InitializeDefaultsIfNeeded();

    // === Noun Form Mastery ===

    NounsFormMastery? GetNounFormMastery(long formId);
    List<NounsFormMastery> GetDueNounForms(int limit = 100);
    HashSet<long> GetPracticedNounFormIds();
    void RecordNounPracticeResult(long formId, bool wasEasy);

    // === Verb Form Mastery ===

    VerbsFormMastery? GetVerbFormMastery(long formId);
    List<VerbsFormMastery> GetDueVerbForms(int limit = 100);
    HashSet<long> GetPracticedVerbFormIds();
    void RecordVerbPracticeResult(long formId, bool wasEasy);

    // === Practice History ===

    List<NounsPracticeHistory> GetRecentNounHistory(int limit = 50);
    List<NounsPracticeHistory> GetTodayNounHistory();
    List<VerbsPracticeHistory> GetRecentVerbHistory(int limit = 50);
    List<VerbsPracticeHistory> GetTodayVerbHistory();

    // === Type-Dispatching Methods ===

    void RecordPracticeResult(long formId, PracticeType type, bool wasEasy);
    List<IPracticeHistory> GetRecentHistory(PracticeType type, int limit = 50);

    // === Settings ===

    T GetSetting<T>(string key, T defaultValue);
    void SetSetting<T>(string key, T value);
    Dictionary<string, string> GetAllSettings();

    // === Daily Progress ===

    DailyProgress GetTodayProgress();
    void IncrementProgress(PracticeType type);
    bool IsDailyGoalMet(PracticeType type);
    int GetDailyGoal(PracticeType type);

    // === Self-Healing Settings Getters ===

    /// <summary>
    /// Gets an enum list setting. If empty after parsing, rewrites the default and returns it.
    /// </summary>
    List<T> GetEnumListOrResetDefault<T>(string key, T[] defaults) where T : struct, Enum;

    /// <summary>
    /// Gets an enum set setting. If empty after parsing, rewrites the default and returns it.
    /// </summary>
    HashSet<T> GetEnumSetOrResetDefault<T>(string key, T[] defaults) where T : struct, Enum;

    /// <summary>
    /// Gets a validated lemma range. If invalid, rewrites defaults.
    /// </summary>
    (int min, int max) GetLemmaRangeOrResetDefault(PracticeType type, int maxAllowed);
}
