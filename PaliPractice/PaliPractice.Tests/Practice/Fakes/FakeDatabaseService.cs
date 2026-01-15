using PaliPractice.Models.Inflection;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Providers;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData.Statistics;

namespace PaliPractice.Tests.Practice.Fakes;

/// <summary>
/// Fake implementation of IDatabaseService for testing.
/// Composes FakeNounRepository, FakeVerbRepository, and FakeUserDataRepository.
/// </summary>
public class FakeDatabaseService : IDatabaseService
{
    public FakeNounRepository FakeNouns { get; }
    public FakeVerbRepository FakeVerbs { get; }
    public FakeUserDataRepository FakeUserData { get; }

    // Interface implementation
    public INounRepository Nouns => FakeNouns;
    public IVerbRepository Verbs => FakeVerbs;
    public IUserDataRepository UserData => FakeUserData;
    public IStatisticsRepository Statistics => new FakeStatisticsRepository();
    public bool HasFatalFailure => false;
    public IReadOnlyList<DatabaseProvisionedEvent> ProvisionLog => [];
    public void PreloadCaches()
    {
        throw new NotImplementedException();
    }

    public FakeDatabaseService()
    {
        FakeNouns = new FakeNounRepository();
        FakeVerbs = new FakeVerbRepository();
        FakeUserData = new FakeUserDataRepository();
    }

    /// <summary>
    /// Creates a database service with the specified fake repositories.
    /// </summary>
    public FakeDatabaseService(
        FakeNounRepository nouns,
        FakeVerbRepository verbs,
        FakeUserDataRepository userData)
    {
        FakeNouns = nouns;
        FakeVerbs = verbs;
        FakeUserData = userData;
    }
}

/// <summary>
/// Minimal fake statistics repository for testing.
/// Returns empty/default values for all methods.
/// </summary>
public class FakeStatisticsRepository : IStatisticsRepository
{
    public List<CalendarDayDto> GetCalendarData(int? year = null, int? month = null) => [];
    public List<CalendarDayDto> GetLast30DaysCalendar() => [];
    public int GetCurrentPracticeStreak() => 0;
    public int GetLongestPracticeStreak() => 0;
    public int GetCurrentGoalStreak(PracticeType type) => 0;
    public int GetTotalPracticeDays() => 0;
    public GeneralStatsDto GetGeneralStats() => new();
    public int GetNounPracticedCount() => 0;
    public PracticeTypeStatsDto GetNounStats() => new();
    public SrsDistributionDto GetNounSrsDistribution() => new();
    public List<ComboStatDto> GetStrongestNounCombos(int count = 5) => [];
    public List<ComboStatDto> GetWeakestNounCombos(int count = 5) => [];
    public PeriodStatsDto GetNounPeriodStats() => new();
    public int GetVerbPracticedCount() => 0;
    public PracticeTypeStatsDto GetVerbStats() => new();
    public SrsDistributionDto GetVerbSrsDistribution() => new();
    public List<ComboStatDto> GetStrongestVerbCombos(int count = 5) => [];
    public List<ComboStatDto> GetWeakestVerbCombos(int count = 5) => [];
    public PeriodStatsDto GetVerbPeriodStats() => new();
}
