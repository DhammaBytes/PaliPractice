using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using PaliPractice.Services.UserData.Statistics;
using SQLite;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Repository for computing aggregated statistics from user data.
/// </summary>
public class StatisticsRepository : IStatisticsRepository
{
    readonly SQLiteConnection _connection;
    readonly IUserDataRepository _userData;

    public StatisticsRepository(SQLiteConnection connection, IUserDataRepository userData)
    {
        _connection = connection;
        _userData = userData;
    }

    // === General Statistics ===

    public List<CalendarDayDto> GetCalendarData(int? year = null, int? month = null)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;

        // Get first and last day of the month
        var firstDay = new DateTime(targetYear, targetMonth, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var firstDayStr = firstDay.ToString("yyyy-MM-dd");
        var lastDayStr = lastDay.ToString("yyyy-MM-dd");

        // Fetch all daily_progress and filter in memory (SQLite lacks string.Compare support)
        var allProgress = _connection.Table<DailyProgress>().ToList();
        var rows = allProgress
            .Where(p => string.CompareOrdinal(p.Date, firstDayStr) >= 0 &&
                       string.CompareOrdinal(p.Date, lastDayStr) <= 0)
            .ToList();

        // Build calendar with all days of the month
        var result = new List<CalendarDayDto>();
        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            var dateStr = day.ToString("yyyy-MM-dd");
            var progress = rows.FirstOrDefault(r => r.Date == dateStr);

            result.Add(new CalendarDayDto
            {
                Date = dateStr,
                DeclensionsCount = progress?.DeclensionsCompleted ?? 0,
                ConjugationsCount = progress?.ConjugationsCompleted ?? 0
            });
        }

        return result;
    }

    public List<CalendarDayDto> GetLast30DaysCalendar()
    {
        var today = DateTime.Now.Date;
        var startDate = today.AddDays(-29); // 30 days including today

        var startDateStr = startDate.ToString("yyyy-MM-dd");
        var endDateStr = today.ToString("yyyy-MM-dd");

        // Fetch all daily_progress and filter in memory (SQLite lacks string.Compare support)
        var allProgress = _connection.Table<DailyProgress>().ToList();
        var rows = allProgress
            .Where(p => string.CompareOrdinal(p.Date, startDateStr) >= 0 &&
                       string.CompareOrdinal(p.Date, endDateStr) <= 0)
            .ToList();

        // Build calendar with all 30 days
        var result = new List<CalendarDayDto>();
        for (var day = startDate; day <= today; day = day.AddDays(1))
        {
            var dateStr = day.ToString("yyyy-MM-dd");
            var progress = rows.FirstOrDefault(r => r.Date == dateStr);

            result.Add(new CalendarDayDto
            {
                Date = dateStr,
                DeclensionsCount = progress?.DeclensionsCompleted ?? 0,
                ConjugationsCount = progress?.ConjugationsCompleted ?? 0
            });
        }

        return result;
    }

    public int GetCurrentPracticeStreak()
    {
        return CalculateStreak(countingBackwards: true, requireGoalMet: false, type: null);
    }

    public int GetLongestPracticeStreak()
    {
        // Get all practice days ordered by date
        var allDays = _connection.Table<DailyProgress>()
            .Where(p => p.DeclensionsCompleted > 0 || p.ConjugationsCompleted > 0)
            .OrderBy(p => p.Date)
            .Select(p => p.Date)
            .ToList();

        if (allDays.Count == 0)
            return 0;

        int longestStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < allDays.Count; i++)
        {
            var prevDate = DateTime.Parse(allDays[i - 1]);
            var currDate = DateTime.Parse(allDays[i]);

            if ((currDate - prevDate).Days == 1)
            {
                currentStreak++;
                if (currentStreak > longestStreak)
                    longestStreak = currentStreak;
            }
            else
            {
                currentStreak = 1;
            }
        }

        return longestStreak;
    }

    public int GetCurrentGoalStreak(PracticeType type)
    {
        return CalculateStreak(countingBackwards: true, requireGoalMet: true, type: type);
    }

    public int GetTotalPracticeDays()
    {
        return _connection.Table<DailyProgress>()
            .Count(p => p.DeclensionsCompleted > 0 || p.ConjugationsCompleted > 0);
    }

    public GeneralStatsDto GetGeneralStats()
    {
        var todayProgress = _userData.GetTodayProgress();

        return new GeneralStatsDto
        {
            CurrentPracticeStreak = GetCurrentPracticeStreak(),
            LongestPracticeStreak = GetLongestPracticeStreak(),
            CurrentNounGoalStreak = GetCurrentGoalStreak(PracticeType.Declension),
            CurrentVerbGoalStreak = GetCurrentGoalStreak(PracticeType.Conjugation),
            TotalPracticeDays = GetTotalPracticeDays(),
            TodayDeclensions = todayProgress.DeclensionsCompleted,
            TodayConjugations = todayProgress.ConjugationsCompleted,
            NounGoalMet = _userData.IsDailyGoalMet(PracticeType.Declension),
            VerbGoalMet = _userData.IsDailyGoalMet(PracticeType.Conjugation)
        };
    }

    /// <summary>
    /// Calculates streak by counting consecutive days backwards from today.
    /// </summary>
    int CalculateStreak(bool countingBackwards, bool requireGoalMet, PracticeType? type)
    {
        var todayKey = DailyProgress.TodayKey;

        // Get all days ordered descending
        var query = _connection.Table<DailyProgress>()
            .OrderByDescending(p => p.Date)
            .ToList();

        if (query.Count == 0)
            return 0;

        // Get daily goals if needed
        int nounGoal = requireGoalMet ? _userData.GetDailyGoal(PracticeType.Declension) : 0;
        int verbGoal = requireGoalMet ? _userData.GetDailyGoal(PracticeType.Conjugation) : 0;

        int streak = 0;
        var expectedDate = DateTime.Parse(todayKey);

        foreach (var day in query)
        {
            var dayDate = DateTime.Parse(day.Date);

            // Check if this is the expected consecutive day
            if (dayDate != expectedDate)
            {
                // If we haven't started counting and today is missing, check yesterday
                if (streak == 0 && (expectedDate - dayDate).Days == 1)
                {
                    expectedDate = dayDate;
                }
                else
                {
                    break; // Gap found, streak ends
                }
            }

            // Check if day qualifies for streak
            bool qualifies;
            if (requireGoalMet && type.HasValue)
            {
                qualifies = type.Value == PracticeType.Declension
                    ? day.DeclensionsCompleted >= nounGoal
                    : day.ConjugationsCompleted >= verbGoal;
            }
            else
            {
                qualifies = day.DeclensionsCompleted > 0 || day.ConjugationsCompleted > 0;
            }

            if (qualifies)
            {
                streak++;
                expectedDate = expectedDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    // === Noun Statistics ===

    public PracticeTypeStatsDto GetNounStats()
    {
        return new PracticeTypeStatsDto
        {
            TotalPracticed = _connection.Table<NounsFormMastery>().Count(),
            DueForReview = CountDueNounForms(),
            Distribution = GetNounSrsDistribution(),
            StrongestCombos = GetStrongestNounCombos(5),
            WeakestCombos = GetWeakestNounCombos(5),
            PeriodStats = GetNounPeriodStats()
        };
    }

    /// <summary>
    /// Counts all due noun forms without loading them into memory.
    /// </summary>
    int CountDueNounForms() => CountDueForms("nouns_form_mastery");

    /// <summary>
    /// Counts all due forms in a table using centralized WHERE clause.
    /// </summary>
    int CountDueForms(string tableName)
    {
        var sql = $"SELECT COUNT(*) FROM {tableName} WHERE {CooldownCalculator.DueFormsWhereClause}";
        var cutoffs = CooldownCalculator.GetDueCutoffParams();

        return _connection.ExecuteScalar<int>(sql,
            cutoffs[0], cutoffs[1], cutoffs[2], cutoffs[3], cutoffs[4],
            cutoffs[5], cutoffs[6], cutoffs[7], cutoffs[8], cutoffs[9]);
    }

    public SrsDistributionDto GetNounSrsDistribution()
    {
        var forms = _connection.Table<NounsFormMastery>().ToList();

        return new SrsDistributionDto
        {
            Unpracticed = 0, // Unpracticed forms aren't in the table
            Struggling = forms.Count(f => f.MasteryLevel >= 1 && f.MasteryLevel <= 3),
            Learning = forms.Count(f => f.MasteryLevel >= 4 && f.MasteryLevel <= 6),
            Strong = forms.Count(f => f.MasteryLevel >= 7 && f.MasteryLevel <= 10),
            Mastered = forms.Count(f => f.MasteryLevel == 11)
        };
    }

    /// <summary>
    /// Minimum number of forms required for a combo to be included in stats.
    /// Combos with fewer forms are not statistically meaningful.
    /// </summary>
    const int MinFormsPerCombo = 3;

    public List<ComboStatDto> GetStrongestNounCombos(int count = 5)
    {
        return PadToCount(GetNounComboStats()
            .Where(c => c.FormCount >= MinFormsPerCombo)
            .OrderByDescending(c => c.AverageMastery)
            .Take(count)
            .ToList(), count);
    }

    public List<ComboStatDto> GetWeakestNounCombos(int count = 5)
    {
        return PadToCount(GetNounComboStats()
            .Where(c => c.FormCount >= MinFormsPerCombo)
            .OrderBy(c => c.AverageMastery)
            .Take(count)
            .ToList(), count);
    }

    static List<ComboStatDto> PadToCount(List<ComboStatDto> list, int count)
    {
        while (list.Count < count)
            list.Add(ComboStatDto.Placeholder);
        return list;
    }

    List<ComboStatDto> GetNounComboStats()
    {
        var forms = _connection.Table<NounsFormMastery>().ToList();

        // Group by combo (case + gender + number)
        var groups = forms
            .GroupBy(f =>
            {
                var parsed = Declension.ParseId(f.FormId);
                return (parsed.Case, parsed.Gender, parsed.Number);
            })
            .Select(g => new ComboStatDto
            {
                ComboKey = Declension.ComboKey(g.Key.Case, g.Key.Gender, g.Key.Number),
                DisplayName = ComboDisplayHelper.GetNounComboDisplayName(g.Key.Case, g.Key.Gender, g.Key.Number),
                FormCount = g.Count(),
                AverageMastery = g.Average(f => f.MasteryLevel)
            })
            .ToList();

        return groups;
    }

    public PeriodStatsDto GetNounPeriodStats()
    {
        // Use logical day boundaries (5am local time) for consistency with DailyProgress
        var todayStart = DailyProgress.TodayStartUtc;
        var weekAgo = DailyProgress.GetDayStartUtc(6); // 6 days ago = 7 day window including today

        var history = _connection.Table<NounsPracticeHistory>().ToList();

        return new PeriodStatsDto
        {
            Today = history.Where(h => h.PracticedUtc >= todayStart).Select(h => h.FormId).Distinct().Count(),
            Last7Days = history.Where(h => h.PracticedUtc >= weekAgo).Select(h => h.FormId).Distinct().Count(),
            AllTime = history.Select(h => h.FormId).Distinct().Count()
        };
    }

    // === Verb Statistics ===

    public PracticeTypeStatsDto GetVerbStats()
    {
        return new PracticeTypeStatsDto
        {
            TotalPracticed = _connection.Table<VerbsFormMastery>().Count(),
            DueForReview = CountDueVerbForms(),
            Distribution = GetVerbSrsDistribution(),
            StrongestCombos = GetStrongestVerbCombos(5),
            WeakestCombos = GetWeakestVerbCombos(5),
            PeriodStats = GetVerbPeriodStats()
        };
    }

    /// <summary>
    /// Counts all due verb forms without loading them into memory.
    /// </summary>
    int CountDueVerbForms() => CountDueForms("verbs_form_mastery");

    public SrsDistributionDto GetVerbSrsDistribution()
    {
        var forms = _connection.Table<VerbsFormMastery>().ToList();

        return new SrsDistributionDto
        {
            Unpracticed = 0, // Unpracticed forms aren't in the table
            Struggling = forms.Count(f => f.MasteryLevel >= 1 && f.MasteryLevel <= 3),
            Learning = forms.Count(f => f.MasteryLevel >= 4 && f.MasteryLevel <= 6),
            Strong = forms.Count(f => f.MasteryLevel >= 7 && f.MasteryLevel <= 10),
            Mastered = forms.Count(f => f.MasteryLevel == 11)
        };
    }

    public List<ComboStatDto> GetStrongestVerbCombos(int count = 5)
    {
        return PadToCount(GetVerbComboStats()
            .Where(c => c.FormCount >= MinFormsPerCombo)
            .OrderByDescending(c => c.AverageMastery)
            .Take(count)
            .ToList(), count);
    }

    public List<ComboStatDto> GetWeakestVerbCombos(int count = 5)
    {
        return PadToCount(GetVerbComboStats()
            .Where(c => c.FormCount >= MinFormsPerCombo)
            .OrderBy(c => c.AverageMastery)
            .Take(count)
            .ToList(), count);
    }

    List<ComboStatDto> GetVerbComboStats()
    {
        var forms = _connection.Table<VerbsFormMastery>().ToList();

        // Group by combo (tense + person + number + voice)
        var groups = forms
            .GroupBy(f =>
            {
                var parsed = Conjugation.ParseId(f.FormId);
                return (parsed.Tense, parsed.Person, parsed.Number, parsed.Voice);
            })
            .Select(g => new ComboStatDto
            {
                ComboKey = Conjugation.ComboKey(g.Key.Tense, g.Key.Person, g.Key.Number, g.Key.Voice),
                DisplayName = ComboDisplayHelper.GetVerbComboDisplayName(g.Key.Tense, g.Key.Person, g.Key.Number, g.Key.Voice),
                FormCount = g.Count(),
                AverageMastery = g.Average(f => f.MasteryLevel)
            })
            .ToList();

        return groups;
    }

    public PeriodStatsDto GetVerbPeriodStats()
    {
        // Use logical day boundaries (5am local time) for consistency with DailyProgress
        var todayStart = DailyProgress.TodayStartUtc;
        var weekAgo = DailyProgress.GetDayStartUtc(6); // 6 days ago = 7 day window including today

        var history = _connection.Table<VerbsPracticeHistory>().ToList();

        return new PeriodStatsDto
        {
            Today = history.Where(h => h.PracticedUtc >= todayStart).Select(h => h.FormId).Distinct().Count(),
            Last7Days = history.Where(h => h.PracticedUtc >= weekAgo).Select(h => h.FormId).Distinct().Count(),
            AllTime = history.Select(h => h.FormId).Distinct().Count()
        };
    }
}
