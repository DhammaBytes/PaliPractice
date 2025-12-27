using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Services.UserData;

/// <summary>
/// Manages user progress data in a separate writable database (user_data.db).
/// Handles form mastery tracking, combination difficulty, settings, and daily progress.
/// </summary>
public interface IUserDataService
{
    /// <summary>
    /// Initialize the user database, creating tables if needed.
    /// </summary>
    void Initialize();

    // === Form Mastery ===

    /// <summary>
    /// Get mastery record for a form. Returns null if never practiced.
    /// </summary>
    FormMastery? GetFormMastery(long formId, PracticeType type);

    /// <summary>
    /// Get all forms due for review (calculated from LastPracticedUtc + cooldown).
    /// </summary>
    List<FormMastery> GetDueForms(PracticeType type, int limit = 100);

    /// <summary>
    /// Get all practiced form IDs for determining untried forms.
    /// </summary>
    HashSet<long> GetPracticedFormIds(PracticeType type);

    /// <summary>
    /// Record practice result and update mastery level.
    /// Creates a new record if first practice, updates existing otherwise.
    /// Also records to history table.
    /// </summary>
    /// <param name="formText">The actual inflected form text for history display.</param>
    void RecordPracticeResult(long formId, PracticeType type, bool wasEasy, string formText);

    // === Practice History ===

    /// <summary>
    /// Get recent practice history for the HistoryPage.
    /// </summary>
    List<PracticeHistory> GetRecentHistory(PracticeType type, int limit = 50);

    /// <summary>
    /// Get today's practice history.
    /// </summary>
    List<PracticeHistory> GetTodayHistory(PracticeType type);

    // === Combination Difficulty ===

    /// <summary>
    /// Get difficulty by combo key (e.g., "nom_m_sg" or "pr_1st_sg_refl").
    /// Returns null if never tracked (defaults to 0.5).
    /// </summary>
    CombinationDifficulty? GetDifficulty(string comboKey);

    /// <summary>
    /// Get difficulty for a declension combination.
    /// Returns null if never tracked (defaults to 0.5).
    /// </summary>
    CombinationDifficulty? GetDeclensionDifficulty(Case @case, Gender gender, Number number);

    /// <summary>
    /// Get difficulty for a conjugation combination.
    /// Returns null if never tracked (defaults to 0.5).
    /// </summary>
    CombinationDifficulty? GetConjugationDifficulty(Tense tense, Person person, Number number, bool reflexive);

    /// <summary>
    /// Update difficulty after practice using exponential moving average.
    /// Hard = increases difficulty, Easy = decreases difficulty.
    /// </summary>
    void UpdateDeclensionDifficulty(Case @case, Gender gender, Number number, bool wasHard);

    /// <summary>
    /// Update difficulty after practice using exponential moving average.
    /// Hard = increases difficulty, Easy = decreases difficulty.
    /// </summary>
    void UpdateConjugationDifficulty(Tense tense, Person person, Number number, bool reflexive, bool wasHard);

    /// <summary>
    /// Get hardest combinations for prioritization during queue building.
    /// </summary>
    List<CombinationDifficulty> GetHardestCombinations(PracticeType type, int limit = 10);

    // === Settings ===

    /// <summary>
    /// Get a setting value, returning defaultValue if not found.
    /// </summary>
    T GetSetting<T>(string key, T defaultValue);

    /// <summary>
    /// Set a setting value.
    /// </summary>
    void SetSetting<T>(string key, T value);

    // === Daily Progress ===

    /// <summary>
    /// Get today's progress record, creating one if needed.
    /// </summary>
    DailyProgress GetTodayProgress();

    /// <summary>
    /// Increment today's completion count for a practice type.
    /// </summary>
    void IncrementProgress(PracticeType type);

    /// <summary>
    /// Check if daily goal is met for a practice type.
    /// </summary>
    bool IsDailyGoalMet(PracticeType type);

    /// <summary>
    /// Get the daily goal for a practice type.
    /// </summary>
    int GetDailyGoal(PracticeType type);
}
