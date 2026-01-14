using PaliPractice.Models.Inflection;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using PaliPractice.Tests.Practice.Builders;
using PaliPractice.Tests.Practice.Fakes;

namespace PaliPractice.Tests.Practice;

[TestFixture]
public class PracticeProviderTests
{
    #region Queue Buffer Tests

    [Test]
    public async Task LoadAsync_BuildsQueueAt120PercentOfDailyGoal()
    {
        // Arrange: 100 eligible forms, daily goal = 50
        // Expected queue size: 60 (50 * 1.2)
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 20)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative, Case.Ablative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(20, NounPattern.AMasc, Gender.Masculine) // 20 * 5 = 100 forms
            .Build();

        db.FakeUserData.SetSetting(SettingsKeys.NounsDailyGoal, 50);

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new DeclensionPracticeProvider(queueBuilder, db);

        // Act
        await provider.LoadAsync();

        // Assert: queue should be 60 items (120% of 50)
        provider.TotalCount.Should().Be(60,
            "Queue should be built at 120% of daily goal (50 * 1.2 = 60)");
    }

    [Test]
    public async Task LoadAsync_CapsQueueAtAvailableForms_WhenPoolIsSmall()
    {
        // Arrange: Only 30 eligible forms, daily goal = 50
        // Expected queue size: 30 (capped at available)
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 10)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(10, NounPattern.AMasc, Gender.Masculine) // 10 * 3 = 30 forms
            .Build();

        db.FakeUserData.SetSetting(SettingsKeys.NounsDailyGoal, 50);

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new DeclensionPracticeProvider(queueBuilder, db);

        // Act
        await provider.LoadAsync();

        // Assert: queue should be capped at 30 (all available)
        provider.TotalCount.Should().Be(30,
            "Queue should be capped at available forms when pool is smaller than 120% of goal");
    }

    #endregion

    #region Silent Rebuild Tests

    [Test]
    public async Task MoveNext_SilentlyRebuilds_WhenQueueExhaustedButMoreFormsAvailable()
    {
        // Arrange: 100 eligible forms, daily goal = 10 (queue = 12)
        // After exhausting 12, should silently rebuild with 12 more
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 20)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative, Case.Ablative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(20, NounPattern.AMasc, Gender.Masculine) // 100 forms
            .Build();

        db.FakeUserData.SetSetting(SettingsKeys.NounsDailyGoal, 10); // Queue = 12

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new DeclensionPracticeProvider(queueBuilder, db);
        await provider.LoadAsync();

        var initialQueueSize = provider.TotalCount;
        initialQueueSize.Should().Be(12);

        // Act: Exhaust the initial queue
        int moveCount = 0;
        while (provider.MoveNext())
        {
            moveCount++;
            // Simulate practicing by marking forms as practiced
            var current = provider.Current!;
            db.FakeUserData.AddNounFormMastery(current.FormId, masteryLevel: 4, lastPracticedUtc: DateTime.UtcNow);
        }

        // Assert: Should have moved more than initial queue size (silent rebuild happened)
        // Initial queue was 12, but we have 100 forms, so we should be able to continue
        moveCount.Should().BeGreaterThan(initialQueueSize,
            "MoveNext should silently rebuild when queue exhausted but more forms available");
    }

    [Test]
    public async Task MoveNext_ReturnsFalse_WhenPoolTrulyExhausted()
    {
        // Arrange: Only 5 forms, daily goal = 10
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Nominative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine) // 5 forms only
            .Build();

        db.FakeUserData.SetSetting(SettingsKeys.NounsDailyGoal, 10);

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new DeclensionPracticeProvider(queueBuilder, db);
        await provider.LoadAsync();

        // Mark first item as practiced (it's already loaded at index 0)
        var firstItem = provider.Current!;
        db.FakeUserData.AddNounFormMastery(firstItem.FormId, masteryLevel: 4, lastPracticedUtc: DateTime.UtcNow);

        // Act: Exhaust remaining forms (mark them as practiced with cooldown)
        int moveCount = 0;
        while (provider.MoveNext())
        {
            moveCount++;
            var current = provider.Current!;
            // Mark as practiced just now - will be on cooldown
            db.FakeUserData.AddNounFormMastery(current.FormId, masteryLevel: 4, lastPracticedUtc: DateTime.UtcNow);
        }

        // Assert: Should have 4 more moves after the first (5 total forms)
        moveCount.Should().Be(4,
            "Should stop after all 5 forms are practiced and on cooldown (1 initial + 4 moves)");

        // Verify MoveNext now returns false
        provider.MoveNext().Should().BeFalse(
            "MoveNext should return false when pool is truly exhausted");
    }

    [Test]
    public async Task MoveNext_ContinuesSeamlessly_UserDoesNotNotice()
    {
        // Arrange: Enough forms for multiple queue rebuilds
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 10)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(10, NounPattern.AMasc, Gender.Masculine) // 80 forms
            .Build();

        db.FakeUserData.SetSetting(SettingsKeys.NounsDailyGoal, 5); // Queue = 6

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new DeclensionPracticeProvider(queueBuilder, db);
        await provider.LoadAsync();

        // Act: Move through items, tracking when CurrentIndex resets (indicates rebuild)
        var indices = new List<int> { provider.CurrentIndex };
        int rebuilds = 0;
        int prevIndex = 0;

        for (int i = 0; i < 30; i++) // Practice 30 items (across multiple rebuilds)
        {
            var current = provider.Current!;
            db.FakeUserData.AddNounFormMastery(current.FormId, masteryLevel: 4, lastPracticedUtc: DateTime.UtcNow);

            if (!provider.MoveNext()) break;

            if (provider.CurrentIndex < prevIndex)
                rebuilds++;

            prevIndex = provider.CurrentIndex;
            indices.Add(provider.CurrentIndex);
        }

        // Assert: Should have had multiple silent rebuilds
        rebuilds.Should().BeGreaterThan(0,
            "With 80 forms and queue of 6, should have multiple silent rebuilds");
    }

    #endregion

    #region Conjugation Provider Tests

    [Test]
    public async Task ConjugationProvider_BuildsQueueAt120Percent()
    {
        var db = new TestScenarioBuilder()
            .WithVerbRange(1, 10)
            .WithTenses(Tense.Present, Tense.Imperative)
            .WithPersons(Person.First, Person.Second, Person.Third)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithVoices(Voice.Active)
            .WithVerbPatterns(VerbPattern.Ati)
            .AddVerbs(10, VerbPattern.Ati) // Many forms
            .Build();

        db.FakeUserData.SetSetting(SettingsKeys.VerbsDailyGoal, 20);

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new ConjugationPracticeProvider(queueBuilder, db);

        await provider.LoadAsync();

        // 120% of 20 = 24
        provider.TotalCount.Should().Be(24,
            "Conjugation provider should also build at 120% of daily goal");
    }

    [Test]
    public async Task ConjugationProvider_SilentlyRebuilds()
    {
        var db = new TestScenarioBuilder()
            .WithVerbRange(1, 20)
            .WithTenses(Tense.Present)
            .WithPersons(Person.First, Person.Second, Person.Third)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithVoices(Voice.Active)
            .WithVerbPatterns(VerbPattern.Ati)
            .AddVerbs(20, VerbPattern.Ati)
            .Build();

        db.FakeUserData.SetSetting(SettingsKeys.VerbsDailyGoal, 5); // Queue = 6

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new ConjugationPracticeProvider(queueBuilder, db);
        await provider.LoadAsync();

        var initialQueueSize = provider.TotalCount;

        // Exhaust initial queue
        int moveCount = 0;
        while (provider.MoveNext())
        {
            moveCount++;
            var current = provider.Current!;
            db.FakeUserData.AddVerbFormMastery(current.FormId, masteryLevel: 4, lastPracticedUtc: DateTime.UtcNow);
        }

        moveCount.Should().BeGreaterThan(initialQueueSize,
            "Conjugation provider should also silently rebuild");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task LoadAsync_EmptyPool_ReturnsZeroCount()
    {
        // No nouns added at all
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 10)
            .WithCases(Case.Nominative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            // Note: No AddNouns call!
            .Build();

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new DeclensionPracticeProvider(queueBuilder, db);

        await provider.LoadAsync();

        provider.TotalCount.Should().Be(0);
        provider.Current.Should().BeNull();
    }

    [Test]
    public async Task MoveNext_WithEmptyQueue_ReturnsFalse()
    {
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 10)
            .WithCases(Case.Nominative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .Build();

        var queueBuilder = new PracticeQueueBuilder(db);
        var provider = new DeclensionPracticeProvider(queueBuilder, db);
        await provider.LoadAsync();

        provider.MoveNext().Should().BeFalse();
    }

    #endregion
}
