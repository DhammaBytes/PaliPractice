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
    /// Returns true if the user has manually opened the store page.
    /// When true, the review explanation footer should be hidden and automatic prompts are disabled.
    /// </summary>
    bool HasUserOpenedStore { get; }

    /// <summary>
    /// Opens the app's store page in the platform's app store.
    /// Also marks that the user has opened the store, which disables future automatic prompts.
    /// </summary>
    Task OpenStorePageAsync();

    /// <summary>
    /// Checks engagement conditions and requests an in-app review if appropriate.
    /// Call this when entering practice sessions.
    /// </summary>
    Task TryPromptForReviewAsync();
}
