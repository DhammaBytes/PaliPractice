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

    sealed class NoStoreReview : IStoreReviewService
    {
        public bool IsAvailable => false;
        public bool HasUserOpenedStore => false;
        public Task OpenStorePageAsync() => Task.CompletedTask;
        public Task TryPromptForReviewAsync() => Task.CompletedTask;
    }
}
