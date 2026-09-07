using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;

namespace PaliPractice.Tests.Practice.Fakes;

/// <summary>
/// Test database service that composes arbitrary repository implementations.
/// This allows contract tests to use the production user-data repository while
/// keeping the grammar corpus small and deterministic.
/// </summary>
public sealed class RepositoryBackedTestDatabaseService : IDatabaseService
{
    public INounRepository Nouns { get; }
    public IVerbRepository Verbs { get; }
    public IUserDataRepository UserData { get; }
    public IStatisticsRepository Statistics { get; } = new FakeStatisticsRepository();
    public bool HasFatalFailure => false;
    public IReadOnlyList<DatabaseProvisionedEvent> ProvisionLog => [];

    public RepositoryBackedTestDatabaseService(
        INounRepository nouns,
        IVerbRepository verbs,
        IUserDataRepository userData)
    {
        Nouns = nouns;
        Verbs = verbs;
        UserData = userData;
    }

    public void PreloadCaches() => throw new NotSupportedException();
}
