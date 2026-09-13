namespace Jarvis.Core.Security;

/// <summary>
/// Gate for destructive or high-impact actions (shell commands, deletes, overwrites,
/// self-upgrade installs). The UI layer supplies an implementation that shows a modal;
/// headless contexts can use <see cref="AutoApproveConfirmationService"/>.
/// </summary>
public interface IConfirmationService
{
    /// <summary>Ask the user to approve an action. Returns true if approved.</summary>
    Task<bool> ConfirmAsync(string title, string details, CancellationToken cancellationToken = default);
}

/// <summary>Approves everything without asking — used when confirmation is disabled or no UI is attached.</summary>
public sealed class AutoApproveConfirmationService : IConfirmationService
{
    public Task<bool> ConfirmAsync(string title, string details, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
