namespace PaliPractice.Services.Feedback;

/// <summary>
/// Service for store reviews and in-app review prompts.
/// </summary>
public interface IStoreReviewService
{
    /// <summary>
    /// Returns true if the current platform supports opening the store page.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Opens the app's store page in the platform's app store.
    /// </summary>
    Task OpenStorePageAsync();

    /// <summary>
    /// Checks engagement conditions and requests an in-app review if appropriate.
    /// Call this when entering practice sessions.
    /// </summary>
    Task TryPromptForReviewAsync();
}
