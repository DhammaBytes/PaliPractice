using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Providers;
using PaliPractice.Services.Database.Repositories;

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
