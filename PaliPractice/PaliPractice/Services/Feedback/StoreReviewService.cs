using Windows.System;
using PaliPractice.Services.Database;
using PaliPractice.Services.Feedback.Helpers;
using PaliPractice.Services.Feedback.Storage;

#if __IOS__ || __ANDROID__
using Windows.Services.Store;
#endif

namespace PaliPractice.Services.Feedback;

/// <summary>
/// Service for store reviews with engagement-based prompting.
/// </summary>
public sealed class StoreReviewService : IStoreReviewService
{
    /// <summary>
    /// Minimum days between review prompts (125 days ~ 4 months).
    /// </summary>
    const int CooldownDays = 125;

    /// <summary>
    /// Maximum lifetime review prompts to avoid annoying users.
    /// </summary>
    const int MaxLifetimePrompts = 3;

    /// <summary>
    /// Minimum practice items (nouns OR verbs) before prompting.
    /// </summary>
    const int MinPracticeItems = 10;

    readonly ILogger<StoreReviewService> _logger;
    readonly IDatabaseService _db;
    readonly ReviewPromptStorage _storage;

    public StoreReviewService(ILogger<StoreReviewService> logger, IDatabaseService db)
    {
        _logger = logger;
        _db = db;
        _storage = new ReviewPromptStorage();
    }

    /// <inheritdoc />
    public bool IsAvailable => StorePlatformHelper.IsStoreAvailable();

    /// <inheritdoc />
    public bool HasUserOpenedStore => _storage.HasUserOpenedStore;

    /// <inheritdoc />
    public async Task OpenStorePageAsync()
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Store page not available on this platform");
            return;
        }

        var url = StorePlatformHelper.GetStoreUrl();
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("No store URL configured for this platform");
            return;
        }

        try
        {
            _logger.LogInformation("Opening store page: {Url}", url);
            await Launcher.LaunchUriAsync(new Uri(url));

            // Record that user manually opened store - this disables future automatic prompts
            _storage.HasUserOpenedStore = true;
            _storage.PromptCount = MaxLifetimePrompts;
            _logger.LogInformation("User manually opened store, automatic prompts disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open store page");
        }
    }

    /// <inheritdoc />
    public async Task TryPromptForReviewAsync()
    {
        if (!ShouldPromptForReview())
            return;

        await RequestInAppReviewAsync();
    }

    /// <summary>
    /// Checks all engagement conditions for showing a review prompt.
    /// </summary>
    bool ShouldPromptForReview()
    {
        // Check platform support
        if (!StorePlatformHelper.IsInAppReviewAvailable())
        {
            _logger.LogDebug("In-app review not available on this platform");
            return false;
        }

        // Check lifetime limit
        if (_storage.PromptCount >= MaxLifetimePrompts)
        {
            _logger.LogDebug("Max lifetime prompts ({Max}) reached, count={Count}",
                MaxLifetimePrompts, _storage.PromptCount);
            return false;
        }

        // Check cooldown
        var daysSinceLastPrompt = _storage.DaysSinceLastPrompt;
        if (daysSinceLastPrompt < CooldownDays)
        {
            _logger.LogDebug("Still on cooldown, {Days} days since last prompt (need {Required})",
                daysSinceLastPrompt, CooldownDays);
            return false;
        }

        // Check minimum practice (either nouns OR verbs, not combined)
        int totalNounsPracticed, totalVerbsPracticed;
        try
        {
            totalNounsPracticed = _db.Statistics.GetNounPracticedCount();
            totalVerbsPracticed = _db.Statistics.GetVerbPracticedCount();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get practice counts, skipping review prompt");
            return false;
        }

        var hasEnoughPractice = totalNounsPracticed >= MinPracticeItems ||
                               totalVerbsPracticed >= MinPracticeItems;
        if (!hasEnoughPractice)
        {
            _logger.LogDebug("Not enough practice, nouns={Nouns} verbs={Verbs} (need {Min} of either)",
                totalNounsPracticed, totalVerbsPracticed, MinPracticeItems);
            return false;
        }

        _logger.LogInformation("Review prompt conditions met: count={Count}/{Max}, daysSince={Days}, nouns={Nouns}, verbs={Verbs}",
            _storage.PromptCount, MaxLifetimePrompts, daysSinceLastPrompt, totalNounsPracticed, totalVerbsPracticed);

        return true;
    }

    /// <summary>
    /// Requests the native in-app review dialog.
    /// </summary>
    async Task RequestInAppReviewAsync()
    {
#if __IOS__ || __ANDROID__
        try
        {
            _logger.LogInformation("Requesting in-app review (prompt #{Count})", _storage.PromptCount + 1);

            // Record prompt BEFORE calling API (in case of crash/exception)
            _storage.RecordPrompt();

            var result = await StoreContext.GetDefault().RequestRateAndReviewAppAsync();
            _logger.LogInformation("In-app review result: {Status}", result.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request in-app review");
        }
#else
        await Task.CompletedTask;
        _logger.LogWarning("In-app review not implemented for this platform");
#endif
    }
}
