using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData.Statistics;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Repository for computing aggregated statistics from user data.
/// </summary>
public interface IStatisticsRepository
{
    // === General Statistics ===

    /// <summary>
    /// Gets calendar data for the specified month.
    /// </summary>
    /// <param name="year">Year (defaults to current year if null).</param>
    /// <param name="month">Month 1-12 (defaults to current month if null).</param>
    List<CalendarDayDto> GetCalendarData(int? year = null, int? month = null);

    /// <summary>
    /// Gets calendar data for the last 30 days (including today).
    /// Returns 30 entries ordered from oldest to newest.
    /// </summary>
    List<CalendarDayDto> GetLast30DaysCalendar();

    /// <summary>
    /// Calculates the current consecutive practice streak (days with any practice).
    /// </summary>
    int GetCurrentPracticeStreak();

    /// <summary>
    /// Calculates the longest practice streak ever achieved.
    /// </summary>
    int GetLongestPracticeStreak();

    /// <summary>
    /// Calculates the current consecutive daily goal streak for a practice type.
    /// </summary>
    int GetCurrentGoalStreak(PracticeType type);

    /// <summary>
    /// Gets total number of days with any practice.
    /// </summary>
    int GetTotalPracticeDays();

    /// <summary>
    /// Gets complete general statistics bundle.
    /// </summary>
    GeneralStatsDto GetGeneralStats();

    // === Noun Statistics ===

    /// <summary>Gets complete noun practice statistics.</summary>
    PracticeTypeStatsDto GetNounStats();

    /// <summary>Gets noun SRS distribution across mastery levels.</summary>
    SrsDistributionDto GetNounSrsDistribution();

    /// <summary>Gets top N strongest noun combos (highest average mastery).</summary>
    List<ComboStatDto> GetStrongestNounCombos(int count = 5);

    /// <summary>Gets top N weakest noun combos (lowest average mastery).</summary>
    List<ComboStatDto> GetWeakestNounCombos(int count = 5);

    /// <summary>Gets noun period statistics (today, 7 days, all time).</summary>
    PeriodStatsDto GetNounPeriodStats();

    // === Verb Statistics ===

    /// <summary>Gets complete verb practice statistics.</summary>
    PracticeTypeStatsDto GetVerbStats();

    /// <summary>Gets verb SRS distribution across mastery levels.</summary>
    SrsDistributionDto GetVerbSrsDistribution();

    /// <summary>Gets top N strongest verb combos (highest average mastery).</summary>
    List<ComboStatDto> GetStrongestVerbCombos(int count = 5);

    /// <summary>Gets top N weakest verb combos (lowest average mastery).</summary>
    List<ComboStatDto> GetWeakestVerbCombos(int count = 5);

    /// <summary>Gets verb period statistics (today, 7 days, all time).</summary>
    PeriodStatsDto GetVerbPeriodStats();
}
