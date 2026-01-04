using PaliPractice.Models.Inflection;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using PaliPractice.Services.UserData.Statistics;
using SQLite;

namespace PaliPractice.Tests.Statistics;

/// <summary>
/// Tests for StatisticsRepository with focus on edge cases and non-obvious behavior.
/// Uses in-memory SQLite database for isolation.
/// </summary>
[TestFixture]
public class StatisticsRepositoryTests
{
    SQLiteConnection _connection = null!;
    UserDataRepository _userData = null!;
    StatisticsRepository _stats = null!;

    [SetUp]
    public void Setup()
    {
        _connection = new SQLiteConnection(":memory:");
        _connection.CreateTable<DailyProgress>();
        _connection.CreateTable<NounsFormMastery>();
        _connection.CreateTable<VerbsFormMastery>();
        _connection.CreateTable<NounsPracticeHistory>();
        _connection.CreateTable<VerbsPracticeHistory>();
        _connection.CreateTable<UserSetting>();

        _userData = new UserDataRepository(_connection);
        _stats = new StatisticsRepository(_connection, _userData);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    #region Streak Tests

    [Test]
    public void GetCurrentPracticeStreak_NoPracticeToday_IncludesYesterdayStreak()
    {
        // User practiced 3 consecutive days but not today yet
        InsertProgress(DaysAgo(1), 5, 0);
        InsertProgress(DaysAgo(2), 5, 0);
        InsertProgress(DaysAgo(3), 5, 0);

        var streak = _stats.GetCurrentPracticeStreak();

        streak.Should().Be(3, "streak should include days before today if today hasn't been practiced");
    }

    [Test]
    public void GetCurrentPracticeStreak_PracticedToday_IncludesToday()
    {
        InsertProgress(DailyProgress.TodayKey, 5, 0);
        InsertProgress(DaysAgo(1), 5, 0);
        InsertProgress(DaysAgo(2), 5, 0);

        var streak = _stats.GetCurrentPracticeStreak();

        streak.Should().Be(3);
    }

    [Test]
    public void GetCurrentPracticeStreak_GapInMiddle_StopsAtGap()
    {
        InsertProgress(DailyProgress.TodayKey, 5, 0);
        InsertProgress(DaysAgo(1), 5, 0);
        // Gap on day -2
        InsertProgress(DaysAgo(3), 5, 0);

        var streak = _stats.GetCurrentPracticeStreak();

        streak.Should().Be(2, "streak stops at the first gap");
    }

    [Test]
    public void GetCurrentPracticeStreak_NoData_ReturnsZero()
    {
        var streak = _stats.GetCurrentPracticeStreak();

        streak.Should().Be(0);
    }

    [Test]
    public void GetLongestPracticeStreak_OldStreakWasLonger_ReturnsOldStreak()
    {
        // Old streak of 10 days (100-109 days ago)
        for (int i = 100; i < 110; i++)
            InsertProgress(DaysAgo(i), 5, 0);

        // Current streak of 3 days
        InsertProgress(DailyProgress.TodayKey, 5, 0);
        InsertProgress(DaysAgo(1), 5, 0);
        InsertProgress(DaysAgo(2), 5, 0);

        var longest = _stats.GetLongestPracticeStreak();

        longest.Should().Be(10);
    }

    [Test]
    public void GetLongestPracticeStreak_CurrentStreakIsLongest_ReturnsCurrent()
    {
        // Old streak of 2 days
        InsertProgress(DaysAgo(100), 5, 0);
        InsertProgress(DaysAgo(101), 5, 0);

        // Current streak of 5 days
        InsertProgress(DailyProgress.TodayKey, 5, 0);
        InsertProgress(DaysAgo(1), 5, 0);
        InsertProgress(DaysAgo(2), 5, 0);
        InsertProgress(DaysAgo(3), 5, 0);
        InsertProgress(DaysAgo(4), 5, 0);

        var longest = _stats.GetLongestPracticeStreak();

        longest.Should().Be(5);
    }

    #endregion

    #region Goal Streak Tests

    [Test]
    public void GetCurrentGoalStreak_MetNounButNotVerb_OnlyCountsRelevantType()
    {
        _userData.SetSetting(SettingsKeys.NounsDailyGoal, 5);
        _userData.SetSetting(SettingsKeys.VerbsDailyGoal, 5);
        InsertProgress(DailyProgress.TodayKey, 10, 0); // Met noun goal, not verb

        var nounStreak = _stats.GetCurrentGoalStreak(PracticeType.Declension);
        var verbStreak = _stats.GetCurrentGoalStreak(PracticeType.Conjugation);

        nounStreak.Should().Be(1);
        verbStreak.Should().Be(0);
    }

    [Test]
    public void GetCurrentGoalStreak_ExactlyMeetsGoal_CountsAsStreak()
    {
        _userData.SetSetting(SettingsKeys.NounsDailyGoal, 10);
        InsertProgress(DailyProgress.TodayKey, 10, 0); // Exactly meets goal

        var streak = _stats.GetCurrentGoalStreak(PracticeType.Declension);

        streak.Should().Be(1, "exactly meeting goal should count");
    }

    [Test]
    public void GetCurrentGoalStreak_OneBelowGoal_DoesNotCount()
    {
        _userData.SetSetting(SettingsKeys.NounsDailyGoal, 10);
        InsertProgress(DailyProgress.TodayKey, 9, 0); // One below goal

        var streak = _stats.GetCurrentGoalStreak(PracticeType.Declension);

        streak.Should().Be(0, "one below goal should not count");
    }

    #endregion

    #region SRS Distribution Tests

    [Test]
    public void GetNounSrsDistribution_EmptyDatabase_ReturnsAllZeros()
    {
        var dist = _stats.GetNounSrsDistribution();

        dist.Unpracticed.Should().Be(0);
        dist.Struggling.Should().Be(0);
        dist.Learning.Should().Be(0);
        dist.Strong.Should().Be(0);
        dist.Mastered.Should().Be(0);
        dist.Total.Should().Be(0);
    }

    [Test]
    public void GetNounSrsDistribution_CategorizesLevelsCorrectly()
    {
        // Level 1-3: Struggling
        InsertNounMastery(100011110, 1);
        InsertNounMastery(100011120, 2);
        InsertNounMastery(100011130, 3);

        // Level 4-6: Learning
        InsertNounMastery(100021110, 4);
        InsertNounMastery(100021120, 5);
        InsertNounMastery(100021130, 6);

        // Level 7-10: Strong
        InsertNounMastery(100031110, 7);
        InsertNounMastery(100031120, 8);
        InsertNounMastery(100031130, 9);
        InsertNounMastery(100031140, 10);

        // Level 11: Mastered
        InsertNounMastery(100041110, 11);

        var dist = _stats.GetNounSrsDistribution();

        dist.Struggling.Should().Be(3);
        dist.Learning.Should().Be(3);
        dist.Strong.Should().Be(4);
        dist.Mastered.Should().Be(1);
        dist.Total.Should().Be(11);
    }

    #endregion

    #region Combo Stats Tests

    [Test]
    public void GetWeakestNounCombos_FewerThanRequested_PadsWithPlaceholders()
    {
        // Only practice one form (won't meet minimum of 3 forms per combo)
        var formId = Declension.ResolveId(10001, Case.Nominative, Gender.Masculine, Number.Singular, 0);
        InsertNounMastery(formId, 5);

        var weakest = _stats.GetWeakestNounCombos(5);

        weakest.Should().HaveCount(5);
        weakest.All(c => c.IsPlaceholder).Should().BeTrue("single form doesn't meet minimum 3 forms requirement");
    }

    [Test]
    public void GetWeakestNounCombos_ComboWithLessThan3Forms_IsExcluded()
    {
        // Create combo with only 2 forms (below minimum)
        InsertNounMastery(Declension.ResolveId(10001, Case.Nominative, Gender.Masculine, Number.Singular, 0), 2);
        InsertNounMastery(Declension.ResolveId(10002, Case.Nominative, Gender.Masculine, Number.Singular, 0), 2);

        // Create combo with 3 forms (meets minimum)
        InsertNounMastery(Declension.ResolveId(10001, Case.Accusative, Gender.Masculine, Number.Singular, 0), 5);
        InsertNounMastery(Declension.ResolveId(10002, Case.Accusative, Gender.Masculine, Number.Singular, 0), 5);
        InsertNounMastery(Declension.ResolveId(10003, Case.Accusative, Gender.Masculine, Number.Singular, 0), 5);

        var weakest = _stats.GetWeakestNounCombos(5);

        var nonPlaceholders = weakest.Where(c => !c.IsPlaceholder).ToList();
        nonPlaceholders.Should().HaveCount(1, "only combo with >=3 forms should be included");
        nonPlaceholders[0].ComboKey.Should().Contain("acc", "only accusative combo meets minimum");
    }

    [Test]
    public void GetStrongestNounCombos_SortsByAverageMastery()
    {
        // Combo A: average mastery 8
        InsertNounMastery(Declension.ResolveId(10001, Case.Nominative, Gender.Masculine, Number.Singular, 0), 8);
        InsertNounMastery(Declension.ResolveId(10002, Case.Nominative, Gender.Masculine, Number.Singular, 0), 8);
        InsertNounMastery(Declension.ResolveId(10003, Case.Nominative, Gender.Masculine, Number.Singular, 0), 8);

        // Combo B: average mastery 3
        InsertNounMastery(Declension.ResolveId(10001, Case.Accusative, Gender.Masculine, Number.Singular, 0), 3);
        InsertNounMastery(Declension.ResolveId(10002, Case.Accusative, Gender.Masculine, Number.Singular, 0), 3);
        InsertNounMastery(Declension.ResolveId(10003, Case.Accusative, Gender.Masculine, Number.Singular, 0), 3);

        var strongest = _stats.GetStrongestNounCombos(5);

        strongest[0].ComboKey.Should().Contain("nom", "nominative has higher average mastery");
        strongest[0].AverageMastery.Should().Be(8);
    }

    #endregion

    #region Period Stats Tests

    [Test]
    public void GetNounPeriodStats_NoHistory_ReturnsZeros()
    {
        var stats = _stats.GetNounPeriodStats();

        stats.Today.Should().Be(0);
        stats.Last7Days.Should().Be(0);
        stats.AllTime.Should().Be(0);
    }

    [Test]
    public void GetNounPeriodStats_SameFormMultipleTimes_CountsOnce()
    {
        var formId = 100011110L;
        var now = DateTime.UtcNow;

        // Practice same form 3 times today
        InsertNounHistory(formId, now);
        InsertNounHistory(formId, now.AddMinutes(-10));
        InsertNounHistory(formId, now.AddMinutes(-20));

        var stats = _stats.GetNounPeriodStats();

        stats.Today.Should().Be(1, "same form should only count once");
        stats.AllTime.Should().Be(1);
    }

    [Test]
    public void GetNounPeriodStats_DifferentForms_CountsSeparately()
    {
        var now = DateTime.UtcNow;

        InsertNounHistory(100011110L, now);
        InsertNounHistory(100021110L, now);
        InsertNounHistory(100031110L, now);

        var stats = _stats.GetNounPeriodStats();

        stats.Today.Should().Be(3);
        stats.AllTime.Should().Be(3);
    }

    [Test]
    public void GetNounPeriodStats_Last7Days_Includes7DaysNotMore()
    {
        var now = DateTime.UtcNow;

        // Practice within 7-day window (should be included)
        InsertNounHistory(100011110L, now); // Today
        InsertNounHistory(100021110L, now.AddDays(-6)); // 6 days ago (within window)

        // Practice outside 7-day window (should NOT be included in Last7Days)
        InsertNounHistory(100031110L, now.AddDays(-8)); // 8 days ago (outside window)

        var stats = _stats.GetNounPeriodStats();

        stats.Today.Should().Be(1);
        stats.Last7Days.Should().Be(2, "only forms within 7-day window should be counted");
        stats.AllTime.Should().Be(3);
    }

    #endregion

    #region Calendar Tests

    [Test]
    public void GetLast30DaysCalendar_ReturnsExactly30Days()
    {
        var calendar = _stats.GetLast30DaysCalendar();

        calendar.Should().HaveCount(30);
    }

    [Test]
    public void GetLast30DaysCalendar_NoData_ReturnsZeroCounts()
    {
        var calendar = _stats.GetLast30DaysCalendar();

        calendar.All(d => d.DeclensionsCount == 0 && d.ConjugationsCount == 0).Should().BeTrue();
        calendar.All(d => !d.HasPractice).Should().BeTrue();
    }

    [Test]
    public void GetCalendarData_February_HandlesLeapYear()
    {
        // 2024 is a leap year
        var calendar = _stats.GetCalendarData(2024, 2);

        calendar.Should().HaveCount(29);
    }

    [Test]
    public void GetCalendarData_February_HandlesNonLeapYear()
    {
        // 2023 is not a leap year
        var calendar = _stats.GetCalendarData(2023, 2);

        calendar.Should().HaveCount(28);
    }

    [Test]
    public void CalendarDayDto_Intensity_CalculatesCorrectly()
    {
        var noActivity = new CalendarDayDto { Date = "2024-01-01", DeclensionsCount = 0, ConjugationsCount = 0 };
        var lightActivity = new CalendarDayDto { Date = "2024-01-02", DeclensionsCount = 10, ConjugationsCount = 5 };
        var mediumActivity = new CalendarDayDto { Date = "2024-01-03", DeclensionsCount = 20, ConjugationsCount = 10 };
        var heavyActivity = new CalendarDayDto { Date = "2024-01-04", DeclensionsCount = 25, ConjugationsCount = 20 };

        noActivity.Intensity.Should().Be(0);
        lightActivity.Intensity.Should().Be(1); // 15 total
        mediumActivity.Intensity.Should().Be(2); // 30 total
        heavyActivity.Intensity.Should().Be(3); // 45 total
    }

    #endregion

    #region Due Forms Count Tests

    [Test]
    public void DueForReview_CountsAllDueForms_NotCappedAt1000()
    {
        // Insert 5 forms that are due
        var pastTime = DateTime.UtcNow.AddDays(-30);
        for (int i = 1; i <= 5; i++)
        {
            InsertNounMasteryWithTime(100010000 + i * 1000, 1, pastTime);
        }

        var stats = _stats.GetNounStats();

        stats.DueForReview.Should().Be(5);
    }

    [Test]
    public void DueForReview_RetiredForms_NotCounted()
    {
        // Use time long enough ago that all levels 1-10 are due
        // Level 10 has ~254 day cooldown, so use 300 days ago
        var pastTime = DateTime.UtcNow.AddDays(-300);

        // Due forms (levels 1-5) - all will be due after 300 days
        InsertNounMasteryWithTime(100011110, 1, pastTime);
        InsertNounMasteryWithTime(100021110, 3, pastTime);
        InsertNounMasteryWithTime(100031110, 5, pastTime);

        // Retired form (level 11) - should not be counted even with old timestamp
        InsertNounMasteryWithTime(100041110, 11, pastTime);

        var stats = _stats.GetNounStats();

        stats.DueForReview.Should().Be(3, "retired forms should not be counted as due");
    }

    #endregion

    #region ComboStatDto Tests

    [Test]
    public void ComboStatDto_MasteryPercent_Level1Is10Percent()
    {
        var combo = new ComboStatDto
        {
            ComboKey = "test",
            DisplayName = "Test",
            FormCount = 3,
            AverageMastery = 1.0
        };

        combo.MasteryPercent.Should().Be(10);
    }

    [Test]
    public void ComboStatDto_MasteryPercent_Level10Is100Percent()
    {
        var combo = new ComboStatDto
        {
            ComboKey = "test",
            DisplayName = "Test",
            FormCount = 3,
            AverageMastery = 10.0
        };

        combo.MasteryPercent.Should().Be(100);
    }

    [Test]
    public void ComboStatDto_MasteryPercent_Level11CapsAt100()
    {
        var combo = new ComboStatDto
        {
            ComboKey = "test",
            DisplayName = "Test",
            FormCount = 3,
            AverageMastery = 11.0
        };

        combo.MasteryPercent.Should().Be(100, "level 11 should cap at 100%");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the date string for N days ago in YYYY-MM-DD format.
    /// </summary>
    static string DaysAgo(int days)
    {
        return DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd");
    }

    void InsertProgress(string dateKey, int declensions, int conjugations)
    {
        _connection.Insert(new DailyProgress
        {
            Date = dateKey,
            DeclensionsCompleted = declensions,
            ConjugationsCompleted = conjugations
        });
    }

    void InsertNounMastery(long formId, int level)
    {
        _connection.Insert(new NounsFormMastery
        {
            FormId = formId,
            MasteryLevel = level,
            LastPracticedUtc = DateTime.UtcNow.AddDays(-1)
        });
    }

    void InsertNounMasteryWithTime(long formId, int level, DateTime lastPracticedUtc)
    {
        _connection.Insert(new NounsFormMastery
        {
            FormId = formId,
            MasteryLevel = level,
            LastPracticedUtc = lastPracticedUtc
        });
    }

    void InsertNounHistory(long formId, DateTime practicedUtc)
    {
        _connection.Insert(new NounsPracticeHistory
        {
            FormId = formId,
            OldLevel = 0,
            NewLevel = 4,
            PracticedUtc = practicedUtc
        });
    }

    #endregion
}
