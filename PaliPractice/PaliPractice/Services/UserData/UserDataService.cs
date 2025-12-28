using System.Text.Json;
using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Services.UserData;

public class UserDataService : IUserDataService
{
    SQLiteConnection? _database;
    readonly Lock _initLock = new();
    bool _isInitialized;

    // Exponential moving average factor for difficulty updates
    const double DifficultyAlpha = 0.3;

    public void Initialize()
    {
        if (_isInitialized) return;

        lock (_initLock)
        {
            if (_isInitialized) return;

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = System.IO.Path.Combine(appDataPath, "PaliPractice");

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            var databasePath = System.IO.Path.Combine(appFolder, "user_data.db");
            _database = new SQLiteConnection(databasePath);

            // Create tables
            _database.CreateTable<FormMastery>();
            _database.CreateTable<CombinationDifficulty>();
            _database.CreateTable<UserSetting>();
            _database.CreateTable<DailyProgress>();
            _database.CreateTable<PracticeHistory>();

            // Create indices for efficient querying
            _database.Execute("CREATE INDEX IF NOT EXISTS idx_form_mastery_type ON form_mastery(practice_type)");
            _database.Execute("CREATE INDEX IF NOT EXISTS idx_form_mastery_level ON form_mastery(practice_type, mastery_level)");
            _database.Execute("CREATE INDEX IF NOT EXISTS idx_combination_difficulty ON combination_difficulty(practice_type, difficulty_score DESC)");
            _database.Execute("CREATE INDEX IF NOT EXISTS idx_practice_history_date ON practice_history(practice_type, practiced_utc DESC)");

            _isInitialized = true;
        }
    }

    void EnsureInitialized()
    {
        if (!_isInitialized || _database == null)
            Initialize();
    }

    // === Form Mastery ===

    public FormMastery? GetFormMastery(long formId, PracticeType type)
    {
        EnsureInitialized();
        return _database!.Table<FormMastery>()
            .FirstOrDefault(f => f.FormId == formId && f.PracticeType == type);
    }

    public List<FormMastery> GetDueForms(PracticeType type, int limit = 100)
    {
        EnsureInitialized();
        // Filter in memory since IsDue is calculated
        return _database!.Table<FormMastery>()
            .Where(f => f.PracticeType == type)
            .ToList()
            .Where(f => f.IsDue)
            .OrderBy(f => f.NextDueUtc)
            .Take(limit)
            .ToList();
    }

    public HashSet<long> GetPracticedFormIds(PracticeType type)
    {
        EnsureInitialized();
        return _database!.Table<FormMastery>()
            .Where(f => f.PracticeType == type)
            .Select(f => f.FormId)
            .ToList()
            .ToHashSet();
    }

    public void RecordPracticeResult(long formId, PracticeType type, bool wasEasy, string formText)
    {
        EnsureInitialized();
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
            _database!.Insert(record);
        }
        else
        {
            // Update existing record
            oldLevel = existing.MasteryLevel;
            newLevel = CooldownCalculator.AdjustLevel(oldLevel, wasEasy);

            existing.PreviousLevel = oldLevel;
            existing.MasteryLevel = newLevel;
            existing.LastPracticedUtc = now;

            _database!.Update(existing);
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
        _database!.Insert(history);
    }

    // === Practice History ===

    public List<PracticeHistory> GetRecentHistory(PracticeType type, int limit = 50)
    {
        EnsureInitialized();
        return _database!.Table<PracticeHistory>()
            .Where(h => h.PracticeType == type)
            .OrderByDescending(h => h.PracticedUtc)
            .Take(limit)
            .ToList();
    }

    public List<PracticeHistory> GetTodayHistory(PracticeType type)
    {
        EnsureInitialized();
        var todayStart = DateTime.UtcNow.Date;
        return _database!.Table<PracticeHistory>()
            .Where(h => h.PracticeType == type && h.PracticedUtc >= todayStart)
            .OrderByDescending(h => h.PracticedUtc)
            .ToList();
    }

    // === Combination Difficulty ===

    public CombinationDifficulty? GetDifficulty(string comboKey)
    {
        EnsureInitialized();
        return _database!.Table<CombinationDifficulty>()
            .FirstOrDefault(c => c.ComboKey == comboKey);
    }

    public CombinationDifficulty? GetDeclensionDifficulty(Case @case, Gender gender, Number number)
    {
        return GetDifficulty(Declension.ComboKey(@case, gender, number));
    }

    public CombinationDifficulty? GetConjugationDifficulty(Tense tense, Person person, Number number, bool reflexive)
    {
        return GetDifficulty(Conjugation.ComboKey(tense, person, number, reflexive));
    }

    public void UpdateDeclensionDifficulty(Case @case, Gender gender, Number number, bool wasHard)
    {
        EnsureInitialized();
        var key = Declension.ComboKey(@case, gender, number);
        var existing = GetDifficulty(key);

        if (existing == null)
        {
            existing = CombinationDifficulty.ForDeclension(@case, gender, number);
            UpdateDifficultyScore(existing, wasHard);
            _database!.Insert(existing);
        }
        else
        {
            UpdateDifficultyScore(existing, wasHard);
            _database!.Update(existing);
        }
    }

    public void UpdateConjugationDifficulty(Tense tense, Person person, Number number, bool reflexive, bool wasHard)
    {
        EnsureInitialized();
        var key = Conjugation.ComboKey(tense, person, number, reflexive);
        var existing = GetDifficulty(key);

        if (existing == null)
        {
            existing = CombinationDifficulty.ForConjugation(tense, person, number, reflexive);
            UpdateDifficultyScore(existing, wasHard);
            _database!.Insert(existing);
        }
        else
        {
            UpdateDifficultyScore(existing, wasHard);
            _database!.Update(existing);
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
        EnsureInitialized();
        return _database!.Table<CombinationDifficulty>()
            .Where(c => c.PracticeType == type)
            .OrderByDescending(c => c.DifficultyScore)
            .Take(limit)
            .ToList();
    }

    // === Settings ===

    public T GetSetting<T>(string key, T defaultValue)
    {
        EnsureInitialized();
        var setting = _database!.Table<UserSetting>()
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
        EnsureInitialized();

        string stringValue;
        if (typeof(T) == typeof(string))
            stringValue = (string)(object)value!;
        else if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal))
            stringValue = value?.ToString() ?? "";
        else
            stringValue = JsonSerializer.Serialize(value);

        var existing = _database!.Table<UserSetting>()
            .FirstOrDefault(s => s.Key == key);

        if (existing == null)
        {
            _database.Insert(new UserSetting
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
            _database.Update(existing);
        }
    }

    // === Daily Progress ===

    public DailyProgress GetTodayProgress()
    {
        EnsureInitialized();
        var todayKey = DailyProgress.TodayKey;
        var existing = _database!.Table<DailyProgress>()
            .FirstOrDefault(p => p.Date == todayKey);

        if (existing != null)
            return existing;

        // Create new record for today
        var progress = DailyProgress.CreateForToday();
        _database.Insert(progress);
        return progress;
    }

    public void IncrementProgress(PracticeType type)
    {
        EnsureInitialized();
        var progress = GetTodayProgress();

        if (type == PracticeType.Declension)
            progress.DeclensionsCompleted++;
        else
            progress.ConjugationsCompleted++;

        _database!.Update(progress);
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
            ? SettingsKeys.DeclensionDailyGoal
            : SettingsKeys.ConjugationDailyGoal;

        return GetSetting(key, SettingsKeys.DefaultDailyGoal);
    }
}
