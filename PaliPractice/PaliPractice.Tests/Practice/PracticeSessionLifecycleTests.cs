using Microsoft.Extensions.Logging.Abstractions;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Feedback;
using PaliPractice.Services.Grammar;
using PaliPractice.Services.Practice;
using PaliPractice.Tests.Practice.Fakes;
using PaliPractice.Tests.Practice.Builders;
using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.Practice;

[TestFixture]
public class PracticeSessionLifecycleTests
{
    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task EmptySessionNotifiesAfterSubscriptionAndNeverEnablesCardActions(PracticeType type)
    {
        var db = new FakeDatabaseService();
        var queue = new PracticeQueueBuilder(db);
        PracticeViewModelBase vm = type == PracticeType.Declension
            ? new DeclensionPracticeViewModel(new DeclensionPracticeProvider(queue, db), db,
                new FlashCardViewModel(), null!, new NoStoreReview(),
                NullLogger<DeclensionPracticeViewModel>.Instance, new InflectionService(db))
            : new ConjugationPracticeViewModel(new ConjugationPracticeProvider(queue, db), db,
                new FlashCardViewModel(), null!, new NoStoreReview(),
                NullLogger<ConjugationPracticeViewModel>.Instance, new InflectionService(db));
        var exhausted = 0;
        vm.QueueExhausted += (_, _) => exhausted++;
        vm.FlashCard.IsLoading.Should().BeTrue();
        vm.RevealCommand.CanExecute(null).Should().BeFalse();

        await vm.StartAsync();

        exhausted.Should().Be(1);
        vm.FlashCard.IsLoading.Should().BeFalse();
        vm.FlashCard.ErrorMessage.Should().BeEmpty();
        vm.RevealCommand.CanExecute(null).Should().BeFalse();
        vm.RevealCommand.Execute(null); // Programmatic execution must also be safe.
        vm.FlashCard.IsRevealed.Should().BeFalse();
        vm.EasyCommand.CanExecute(null).Should().BeFalse();
        vm.HardCommand.CanExecute(null).Should().BeFalse();
        db.UserData.GetPracticeCount(type).Should().Be(0);
    }

    [Test]
    public async Task UnchangedSessionPreservesRevealedCard()
    {
        var (vm, provider, _) = CreateNounSession();
        await vm.StartAsync();
        var formId = provider.Current!.FormId;
        vm.FlashCard.ErrorMessage.Should().BeEmpty();
        vm.RevealCommand.Execute(null);

        await vm.StartAsync();

        provider.Current!.FormId.Should().Be(formId);
        vm.FlashCard.IsRevealed.Should().BeTrue();
        vm.EasyCommand.CanExecute(null).Should().BeTrue();
    }

    [Test]
    public async Task ChangedFiltersAndGoalRefreshCachedSession()
    {
        var (vm, provider, db) = CreateNounSession();
        await vm.StartAsync();
        db.UserData.SetSetting(SettingsKeys.NounsCases, "2");
        db.UserData.SetSetting(SettingsKeys.NounsDailyGoal, 10);

        await vm.StartAsync();

        Declension.ParseId(provider.Current!.FormId).Case.Should().Be(Case.Accusative);
        vm.DailyGoal.DailyGoalText.Should().Be("0/10");
        vm.FlashCard.ErrorMessage.Should().BeEmpty();
        vm.RevealCommand.CanExecute(null).Should().BeTrue();
    }

    [Test]
    public async Task ChangedLogicalDayRefreshesProgressAndCard()
    {
        var (vm, _, db) = CreateNounSession();
        await vm.StartAsync();
        vm.RevealCommand.Execute(null);
        var progress = db.UserData.GetTodayProgress();
        progress.Date = PaliPractice.Services.UserData.Entities.DailyProgress.ToDateKey(
            PaliPractice.Services.UserData.Entities.DailyProgress.FromDateKey(progress.Date).AddDays(1));
        progress.DeclensionsCompleted = 0;

        await vm.StartAsync();

        vm.FlashCard.IsRevealed.Should().BeFalse();
        vm.RevealCommand.CanExecute(null).Should().BeTrue();
    }

    [Test]
    public async Task ExhaustedSessionCanRecoverAfterSettingsChange()
    {
        var (vm, provider, db) = CreateNounSession();
        var forms = new PracticeQueueBuilder(db).BuildQueue(PracticeType.Declension, 100);
        foreach (var form in forms)
            db.FakeUserData.AddNounFormMastery(form.FormId, 9, DateTime.UtcNow);
        var exhausted = 0;
        vm.QueueExhausted += (_, _) => exhausted++;
        await vm.StartAsync();
        exhausted.Should().Be(1);
        provider.Current.Should().BeNull();

        db.UserData.SetSetting(SettingsKeys.NounsCases, "2");
        await vm.StartAsync();

        provider.Current.Should().NotBeNull();
        vm.FlashCard.Question.Should().NotBeEmpty();
        vm.RevealCommand.CanExecute(null).Should().BeTrue();
    }

    static (PracticeViewModelBase Vm, DeclensionPracticeProvider Provider, FakeDatabaseService Db) CreateNounSession()
    {
        var db = new TestScenarioBuilder().WithNounRange(1, 20)
            .WithCases(Case.Nominative, Case.Accusative).WithNumbers(Number.Plural)
            .WithMascPatterns(NounPattern.AMasc).AddNouns(20, NounPattern.AMasc, Gender.Masculine).Build();
        db.UserData.SetSetting(SettingsKeys.NounsCases, "1");
        var provider = new DeclensionPracticeProvider(new PracticeQueueBuilder(db), db);
        var vm = new SessionViewModel(provider, db);
        return (vm, provider, db);
    }

    [TestCase(1, 9, "exhausted")]
    [TestCase(2, 9, "goal")]
    [TestCase(1, 0, "exhausted")]
    public async Task RatingProducesOneCompletionOutcome(int cardCount, int completed, string expected)
    {
        var db = new FakeDatabaseService();
        db.UserData.SetSetting(SettingsKeys.NounsDailyGoal, 10);
        for (var i = 0; i < completed; i++)
            db.UserData.IncrementProgress(PracticeType.Declension);
        var provider = new FixedProvider(cardCount);
        var vm = new SessionViewModel(provider, db);
        var outcomes = new List<string>();
        vm.QueueExhausted += (_, _) => outcomes.Add("exhausted");
        vm.DailyGoalReached += (_, _) => outcomes.Add("goal");
        await vm.StartAsync();
        vm.RevealCommand.Execute(null);

        vm.EasyCommand.Execute(null);

        outcomes.Should().Equal(expected);
        db.UserData.GetTodayProgress().DeclensionsCompleted.Should().Be(completed + 1);
        vm.EasyCommand.CanExecute(null).Should().BeFalse();
        vm.RevealCommand.CanExecute(null).Should().Be(cardCount > 1);
    }

    sealed class FixedProvider(int count) : IPracticeProvider
    {
        int _index;
        public PracticeItem? Current => _index < count
            ? PracticeItem.NewForm(Declension.ResolveId(10001 + _index, Case.Nominative, Gender.Masculine, Number.Plural, 0),
                PracticeType.Declension, 10001 + _index) : null;
        public int CurrentIndex => _index;
        public int TotalCount => count;
        public bool HasNext => _index + 1 < count;
        public Task LoadAsync(CancellationToken ct = default) { _index = 0; return Task.CompletedTask; }
        public bool MoveNext() => ++_index < count;
        public ILemma? GetCurrentLemma() => Current == null ? null :
            FakeLemma.CreateNoun(10001 + _index, "deva", Gender.Masculine, NounPattern.AMasc);
        public object GetCurrentParameters() => (Case.Nominative, Gender.Masculine, Number.Plural);
    }

    // The shared session logic needs no native theme or badge rendering.
    sealed class SessionViewModel(IPracticeProvider provider, FakeDatabaseService db)
        : PracticeViewModelBase(provider, db.UserData, new FlashCardViewModel(), null!, new NoStoreReview(), NullLogger.Instance)
    {
        public override PracticeType PracticeTypePublic => PracticeType.Declension;
        protected override PracticeType CurrentPracticeType => PracticeType.Declension;
        public override System.Windows.Input.ICommand GoToSettingsCommand => null!;
        protected override void PrepareCardAnswer(ILemma lemma, object parameters) { }
        protected override string GetInflectedForm() => "devā";
        protected override string GetInflectedEnding() => "ā";
        protected override IReadOnlyList<string> GetAllInflectedForms() => ["devā"];
        protected override string GetAlternativeForms() => string.Empty;
    }

    sealed class NoStoreReview : IStoreReviewService
    {
        public bool IsAvailable => false;
        public bool HasUserOpenedStore => false;
        public Task OpenStorePageAsync() => Task.CompletedTask;
        public Task TryPromptForReviewAsync() => Task.CompletedTask;
    }
}
