using PaliPractice.Services.Feedback;

namespace PaliPractice.Tests.Feedback;

/// <summary>
/// Tests for coordination between manual store review and automatic prompts.
/// </summary>
[TestFixture]
public class StoreReviewCoordinationTests
{
    #region FakeStoreReviewService Logic Tests

    [Test]
    public void HasUserOpenedStore_InitiallyFalse()
    {
        var service = new FakeStoreReviewService(isAvailable: true);

        service.HasUserOpenedStore.Should().BeFalse();
    }

    [Test]
    public async Task OpenStorePageAsync_SetsHasUserOpenedStoreToTrue()
    {
        var service = new FakeStoreReviewService(isAvailable: true);

        await service.OpenStorePageAsync();

        service.HasUserOpenedStore.Should().BeTrue();
    }

    [Test]
    public async Task OpenStorePageAsync_DisablesAutomaticPrompts()
    {
        var service = new FakeStoreReviewService(isAvailable: true);

        await service.OpenStorePageAsync();

        // After opening store manually, automatic prompts should be disabled
        service.AutomaticPromptsDisabled.Should().BeTrue();
    }

    [Test]
    public async Task TryPromptForReviewAsync_DoesNotPromptAfterManualOpen()
    {
        var service = new FakeStoreReviewService(isAvailable: true);
        await service.OpenStorePageAsync();

        await service.TryPromptForReviewAsync();

        // Should not have attempted to show prompt after manual store open
        service.PromptAttempts.Should().Be(0);
    }

    [Test]
    public async Task TryPromptForReviewAsync_CanPromptIfNotManuallyOpened()
    {
        var service = new FakeStoreReviewService(isAvailable: true);

        await service.TryPromptForReviewAsync();

        // Should have attempted prompt (fake always allows it when not disabled)
        service.PromptAttempts.Should().Be(1);
    }

    #endregion

    #region ViewModel Property Tests

    [Test]
    public void ShouldShowReviewExplanation_TrueWhenAvailableAndNotOpened()
    {
        var service = new FakeStoreReviewService(isAvailable: true);

        var shouldShow = service.IsAvailable && !service.HasUserOpenedStore;

        shouldShow.Should().BeTrue();
    }

    [Test]
    public async Task ShouldShowReviewExplanation_FalseAfterUserOpensStore()
    {
        var service = new FakeStoreReviewService(isAvailable: true);
        await service.OpenStorePageAsync();

        var shouldShow = service.IsAvailable && !service.HasUserOpenedStore;

        shouldShow.Should().BeFalse();
    }

    [Test]
    public void ShouldShowReviewExplanation_FalseWhenNotAvailable()
    {
        var service = new FakeStoreReviewService(isAvailable: false);

        var shouldShow = service.IsAvailable && !service.HasUserOpenedStore;

        shouldShow.Should().BeFalse();
    }

    #endregion
}

/// <summary>
/// Fake implementation of IStoreReviewService for testing coordination logic.
/// </summary>
public class FakeStoreReviewService : IStoreReviewService
{
    public bool IsAvailable { get; }
    public bool HasUserOpenedStore { get; private set; }
    public bool AutomaticPromptsDisabled { get; private set; }
    public int PromptAttempts { get; private set; }

    public FakeStoreReviewService(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }

    public Task OpenStorePageAsync()
    {
        if (!IsAvailable)
            return Task.CompletedTask;

        HasUserOpenedStore = true;
        AutomaticPromptsDisabled = true;
        return Task.CompletedTask;
    }

    public Task TryPromptForReviewAsync()
    {
        if (AutomaticPromptsDisabled)
            return Task.CompletedTask;

        PromptAttempts++;
        return Task.CompletedTask;
    }
}
